using MediatR;
using System.Collections.Generic;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Vocabulary.DTOs;

namespace Tokki.Application.UseCases.Vocabulary.Commands.BulkCreateVocabularies
{
    
    public class BulkCreateVocabulariesCommand : IRequest<OperationResult<BulkCreateVocabulariesResponse>>
    {
        public List<VocabularyCreateDto> Vocabularies { get; set; } = new();
    }
}
