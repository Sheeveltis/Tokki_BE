using MediatR;
using Microsoft.AspNetCore.Http; // Cần cho IFormFile
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.Cloudinary.Commands.UploadImage;
using Tokki.Application.UseCases.Cloudinary.Commands.UploadVocabularyImageByUrl;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CloudinaryController : ControllerBase
    {
        private readonly ISender _sender;

        public CloudinaryController(ISender sender)
        {
            _sender = sender;
        }
        [HttpPost("image/question")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadQuestionImage(IFormFile file)
        {
            var command = new UploadImageCommand
            {
                File = file,
                FolderName = "tokki/image/question"
            };
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
        [HttpPost("image/option")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadOptionImage(IFormFile file)
        {
            var command = new UploadImageCommand
            {
                File = file,
                FolderName = "tokki/image/option"
            };
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
        [HttpPost("image/passage")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadPassageImage(IFormFile file)
        {
            var command = new UploadImageCommand
            {
                File = file,
                FolderName = "tokki/image/passage"
            };
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
        [HttpPost("image/vocabulary")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadVocabularyImage(IFormFile file)
        {
            var command = new UploadImageCommand
            {
                File = file,
                FolderName = "tokki/vocab-image"
            };
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
        [HttpPost("image/vocabulary/url")]
        public async Task<IActionResult> TestUpload(string ImgUrl)
        {
            try
            {
                var command = new UploadVocabularyImageByUrlCommand
                {
                    ImageUrl = ImgUrl
                };
                var result = await _sender.Send(command);
                return StatusCode(result.StatusCode, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.ToString());
            }
        }
        [HttpPost("image/topic")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadTopicImage(IFormFile file)
        {
            var command = new UploadImageCommand
            {
                File = file,
                FolderName = "tokki/topic-image"
            };
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
        [HttpPost("image/template-part")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadTemplatePartImage(IFormFile file)
        {
            var command = new UploadImageCommand
            {
                File = file,
                FolderName = "tokki/template-parts"
            };
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
        [HttpPost("image/avatar")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            var command = new UploadImageCommand
            {
                File = file,
                FolderName = "tokki/avatar"
            };
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
        
        [HttpPost("audio/question")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadQuestionAudio(IFormFile file)
        {
            var command = new Tokki.Application.UseCases.Cloudinary.Commands.UploadAudio.UploadAudioCommand
            {
                AudioFile = file,
                FolderName = "tokki/audio/question"
            };
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
        [HttpPost("audio/option")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadOptionAudio(IFormFile file)
        {
            var command = new Tokki.Application.UseCases.Cloudinary.Commands.UploadAudio.UploadAudioCommand
            {
                AudioFile = file,
                FolderName = "tokki/audio/option"
            };
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
        [HttpPost("audio/passage")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadPassageAudio(IFormFile file)
        {
            var command = new Tokki.Application.UseCases.Cloudinary.Commands.UploadAudio.UploadAudioCommand
            {
                AudioFile = file,
                FolderName = "tokki/audio/passage"
            };
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
    }
}