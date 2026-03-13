using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.ExcelCore;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Excel.DTOs
{
    public class QuestionTypeExcelDTO
    {
        [ExcelColumn("Code", Order = 1)]
        public string Code { get; set; } = string.Empty;

        [ExcelColumn("Tên Loại Câu Hỏi", Order = 2)]
        public string Name { get; set; } = string.Empty;
       
        [ExcelColumn("Mô Tả", Order = 3)]
        public string? Description { get; set; }
        [ExcelColumn("TOPIK", Order = 4)]
        public string ExamType { get; set; }

        [ExcelColumn("Kỹ Năng", Order = 5)]
        public string Skill { get; set; }

        [ExcelColumn("Độ Khó", Order = 6)]
        public string Difficulty { get; set; }
    }
}
