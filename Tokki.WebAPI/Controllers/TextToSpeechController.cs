using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.TextToSpeech.Commands.GenerateVocabularyAudioUrl;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/tts")]
    [ApiController]
    public class TextToSpeechController : ControllerBase
    {
        private readonly ISender _sender;

        public TextToSpeechController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("vocabulary-audio-url")]
        public async Task<IActionResult> GenerateVocabularyAudioUrl([FromBody] GenerateVocabularyAudioUrlCommand command)
        {
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
    }
}
