using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading; // Thêm cái này
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.DTOs;
using Tokki.Application.UseCases.Vocabulary.DTOs;

namespace Tokki.Application.UseCases.Topics.Queries.GetStudyVocabs
{
    public class GetStudyVocabsQueryHandler : IRequestHandler<GetStudyVocabsQuery, OperationResult<List<VocabBasicInfoDTO>>>
    {
        private readonly ITopicRepository _topicRepo;
        private readonly IUserVocabProgressRepository _progressRepo;

        // Đã XÓA dòng private readonly IMapper _mapper;

        public GetStudyVocabsQueryHandler(
            ITopicRepository topicRepo,
            IUserVocabProgressRepository progressRepo) // Đã XÓA tham số IMapper trong constructor
        {
            _topicRepo = topicRepo;
            _progressRepo = progressRepo;
        }

        public async Task<OperationResult<List<VocabBasicInfoDTO>>> Handle(GetStudyVocabsQuery request, CancellationToken cancellationToken)
        {
            // 1. Lấy tất cả từ vựng
            var allVocabs = await _topicRepo.GetVocabulariesByTopicIdAsync(request.TopicId);

            if (allVocabs == null || !allVocabs.Any())
            {
                return OperationResult<List<VocabBasicInfoDTO>>.Failure("Topic này chưa có từ vựng nào.", 404);
            }

            // 2. Lấy danh sách ID đã học
            var learnedVocabIds = await _progressRepo.GetLearnedVocabIdsByTopicAsync(request.UserId, request.TopicId);
            if (learnedVocabIds == null) learnedVocabIds = new List<string>();

            // 3. Lọc từ chưa học
            var unlearnedVocabs = allVocabs
                .Where(v => !learnedVocabIds.Contains(v.VocabularyId))
                .ToList();

            List<Tokki.Domain.Entities.Vocabulary> finalResult;

            // 4. Random lấy từ
            if (unlearnedVocabs.Count > 0)
            {
                finalResult = unlearnedVocabs
                    .OrderBy(x => Guid.NewGuid())
                    .Take(request.Count)
                    .ToList();
            }
            else
            {
                finalResult = allVocabs
                    .OrderBy(x => Guid.NewGuid())
                    .Take(request.Count)
                    .ToList();
            }

            var resultDtos = finalResult.Select(v => new VocabBasicInfoDTO
            {
                VocabularyId = v.VocabularyId,
                Text = v.Text,
                Pronunciation = v.Pronunciation,   
                ImgURL = v.ImgURL,
                Definition = v.Definition,
                AudioUrl = v.AudioURL
            }).ToList();

            string message = unlearnedVocabs.Count > 0 ? "Danh sách từ mới" : "Đã học hết! Chuyển sang chế độ ôn tập ngẫu nhiên.";

            return OperationResult<List<VocabBasicInfoDTO>>.Success(resultDtos, 200, message);
        }
    }
}