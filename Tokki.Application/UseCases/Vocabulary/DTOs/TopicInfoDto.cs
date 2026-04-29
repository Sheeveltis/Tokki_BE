using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Vocabulary.DTOs
{
    public class TopicInfoDto
    {
        public string TopicId { get; set; } = string.Empty;
        public string TopicName { get; set; } = string.Empty;
        public int Level { get; set; }

    }
}
