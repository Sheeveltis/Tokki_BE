using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Excel.DTOs;

namespace Tokki.Application.UseCases.Excel.Commands.ImportPronunciationExample
{
    public class ImportPronunciationExampleCommand : IRequest<OperationResult<ImportExampleResponse>>
    {
        public IFormFile File { get; set; } = null!;
        public string PronunciationRuleId { get; set; } = string.Empty;
        [JsonIgnore]
        public string? UserId { get; set; } 
    }
}
