using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Excel.DTOs;

namespace Tokki.Application.UseCases.Excel.Commands.ImportQuestionTypes
{
    public class ImportQuestionTypesCommand : IRequest<OperationResult<ImportQuestionTypeResponse>>
    {
        public IFormFile File { get; set; }
        public ImportQuestionTypesCommand(IFormFile file) { File = file; }
    }
}
