using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Roadmap.DTOs
{
    public class CompleteTaskRequestDto
    {
        public string TaskId { get; set; }
        public string? Performance { get; set; }
    }
}
