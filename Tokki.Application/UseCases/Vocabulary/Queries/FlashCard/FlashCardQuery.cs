using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Vocabulary.Queries.FlashCard
{
    public class FlashCardQuery : IRequest<OperationResult<List<FlashCardDto>>>
    {
        public string TopicId { get; set; } = string.Empty;
    }
}