namespace Tokki.Application.UseCases.Roadmap.DTOs
{
    public class VirtualQuizQuestionViewModel
    {
        public string QuestionId { get; set; }
        public string? Content { get; set; }
        public string? MediaUrl { get; set; }
        public string? PassageContent { get; set; }
        public List<VirtualQuizOptionViewModel> Options { get; set; } = new();
    }

    public class VirtualQuizOptionViewModel
    {
        public string OptionId { get; set; }
        public string KeyOption { get; set; }
        public string? Content { get; set; }
        public string? ImageUrl { get; set; }
    }
}