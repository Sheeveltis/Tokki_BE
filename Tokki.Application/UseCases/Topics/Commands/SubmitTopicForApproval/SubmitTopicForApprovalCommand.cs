using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Topics.Commands.SubmitTopicForApproval
{
    public class SubmitTopicForApprovalCommand : IRequest<OperationResult<bool>>
    {
        public string TopicId { get; set; } = string.Empty;
    }
}
