using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Passages.DTOs
{
    public class PassageDto
    {
        public string PassageId { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? ImageUrl { get; set; }
        public PassageStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public PassageMediaType MediaType { get; set; }
    }
}
