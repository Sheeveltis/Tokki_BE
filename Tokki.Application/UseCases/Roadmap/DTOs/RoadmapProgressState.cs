namespace Tokki.Application.UseCases.Roadmap.DTOs
{
    public class RoadmapProgressState
    {
        public string JobId { get; set; } = string.Empty;
        public int Percent { get; set; }
        public string Step { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public bool IsError { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RoadmapId { get; set; }
    }
}