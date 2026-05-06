using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Excel.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Alphabet.Commands.ImportAlphabetFromExcel
{
    public class ImportAlphabetFromExcelCommandHandler : IRequestHandler<ImportAlphabetFromExcelCommand, OperationResult<AlphabetImportResponse>>
    {
        private readonly IAlphabetRepository _alphabetRepo;
        private readonly IExcelService _excelService;

        public ImportAlphabetFromExcelCommandHandler(IAlphabetRepository alphabetRepo, IExcelService excelService)
        {
            _alphabetRepo = alphabetRepo;
            _excelService = excelService;
        }

        public async Task<OperationResult<AlphabetImportResponse>> Handle(ImportAlphabetFromExcelCommand request, CancellationToken cancellationToken)
        {
            var response = new AlphabetImportResponse();
            try
            {
                var excelData = await _excelService.ExtractAlphabetDataAsync(request.File);
                if (excelData == null || !excelData.Any())
                {
                    return OperationResult<AlphabetImportResponse>.Failure(new Error("FILE_EMPTY", "File Excel không có dữ liệu."));
                }

                var existingData = await _alphabetRepo.GetAllAsync();
                var lettersInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // --- BƯỚC 1: KIỂM TRA LỖI TRƯỚC (Atomic validation) ---
                foreach (var item in excelData)
                {
                    if (string.IsNullOrWhiteSpace(item.Letter))
                    {
                        response.FailureList.Add(new AlphabetPreviewDTO
                        {
                            Letter = "TRỐNG",
                            Reason = "Ký tự (Letter) không được để trống."
                        });
                        continue;
                    }

                    if (lettersInFile.Contains(item.Letter))
                    {
                        response.FailureList.Add(new AlphabetPreviewDTO
                        {
                            Letter = item.Letter,
                            Reason = "Ký tự bị lặp lại trong file Excel."
                        });
                    }
                    else
                    {
                        lettersInFile.Add(item.Letter);
                    }

                    // Validate Type
                    if (string.IsNullOrEmpty(item.Type) || !TryParseAlphabetType(item.Type, out _))
                    {
                        response.FailureList.Add(new AlphabetPreviewDTO
                        {
                            Letter = item.Letter,
                            Type = item.Type,
                            Reason = $"Loại (Type) '{item.Type}' không hợp lệ (Phải là 'Vowel', 'Consonant', '1' hoặc '2')."
                        });
                    }
                }

                if (response.FailureList.Any())
                {
                    var errorMsg = $"Phát hiện {response.FailureList.Count} dòng lỗi. Import đã bị dừng lại.";
                    return OperationResult<AlphabetImportResponse>.Success(response, 400, errorMsg);
                }

                // --- BƯỚC 2: THỰC THI ---
                var entitiesToAdd = new List<AlphabetData>();

                foreach (var item in excelData)
                {
                    var existing = existingData.FirstOrDefault(x => x.Letter.Equals(item.Letter, StringComparison.OrdinalIgnoreCase));
                    TryParseAlphabetType(item.Type!, out var type);

                    if (existing != null)
                    {
                        // Update
                        existing.Meaning = item.Meaning;
                        existing.Pronunciation = item.Pronunciation;
                        existing.Type = type;
                        existing.AudioUrl = item.AudioUrl;
                        existing.DisplayDataJson = item.DisplayDataJson;
                        existing.ValidationDataJson = item.ValidationDataJson;
                        existing.TotalStrokes = item.TotalStrokes;
                        existing.SortOrder = item.SortOrder;
                        existing.UpdatedAt = DateTime.UtcNow.AddHours(7);

                        response.UpdateList.Add(new AlphabetPreviewDTO
                        {
                            Letter = item.Letter,
                            Type = type.ToString(),
                            Reason = "Cập nhật thành công."
                        });
                        
                        await _alphabetRepo.UpdateAsync(existing);
                    }
                    else
                    {
                        // Add new
                        var newEntity = new AlphabetData
                        {
                            Letter = item.Letter!,
                            Meaning = item.Meaning,
                            Pronunciation = item.Pronunciation,
                            Type = type,
                            AudioUrl = item.AudioUrl,
                            DisplayDataJson = item.DisplayDataJson,
                            ValidationDataJson = item.ValidationDataJson,
                            TotalStrokes = item.TotalStrokes,
                            SortOrder = item.SortOrder,
                            CreatedAt = DateTime.UtcNow.AddHours(7),
                            UpdatedAt = DateTime.UtcNow.AddHours(7)
                        };
                        entitiesToAdd.Add(newEntity);

                        response.SuccessList.Add(new AlphabetPreviewDTO
                        {
                            Letter = item.Letter,
                            Type = type.ToString(),
                            Reason = "Thêm mới thành công."
                        });
                    }
                }

                if (entitiesToAdd.Any())
                {
                    await _alphabetRepo.AddRangeAsync(entitiesToAdd);
                }

                await _alphabetRepo.SaveChangesAsync(cancellationToken);

                var msg = $"Xử lý hoàn tất. Thêm mới: {response.SuccessList.Count}, Cập nhật: {response.UpdateList.Count}.";
                return OperationResult<AlphabetImportResponse>.Success(response, 200, msg);
            }
            catch (Exception ex)
            {
                return OperationResult<AlphabetImportResponse>.Failure(new Error("IMPORT_ERROR", $"Lỗi hệ thống: {ex.Message}"));
            }
        }

        private bool TryParseAlphabetType(string input, out AlphabetType type)
        {
            if (Enum.TryParse<AlphabetType>(input, true, out type))
            {
                return true;
            }

            if (input == "1")
            {
                type = AlphabetType.Vowel;
                return true;
            }
            if (input == "2")
            {
                type = AlphabetType.Consonant;
                return true;
            }

            return false;
        }
    }
}
