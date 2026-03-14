using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.ExcelCore;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Excel.DTOs
{
    public class AccountExcelDTO
    {
        [ExcelColumn("Họ và Tên", Order = 1)]
        public string FullName { get; set; } = string.Empty;

        [ExcelColumn("Email", Order = 2)]
        public string Email { get; set; } = string.Empty;

        [ExcelColumn("Mật khẩu", Order = 3)]
        public string Password { get; set; } = string.Empty;

        [ExcelColumn("Vai trò (Admin/User)", Order = 4)]
        public AccountRole? Role { get; set; }

        [ExcelColumn("Ngày sinh", Order = 5)]
        public DateTime? DateOfBirth { get; set; }

        [ExcelColumn("Số điện thoại", Order = 6)]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
