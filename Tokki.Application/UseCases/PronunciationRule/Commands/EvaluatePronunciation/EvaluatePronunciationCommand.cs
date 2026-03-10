using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.PronunciationRule.DTOs;

namespace Tokki.Application.UseCases.PronunciationRule.Commands.EvaluatePronunciation
{
    public class EvaluatePronunciationCommand : IRequest<OperationResult<PronunciationResponse>>
    {
        public IFormFile AudioFile { get; set; } = null!;

        public string ExampleId { get; set; } = string.Empty;
    }
}
