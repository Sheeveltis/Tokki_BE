using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.SystemConfigs.DTOs
{
    public class SystemConfigDto
    {
        public string Key { get; set; } = string.Empty;
        public string? Value { get; set; }
        public string? Description { get; set; }
        public string? DataType { get; set; }
        public bool IsActive { get; set; }
    }
}
