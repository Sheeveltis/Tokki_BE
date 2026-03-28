using System;

namespace Tokki.Application.UseCases.Exam.DTOs
{
    public class ExportExamPdfResponse
    {
        public byte[] PdfData { get; set; } = Array.Empty<byte>();
        public string FileName { get; set; } = string.Empty;
    }
}
