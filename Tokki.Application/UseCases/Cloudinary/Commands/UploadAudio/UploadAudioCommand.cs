using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Cloudinary.Commands.UploadAudio
{
    public class UploadAudioCommand : IRequest<OperationResult<string>>
    {
        public IFormFile AudioFile { get; set; }
        public string FolderName { get; set; }
    }
}
