namespace Tokki.Application.UseCases.Roadmap.DTOs
{
    public class QuestionTypeMenuDto
    {
        public string QuestionTypeId { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Skill { get; set; } = string.Empty;
    }
}
