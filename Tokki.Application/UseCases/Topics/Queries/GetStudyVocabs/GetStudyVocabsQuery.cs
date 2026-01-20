using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Topics.DTOs;
using Tokki.Application.UseCases.Vocabulary.DTOs;

namespace Tokki.Application.UseCases.Topics.Queries.GetStudyVocabs
{
    public class GetStudyVocabsQuery : IRequest<OperationResult<List<VocabBasicInfoDTO>>>
    {
        [JsonIgnore]
        public string UserId { get; set; } = string.Empty; 
        public string TopicId { get; set; } = string.Empty;
        public int Count { get; set; } = 10; 
    }
}
