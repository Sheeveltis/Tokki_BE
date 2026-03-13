using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Infrastructure.Configurations
{
    public class GeminiOptions
    {
        public GeminiConfig Pronunciation { get; set; } = new();
        public GeminiConfig Wordle { get; set; } = new();
        public GeminiConfig Writing { get; set; } = new();
        public GeminiConfig Roadmap { get; set; } = new();
    }

    public class GeminiConfig
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
    }
}
