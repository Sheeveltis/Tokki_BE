using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Word.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Word.Queries.GetWordsByTopicQuery
{
    public class GetWordsByTopicQuery : IRequest<OperationResult<PagedResult<WordWithMeaningsDto>>>
    {
        public string TopicId { get; set; } = string.Empty;
        public WordStatus? Status { get; set; }
        public string? SearchTerm { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
