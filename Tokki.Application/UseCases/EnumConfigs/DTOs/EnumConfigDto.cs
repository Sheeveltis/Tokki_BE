namespace Tokki.Application.UseCases.EnumConfigs.DTOs
{
    public class EnumConfigDto
    {
        public string Key { get; set; } = string.Empty;
        public int Value { get; set; }
        public string Label { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SortOrder { get; set; }
    }
}
