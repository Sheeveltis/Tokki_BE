using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Vocabulary.DTOs;

namespace Tokki.Application.UseCases.Vocabulary.Commands.CreateVocabularyByStaff
{
    public class CreateVocabularyByStaffCommand
     : IRequest<OperationResult<VocabularyResponse>>
    {
        public string Text { get; set; } = string.Empty;
        public string? Pronunciation { get; set; }
        public string Definition { get; set; } = string.Empty;
        public List<VocabularyExampleDto>? Examples { get; set; }
        public string? ImgURL { get; set; }
    }
}
