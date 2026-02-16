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
    public class GetDailyWordleStatusQuery : IRequest<OperationResult<WordleDashboardDTO>>
    {
        [JsonIgnore]
        public string UserId { get; set; }
    }
}
