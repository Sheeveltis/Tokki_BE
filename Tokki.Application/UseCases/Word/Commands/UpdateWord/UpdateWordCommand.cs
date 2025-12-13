using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Word.DTOs;

namespace Tokki.Application.UseCases.Word.Commands.UpdateWord
{

    public class UpdateWordCommand : IRequest<OperationResult<WordResponseDto>>
    {
        public string WordId { get; set; } = string.Empty;
        public string? Text { get; set; }
        public string? Pronunciation { get; set; }
        public List<MeaningUpdateDto>? Meanings { get; set; }
    }
}
