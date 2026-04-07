using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Excel.DTOs;

namespace Tokki.Application.UseCases.Excel.Queries.ExportVocabByTopic
{
    public class ExportVocabByTopicQueryHandler : IRequestHandler<ExportVocabByTopicQuery, OperationResult<ExportFileDTO>>
    {
        private readonly IVocabularyTopicRepository _vocabTopicRepo;
        private readonly ITopicRepository _topicRepo;
        private readonly IExcelService _excelService;

        public ExportVocabByTopicQueryHandler(IVocabularyTopicRepository vocabTopicRepo, ITopicRepository topicRepo,
            IExcelService excelService)
        {
            _vocabTopicRepo = vocabTopicRepo;
            _topicRepo = topicRepo;
            _excelService = excelService;
        }

        public async Task<OperationResult<ExportFileDTO>> Handle(ExportVocabByTopicQuery request, CancellationToken cancellationToken)
        {
            var topicName = await _topicRepo.GetTopicNameAsync(request.TopicId);
            if (string.IsNullOrEmpty(topicName))
            {
                return OperationResult<ExportFileDTO>.Failure(AppErrors.TopicNotFound);
            }

            var vocabs = await _vocabTopicRepo.GetVocabsByTopicIdAsync(request.TopicId);
            if (vocabs == null || !vocabs.Any())
            {
                return OperationResult<ExportFileDTO>.Failure(AppErrors.VocabTopicIsEmpty);
            }

            string safeSheetName = topicName.Length > 30 ? topicName.Substring(0, 30) : topicName;
            var fileContent = await _excelService.ExportVocabularyToExcelAsync(vocabs, safeSheetName);

            // 4. Trả về kết quả
            return OperationResult<ExportFileDTO>.Success(new ExportFileDTO
            {
                FileName = $"Tokki_Vocab_{topicName}_{DateTime.Now:ddMMyyyy}.xlsx", 
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileContent = fileContent
            });
        }
    }
}
