namespace Tokki.Application.UseCases.Roadmap.DTOs
{
    public class GrammarMenuItem
    {
        public string GrammarId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Syntaxes { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? RelatedQuestionTypeId { get; set; }
    }
}
