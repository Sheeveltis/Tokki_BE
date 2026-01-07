using MediatR;
using Microsoft.AspNetCore.Http; // Cần cho IFormFile
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.Cloudinary.Commands.UploadImage;
using Tokki.Application.UseCases.Cloudinary.Commands.UploadVocabularyImageByUrl;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/cloudinary")]
    [ApiController]
    public class CloudinaryController : ControllerBase
    {
        private readonly ISender _sender;

        public CloudinaryController(ISender sender)
        {
            _sender = sender;
        }

        [HttpPost("vocabulary-image")]
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
        [HttpPost("topic-image")]
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
        [HttpPost("template-part-image")]
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
        [HttpPost("avatar")]
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
        [HttpPost("vocabulary-image/url")]
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

    }
}