using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Excel.Queries.TemplateQuestionType
{
    public class GetQuestionTypeTemplateQuery : IRequest<OperationResult<(byte[] FileBytes, string FileName)>>
    {
    }
}
