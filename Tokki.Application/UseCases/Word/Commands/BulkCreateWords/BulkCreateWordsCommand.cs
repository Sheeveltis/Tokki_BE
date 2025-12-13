using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Word.DTOs;

namespace Tokki.Application.UseCases.Word.Commands.BulkCreateWords
{
    public class BulkCreateWordsCommand : IRequest<OperationResult<BulkCreateWordsResponse>>
    {
        public string TopicId { get; set; }
        public List<WordCreateDto> Words { get; set; } = new();
    }
}
