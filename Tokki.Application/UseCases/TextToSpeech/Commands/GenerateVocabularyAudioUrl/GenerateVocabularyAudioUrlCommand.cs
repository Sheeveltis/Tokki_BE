using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.TextToSpeech.DTOs;

namespace Tokki.Application.UseCases.TextToSpeech.Commands.GenerateVocabularyAudioUrl
{
    public class GenerateVocabularyAudioUrlCommand
         : IRequest<OperationResult<TextToSpeechUrlResponse>>
    {
        public string Text { get; set; } = default!;
    }
}
