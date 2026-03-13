using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Excel.DTOs
{
    public class ImportAccountResponse
    {
        public List<AccountPreviewDTO> SuccessList { get; set; } = new();
        public List<AccountPreviewDTO> FailureList { get; set; } = new();
    }

    public class AccountPreviewDTO
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}
