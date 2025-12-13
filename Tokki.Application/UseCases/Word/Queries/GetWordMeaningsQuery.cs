using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Word.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Word.Queries
{

    public class GetWordMeaningsQuery : IRequest<OperationResult<PagedResult<MeaningDto>>>
    {
        public string? WordId { get; set; }
        public string? Text { get; set; }
        public string? TopicId { get; set; }
        public MeaningStatus? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
