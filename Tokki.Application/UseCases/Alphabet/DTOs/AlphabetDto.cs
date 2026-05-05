namespace Tokki.Application.UseCases.Alphabet.DTOs
{
    public class AlphabetDto
    {
        public int Id { get; set; }
        public string Letter { get; set; } = string.Empty;
        public string? Meaning { get; set; }
        public string? Pronunciation { get; set; }
        public int Type { get; set; }
        public int TotalStrokes { get; set; }
        public string? AudioUrl { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class AlphabetDetailDto : AlphabetDto
    {
        public string? DisplayDataJson { get; set; }
        public string? ValidationDataJson { get; set; }
    }
}
