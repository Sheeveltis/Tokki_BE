using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Excel.DTOs
{
    public class ImportExampleResponse
    {
        public List<ExamplePreviewDTO> SuccessList { get; set; } = new();
        public List<ExamplePreviewDTO> FailureList { get; set; } = new();
    }

    public class ExamplePreviewDTO
    {
        public string TargetScript { get; set; } = string.Empty;
        public string Meaning { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}
