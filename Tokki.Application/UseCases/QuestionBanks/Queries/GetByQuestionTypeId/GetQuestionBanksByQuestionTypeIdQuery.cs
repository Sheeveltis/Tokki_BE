using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.QuestionBanks.DTOs;

namespace Tokki.Application.UseCases.QuestionBanks.Queries.GetByQuestionTypeId
{
    public class GetQuestionBanksByQuestionTypeIdQuery
       : IRequest<OperationResult<List<QuestionBankByQuestionTypeDto>>>
    {
        public string QuestionTypeId { get; set; } = string.Empty;

        // cho phép filter active/inactive, nếu null thì lấy tất cả
        public bool? IsActive { get; set; } = true;
    }
}
