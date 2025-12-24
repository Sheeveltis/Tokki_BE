using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Vocabulary.DTOs
{
    public class FlashCardDto
    {         
            public string Text { get; set; } = string.Empty;
            public string Definition { get; set; } = string.Empty;
            public string? ImgURL { get; set; }
            public string? AudioUrl { get; set; }

    }
}
