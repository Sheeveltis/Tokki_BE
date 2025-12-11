using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Topics.DTOs;

namespace Tokki.Application.UseCases.Topics.Queries.GetById
{
    public class GetTopicByIdQuery : IRequest<OperationResult<TopicDto>>
    {
        public string TopicId { get; set; } = string.Empty;
    }
}
