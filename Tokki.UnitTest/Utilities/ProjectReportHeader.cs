using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.UnitTest.Utilities
{
    public class ProjectReportHeader
    {
        public string ProjectName { get; set; } = string.Empty;
        public string ProjectCode { get; set; } = string.Empty;
        public string Creator { get; set; } = string.Empty;
        public string Version { get; set; } = "v1.0";
        public int NormalTestCasesPerKLOC { get; set; } = 30; // Giả sử khách hàng yêu cầu 50 TCs / 1000 LOC
        public string Executor { get; set; } = "QA Automation";
    }
}
