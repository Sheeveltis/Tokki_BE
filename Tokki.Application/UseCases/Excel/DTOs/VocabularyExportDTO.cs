using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.Excel.DTOs
{
    public class VocabularyExportDTO
    {
        public string Text { get; set; }
        public string Pronunciation { get; set; }
        public string ImgURL { get; set; }
        public string Definition { get; set; }
    }

    public class ExportFileDTO
    {
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public byte[] FileContent { get; set; }
    }
}
