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
 
namespace Tokki.Application.UseCases.Excel.Commands.ImportSystemConfigs
 {
    public class ImportSystemConfigsCommandHandler : IRequestHandler<ImportSystemConfigsCommand, OperationResult<SystemConfigImportResponse>>
    {
        private readonly ISystemConfigRepository _configRepo;
        private readonly IExcelService _excelService;
 
        public ImportSystemConfigsCommandHandler(ISystemConfigRepository configRepo, IExcelService excelService)
        {
            _configRepo = configRepo;
            _excelService = excelService;
        }
 
        public async Task<OperationResult<SystemConfigImportResponse>> Handle(ImportSystemConfigsCommand request, CancellationToken cancellationToken)
        {
            var response = new SystemConfigImportResponse();
            try
            {
                var excelData = await _excelService.ExtractSystemConfigDataAsync(request.File);
                if (excelData == null || !excelData.Any())
                {
                    return OperationResult<SystemConfigImportResponse>.Failure(new Error("FILE_EMPTY", "File Excel không có dữ liệu."));
                }
 
                var existingConfigs = await _configRepo.GetAllAsync();
                var keysInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                
                // --- BƯỚC 1: KIỂM TRA LỖI TRƯỚC (FIRST PASS) ---
                foreach (var item in excelData)
                {
                    if (string.IsNullOrWhiteSpace(item.Key))
                    {
                        response.FailureList.Add(new SystemConfigPreviewDTO 
                        { 
                            Key = "TRỐNG", 
                            Reason = "Key không được để trống." 
                        });
                        continue;
                    }
 
                    if (keysInFile.Contains(item.Key))
                    {
                        response.FailureList.Add(new SystemConfigPreviewDTO 
                        { 
                            Key = item.Key, 
                            Reason = "Key bị lặp lại trong file Excel." 
                        });
                    }
                    else
                    {
                        keysInFile.Add(item.Key);
                    }
 
                    // Validate ConfigType
                    if (!string.IsNullOrEmpty(item.ConfigType) && !Enum.TryParse<SystemConfigType>(item.ConfigType, true, out _))
                    {
                        response.FailureList.Add(new SystemConfigPreviewDTO 
                        { 
                            Key = item.Key, 
                            Value = item.Value,
                            Reason = $"Loại cấu hình '{item.ConfigType}' không hợp lệ." 
                        });
                    }
                }
 
                // Nếu có bất kỳ lỗi nào, dừng lại và trả về log lỗi (Atomic Rollback)
                if (response.FailureList.Any())
                {
                    var errorMsg = $"Phát hiện {response.FailureList.Count} dòng lỗi. Toàn bộ quá trình import đã bị dừng lại để đảm bảo tính toàn vẹn.";
                    return OperationResult<SystemConfigImportResponse>.Success(response, 400, errorMsg);
                }
 
                // --- BƯỚC 2: THỰC THI (SECOND PASS) ---
                var configsToAdd = new List<SystemConfig>();
 
                foreach (var item in excelData)
                {
                    var existing = existingConfigs.FirstOrDefault(x => x.Key.Equals(item.Key, StringComparison.OrdinalIgnoreCase));
                    
                    SystemConfigType? configType = null;
                    if (!string.IsNullOrEmpty(item.ConfigType))
                    {
                        Enum.TryParse<SystemConfigType>(item.ConfigType, true, out var parsedType);
                        configType = parsedType;
                    }
 
                    if (existing != null)
                    {
                        // Update
                        existing.Value = item.Value;
                        existing.Description = item.Description;
                        existing.DataType = item.DataType;
                        existing.ConfigType = configType;
                        existing.UpdatedAt = DateTime.UtcNow.AddHours(7);
 
                        response.UpdateList.Add(new SystemConfigPreviewDTO 
                        { 
                            Key = item.Key, 
                            Value = item.Value, 
                            Reason = "Cập nhật thành công." 
                        });
                    }
                    else
                    {
                        // Add new
                        var newConfig = new SystemConfig
                        {
                            Key = item.Key,
                            Value = item.Value,
                            Description = item.Description,
                            DataType = item.DataType,
                            ConfigType = configType,
                            CreatedAt = DateTime.UtcNow.AddHours(7)
                        };
                        configsToAdd.Add(newConfig);
 
                        response.SuccessList.Add(new SystemConfigPreviewDTO 
                        { 
                            Key = item.Key, 
                            Value = item.Value, 
                            Reason = "Thêm mới thành công." 
                        });
                    }
                }
 
                if (configsToAdd.Any())
                {
                    await _configRepo.AddRangeAsync(configsToAdd);
                }
 
                await _configRepo.SaveChangesAsync(cancellationToken);
                
                var msg = $"Xử lý hoàn tất. Thêm mới: {response.SuccessList.Count}, Cập nhật: {response.UpdateList.Count}.";
                return OperationResult<SystemConfigImportResponse>.Success(response, 200, msg);
            }
            catch (Exception ex)
            {
                return OperationResult<SystemConfigImportResponse>.Failure(new Error("IMPORT_ERROR", $"Lỗi: {ex.Message}"));
            }
        }
    }
 }
