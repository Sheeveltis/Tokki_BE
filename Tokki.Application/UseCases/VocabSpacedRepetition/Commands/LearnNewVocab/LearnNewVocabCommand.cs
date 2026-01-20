using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.VocabSpacedRepetition.DTOs;

namespace Tokki.Application.UseCases.VocabSpacedRepetition.Commands.LearnNewVocab
{
    public class LearnNewVocabCommand : IRequest<OperationResult<ReviewResponse>>
    {
        [JsonIgnore]
        public string UserId { get; set; } = string.Empty;

        public string VocabularyId { get; set; } = string.Empty;
    }
}
