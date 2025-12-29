using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Excel.Commands.DTOs;

namespace Tokki.Application.UseCases.Excel.Commands.AddVocabByExcel
{
    public class AddVocabByExcelCommand : IRequest<OperationResult<ImportVocabularyResponse>>
    {
        public IFormFile File { get; set; }
        [JsonIgnore]
        public string StaffId { get; set; }
        public string? TopicId { get; set; }
    }
}
