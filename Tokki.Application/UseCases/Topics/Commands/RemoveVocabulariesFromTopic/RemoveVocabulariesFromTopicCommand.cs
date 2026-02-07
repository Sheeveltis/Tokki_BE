using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Topics.Commands.RemoveVocabulariesFromTopic
{
    public class RemoveVocabulariesFromTopicCommand : IRequest<OperationResult<int>>
    {
        public string TopicId { get; set; } = string.Empty;
        public List<string> VocabularyIds { get; set; } = new();
    }
}
