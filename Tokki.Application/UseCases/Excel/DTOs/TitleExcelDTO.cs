using Tokki.Application.Common.ExcelCore;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Excel.DTOs
{
    public class TitleExcelDTO
    {
        [ExcelColumn("Tên danh hiệu")]
        public string Name { get; set; } = string.Empty;

        [ExcelColumn("Mô tả")]
        public string? Description { get; set; }

        [ExcelColumn("Mã màu (HEX)")]
        public string ColorHex { get; set; } = "#000000";

        [ExcelColumn("URL Icon")]
        public string? IconUrl { get; set; }

        [ExcelColumn("Loại điều kiện")]
        public string RequirementType { get; set; } = "Level";

        [ExcelColumn("Giá trị điều kiện")]
        public long RequirementQuantity { get; set; }

        [ExcelColumn("Trạng thái")]
        public string Status { get; set; } = "Active";
    }
}
