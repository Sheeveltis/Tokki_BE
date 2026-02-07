using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Vocabulary.DTOs;

namespace Tokki.Application.UseCases.Vocabulary.Queries.GetById
{
    public class GetVocabularyDetailByIdQuery
       : IRequest<OperationResult<VocabularyDetailDto>>
    {
        public string VocabularyId { get; set; } = string.Empty;
    }
}
