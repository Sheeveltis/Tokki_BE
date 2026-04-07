using System.Collections.Generic;
 
namespace Tokki.Application.UseCases.Excel.DTOs
 {
    public class SystemConfigPreviewDTO
    {
        public string Key { get; set; } = default!;
        public string? Value { get; set; }
        public string? Reason { get; set; }
    }
 
    public class SystemConfigImportResponse
    {
        public List<SystemConfigPreviewDTO> SuccessList { get; set; } = new();
        public List<SystemConfigPreviewDTO> UpdateList { get; set; } = new();
        public List<SystemConfigPreviewDTO> FailureList { get; set; } = new();
    }
 }
