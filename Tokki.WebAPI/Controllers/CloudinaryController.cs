using MediatR;
using Microsoft.AspNetCore.Http; // Cần cho IFormFile
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.Cloudinary.Commands.UploadTopicImage;
using Tokki.Application.UseCases.Cloudinary.Commands.UploadVocabularyImage;
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
            var command = new UploadVocabularyImageCommand
            {
                File = file
            };
            var result = await _sender.Send(command);
            return StatusCode(result.StatusCode, result);
        }
        [HttpPost("topic-image")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadTopicImage(IFormFile file)
        {
            var command = new UploadTopicImageCommand
            {
                File = file
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