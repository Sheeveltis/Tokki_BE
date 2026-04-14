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
 
namespace Tokki.Application.UseCases.Excel.Queries.ExportSystemConfigs
 {
    public class ExportSystemConfigsQueryHandler : IRequestHandler<ExportSystemConfigsQuery, OperationResult<ExportFileDTO>>
    {
        private readonly ISystemConfigRepository _configRepo;
        private readonly IExcelService _excelService;
 
        public ExportSystemConfigsQueryHandler(ISystemConfigRepository configRepo, IExcelService excelService)
        {
            _configRepo = configRepo;
            _excelService = excelService;
        }
 
        public async Task<OperationResult<ExportFileDTO>> Handle(ExportSystemConfigsQuery request, CancellationToken cancellationToken)
        {
            var configs = await _configRepo.GetAllAsync();
 
            var exportData = configs.Select(c => new SystemConfigExcelDTO
            {
                Key = c.Key,
                Value = c.Value,
                Description = c.Description,
                DataType = c.DataType,
                ConfigType = c.ConfigType?.ToString()
            }).ToList();
 
            var excelBytes = await _excelService.ExportSystemConfigsToExcelAsync(exportData, "SystemConfigs");
 
            var response = new ExportFileDTO
            {
                FileContent = excelBytes,
                FileName = $"Tokki_SystemConfig_{DateTime.Now:ddMMyyyy}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };
 
            return OperationResult<ExportFileDTO>.Success(response);
        }
    }
 }
