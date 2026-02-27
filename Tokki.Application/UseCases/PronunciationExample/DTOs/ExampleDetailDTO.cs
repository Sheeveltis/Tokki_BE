using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.PronunciationExample.DTOs
{
    public class ExampleDetailDTO
    {
        public string ExampleId { get; set; } = string.Empty;
        public string TargetScript { get; set; } = string.Empty;
        public string RawScript { get; set; } = string.Empty;
        public string PhoneticScript { get; set; } = string.Empty;
        public string Meaning { get; set; } = string.Empty;
        public string AudioUrl { get; set; } = string.Empty;

        public string RuleName { get; set; } = string.Empty;
        public string RuleDescription { get; set; } = string.Empty;
        public string RuleContent { get; set; } = string.Empty;
    }
}
