using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.MiniGame.DTOs;

namespace Tokki.Application.UseCases.MiniGame.Commands.SubmitWordleGuess
{
    public class SubmitWordleGuessCommand : IRequest<OperationResult<GuessResultDTO>>
    {
        public string DailyWordleId { get; set; }
        public string GuessWord { get; set; }

        [JsonIgnore]
        public string UserId { get; set; }
    }
}
