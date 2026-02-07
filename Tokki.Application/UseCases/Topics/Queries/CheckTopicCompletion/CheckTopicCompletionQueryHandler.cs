using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.DTOs;

namespace Tokki.Application.UseCases.Topics.Queries.CheckTopicCompletion
{
    public class CheckTopicCompletionQueryHandler : IRequestHandler<CheckTopicCompletionQuery, OperationResult<TopicCompletionStatusDTO>>
    {
        private readonly ITopicRepository _topicRepository;

        public CheckTopicCompletionQueryHandler(ITopicRepository topicRepository)
        {
            _topicRepository = topicRepository;
        }

        public async Task<OperationResult<TopicCompletionStatusDTO>> Handle(CheckTopicCompletionQuery request, CancellationToken cancellationToken)
        {
            var topic = await _topicRepository.GetByIdAsync(request.TopicId);
            if (topic == null)
            {
                return OperationResult<TopicCompletionStatusDTO>.Failure(AppErrors.TopicNotFound, 404);
            }

            var totalVocab = await _topicRepository.CountVocabulariesInTopicAsync(request.TopicId);

            if (totalVocab == 0)
            {
                return OperationResult<TopicCompletionStatusDTO>.Success(new TopicCompletionStatusDTO
                {
                    TopicId = request.TopicId,
                    IsCompleted = true,
                    ProgressPercent = 100,
                    TotalVocab = 0,
                    LearnedVocab = 0
                });
            }

            var learnedCount = await _topicRepository.CountLearnedVocabulariesAsync(request.UserId, request.TopicId);

            int progressPercent = (int)((double)learnedCount / totalVocab * 100);
            if (progressPercent > 100) progressPercent = 100;

            bool isCompleted = (learnedCount >= totalVocab);

            var resultDto = new TopicCompletionStatusDTO
            {
                TopicId = request.TopicId,
                IsCompleted = isCompleted,
                ProgressPercent = progressPercent,
                TotalVocab = totalVocab,
                LearnedVocab = learnedCount
            };

            string msg = isCompleted ? "Bạn đã hoàn thành chủ đề này." : "Bạn chưa hoàn thành chủ đề này.";

            return OperationResult<TopicCompletionStatusDTO>.Success(resultDto, 200, msg);
        }
    }
}
