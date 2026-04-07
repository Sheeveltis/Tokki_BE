namespace Tokki.Application.UseCases.Excel.DTOs
 {
    public class SystemConfigExcelDTO
    {
        public string Key { get; set; } = default!;
        public string? Value { get; set; }
        public string? Description { get; set; }
        public string? DataType { get; set; }
        public string? ConfigType { get; set; }
    }
 }
