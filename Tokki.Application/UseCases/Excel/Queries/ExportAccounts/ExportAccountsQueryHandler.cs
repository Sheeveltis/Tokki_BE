using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.ExcelCore;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Excel.DTOs;

namespace Tokki.Application.UseCases.Excel.Queries.ExportAccounts
{
    public class ExportAccountsQueryHandler : IRequestHandler<ExportAccountsQuery, OperationResult<(byte[] FileBytes, string FileName)>>
    {
        private readonly IExcelBaseService _excelBaseService;
        private readonly IAccountRepository _accountRepository;

        public ExportAccountsQueryHandler(IExcelBaseService excelBaseService, IAccountRepository accountRepository)
        {
            _excelBaseService = excelBaseService;
            _accountRepository = accountRepository;
        }

        public async Task<OperationResult<(byte[] FileBytes, string FileName)>> Handle(ExportAccountsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var accounts = await _accountRepository.GetAllAsync(cancellationToken);

                if (accounts == null)
                {
                    return OperationResult<(byte[], string)>.Failure(new Error("DATA_NULL", "Danh sách tài khoản trống."));
                }

                var exportData = accounts.Select(a => new AccountExcelDTO
                {
                    FullName = a.FullName ?? "N/A",
                    Email = a.Email ?? "N/A",
                    Role = a.Role,
                    DateOfBirth = a.DateOfBirth,
                    PhoneNumber = a.PhoneNumber ?? "N/A"
                }).ToList();

                byte[] fileBytes = await _excelBaseService.ExportAsync(
                    exportData,
                    "Danh_Sach_TK",
                    ignoredColumns: new List<string> { nameof(AccountExcelDTO.Password) }
                );

                string fileName = $"Accounts_Export_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

                return OperationResult<(byte[], string)>.Success((fileBytes, fileName), 200, "Xuất file thành công.");
            }
            catch (Exception ex)
            {
                return OperationResult<(byte[], string)>.Failure(new Error("EXPORT_EXCEPTION", $"Lỗi: {ex.Message}"));
            }
        }
    }
}