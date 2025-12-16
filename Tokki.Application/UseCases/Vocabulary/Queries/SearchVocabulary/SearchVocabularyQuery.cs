using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Vocabulary.DTOs;

namespace Tokki.Application.UseCases.Vocabulary.Queries.SearchVocabulary
{
    public class SearchVocabularyQuery : IRequest<OperationResult<PagedResult<VocabularySearchResultDto>>>
    {
        public string SearchTerm { get; set; } = string.Empty;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20; // Giới hạn 20 kết quả cho realtime
    }
}
