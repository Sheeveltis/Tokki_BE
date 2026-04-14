using MediatR;
using System.Collections.Generic;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Excel.DTOs;

namespace Tokki.Application.UseCases.Vocabulary.Commands.AutoFindImages
{
    public class AutoFindVocabularyImagesCommand : IRequest<OperationResult<ExportFileDTO>>
    {
        /// <summary>
        /// Danh sách VocabularyId cần tìm ảnh
        /// </summary>
        public List<string> VocabularyIds { get; set; } = new();

        /// <summary>
        /// Có ghi đè ảnh cũ không (nếu từ vựng đã có ảnh)
        /// </summary>
        public bool OverwriteExisting { get; set; } = false;
    }
}
