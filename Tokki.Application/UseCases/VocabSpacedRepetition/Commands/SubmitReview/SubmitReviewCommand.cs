using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;
using Tokki.Application.UseCases.VocabSpacedRepetition.DTOs;
using System.Text.Json.Serialization;
namespace Tokki.Application.UseCases.VocabSpacedRepetition.Commands.SubmitReview
{
    public class SubmitReviewCommand : IRequest<OperationResult<ReviewResponse>>
    {
        [JsonIgnore]
        public string UserId { get; set; } = string.Empty;

        public string VocabularyId { get; set; } = string.Empty;

        public QualityVocab Quality { get; set; }
    }
}
