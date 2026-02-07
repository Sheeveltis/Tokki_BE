using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Excel.DTOs;

namespace Tokki.Application.UseCases.Excel.Commands.ImportQuestionsFromExcel
{
    public class ImportQuestionsFromExcelCommand : IRequest<OperationResult<ImportQuestionsResponse>>
    {
        public string QuestionTypeId { get; set; } = string.Empty;
        public IFormFile ExcelFile { get; set; }
    }
}
