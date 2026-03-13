using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.ExcelCore;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Excel.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Excel.Commands.ImportAccounts
{
    public class ImportAccountCommandHandler : IRequestHandler<ImportAccountCommand, OperationResult<ImportAccountResponse>>
    {
        private readonly IExcelBaseService _excelBaseService;
        private readonly IAccountRepository _accountRepository;
        private readonly IIdGeneratorService _idGenerator;

        public ImportAccountCommandHandler(
            IExcelBaseService excelBaseService,
            IAccountRepository accountRepository,
            IIdGeneratorService idGenerator)
        {
            _excelBaseService = excelBaseService;
            _accountRepository = accountRepository;
            _idGenerator = idGenerator;
        }

        public async Task<OperationResult<ImportAccountResponse>> Handle(ImportAccountCommand request, CancellationToken cancellationToken)
        {
            var response = new ImportAccountResponse();

            var excelResult = await _excelBaseService.ImportAsync<AccountExcelDTO>(request.File, null, cancellationToken);

            if (excelResult.Errors.Any())
            {
                response.FailureList.AddRange(excelResult.Errors.Select(err => new AccountPreviewDTO
                {
                    FullName = "Lỗi định dạng file",
                    Email = $"Dòng {err.RowIndex}",
                    Reason = err.Reason
                }));
            }

            var emailsInExcel = excelResult.SuccessItems
                .Select(x => x.Data.Email?.Trim())
                .Where(e => !string.IsNullOrEmpty(e))
                .Distinct()
                .ToList();

            var existingEmailsInDb = await _accountRepository.GetExistingEmailsAsync(emailsInExcel, cancellationToken);
            var existingDbEmailsSet = new HashSet<string>(existingEmailsInDb, StringComparer.OrdinalIgnoreCase);
            var processedEmailsInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var validItemsToProcess = new List<(AccountExcelDTO Data, int RowIndex)>();

            foreach (var successDetail in excelResult.SuccessItems)
            {
                var item = successDetail.Data;
                int rowIndex = successDetail.RowIndex;
                string currentEmail = item.Email?.Trim();

                if (string.IsNullOrWhiteSpace(item.FullName) || string.IsNullOrWhiteSpace(currentEmail) ||
                    item.Role == null || string.IsNullOrWhiteSpace(item.Password))
                {
                    response.FailureList.Add(new AccountPreviewDTO
                    {
                        Email = currentEmail ?? "N/A",
                        FullName = item.FullName ?? "N/A",
                        Reason = $"Dòng {rowIndex}: Thiếu thông tin bắt buộc (Họ tên, Email, Role hoặc Password)."
                    });
                    continue;
                }

                if (!processedEmailsInFile.Add(currentEmail))
                {
                    response.FailureList.Add(new AccountPreviewDTO
                    {
                        Email = currentEmail,
                        FullName = item.FullName,
                        Reason = $"Dòng {rowIndex}: Email này bị lặp lại trong file Excel."
                    });
                    continue;
                }

                if (existingDbEmailsSet.Contains(currentEmail))
                {
                    response.FailureList.Add(new AccountPreviewDTO
                    {
                        Email = currentEmail,
                        FullName = item.FullName,
                        Reason = $"Dòng {rowIndex}: Email đã tồn tại trên hệ thống."
                    });
                    continue;
                }

                validItemsToProcess.Add((item, rowIndex));
            }

            var newAccounts = new Account[validItemsToProcess.Count];
            Parallel.For(0, validItemsToProcess.Count, i =>
            {
                var (item, rowIndex) = validItemsToProcess[i];
                newAccounts[i] = new Account
                {
                    UserId = _idGenerator.Generate(15),
                    FullName = item.FullName,
                    Email = item.Email.Trim(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(item.Password),
                    Role = item.Role.Value,
                    DateOfBirth = item.DateOfBirth,
                    PhoneNumber = item.PhoneNumber,
                    Status = AccountStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };
            });

            if (newAccounts.Length > 0)
            {
                try
                {
                    await _accountRepository.AddRangeAsync(newAccounts, cancellationToken);
                    await _accountRepository.SaveChangesAsync(cancellationToken);

                    response.SuccessList.AddRange(newAccounts.Select(a => new AccountPreviewDTO
                    {
                        Email = a.Email,
                        FullName = a.FullName,
                        Reason = "Hợp lệ và đã nhập thành công"
                    }));
                }
                catch (Exception ex)
                {
                    return OperationResult<ImportAccountResponse>.Failure(new Error("DB_ERROR", "Lỗi lưu dữ liệu: " + ex.Message));
                }
            }

            var summaryMsg = $"Import hoàn tất. Thành công: {newAccounts.Length}, Thất bại: {response.FailureList.Count}";
            return OperationResult<ImportAccountResponse>.Success(response, 200, summaryMsg);
        }
    }
}