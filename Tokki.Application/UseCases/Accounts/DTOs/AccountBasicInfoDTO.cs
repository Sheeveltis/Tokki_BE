using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Accounts.DTOs
{
    public class AccountBasicInfoDTO
    {
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? CurrentTitleName { get; set; }
        public string? CurrentColorHexTitle { get; set; }
        public string? TitleIconUrl { get; set; }
    }
}
