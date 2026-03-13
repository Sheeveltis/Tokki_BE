using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.MiniGame.DTOs;

namespace Tokki.Application.UseCases.MiniGame.Queries.Wordle
{
    public class GetTopWordleSentencesQuery : IRequest<OperationResult<List<WordleSentenceDto>>>
    {
        public string DailyWordleId { get; set; } = string.Empty;
        [JsonIgnore]
        public string? CurrentUserId { get; set; } 
        public int Top { get; set; } = 20;
    }
}
