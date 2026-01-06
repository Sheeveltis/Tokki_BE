using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.ActivateQuestionBanks
{
    public class ActivateQuestionBanksCommand : IRequest<OperationResult<int>>
    {
        public List<string> QuestionBankIds { get; set; } = new();
    }
}
