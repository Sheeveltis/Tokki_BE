using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Word.DTOs
{
    public class MeaningDto
    {
        public string MeaningId { get; set; } = string.Empty;
        public string WordId { get; set; } = string.Empty;
        public string WordText { get; set; } = string.Empty;
        public string? Pronunciation { get; set; }
        public string? AudioURL { get; set; }
        public string Definition { get; set; } = string.Empty;
        public string? ExampleSentence { get; set; }
        public string? ImgURL { get; set; }
        public List<TopicInfoDto> Topics { get; set; } = new();
        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; } = string.Empty;
        public DateTime? UpdateDate { get; set; }
        public string? UpdateBy { get; set; }
        public MeaningStatus Status { get; set; }
    }
}
