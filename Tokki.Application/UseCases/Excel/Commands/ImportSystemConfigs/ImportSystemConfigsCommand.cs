using MediatR;
using Microsoft.AspNetCore.Http;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Excel.DTOs;
 
namespace Tokki.Application.UseCases.Excel.Commands.ImportSystemConfigs
 {
    public class ImportSystemConfigsCommand : IRequest<OperationResult<SystemConfigImportResponse>>
    {
        public IFormFile File { get; set; } = default!;
    }
 }
