using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Vocabulary.DTOs;

namespace Tokki.Application.UseCases.Vocabulary.Commands.BulkCreateVocabulariesByStaff
{
    public class BulkCreateVocabulariesByStaffCommand
        : IRequest<OperationResult<BulkCreateVocabulariesResponse>>
    {
        public List<VocabularyCreateDto> Vocabularies { get; set; } = new();
    }
}
