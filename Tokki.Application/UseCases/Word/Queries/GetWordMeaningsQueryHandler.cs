using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Word.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Word.Queries
{
    public class GetWordMeaningsQueryHandler : IRequestHandler<GetWordMeaningsQuery, OperationResult<PagedResult<MeaningDto>>>
    {
        private readonly IWordRepository _wordRepository;
        private readonly IMeaningRepository _meaningRepository;
        private readonly IMeaningTopicRepository _meaningTopicRepository;
        private readonly ITopicRepository _topicRepository;

        public GetWordMeaningsQueryHandler(
            IWordRepository wordRepository,
            IMeaningRepository meaningRepository,
            IMeaningTopicRepository meaningTopicRepository,
            ITopicRepository topicRepository)
        {
            _wordRepository = wordRepository;
            _meaningRepository = meaningRepository;
            _meaningTopicRepository = meaningTopicRepository;
            _topicRepository = topicRepository;
        }

        public async Task<OperationResult<PagedResult<MeaningDto>>> Handle(
            GetWordMeaningsQuery request,
            CancellationToken cancellationToken)
        {
            // Validate input
            if (string.IsNullOrEmpty(request.WordId) && string.IsNullOrEmpty(request.Text))
            {
                return OperationResult<PagedResult<MeaningDto>>.Failure(
                    new List<Error> { new Error("INVALID_INPUT", "Cần cung cấp WordId hoặc Text.") },
                    400
                );
            }

            // Tìm word
            Tokki.Domain.Entities.Word? word = null;

            if (!string.IsNullOrEmpty(request.WordId))
            {
                word = await _wordRepository.GetByIdAsync(request.WordId);
            }
            else if (!string.IsNullOrEmpty(request.Text))
            {
                word = await _wordRepository.GetByTextAsync(request.Text);
            }

            if (word == null)
            {
                return OperationResult<PagedResult<MeaningDto>>.Failure(
                    new List<Error> { new Error("WORD_NOT_FOUND", "Không tìm thấy từ vựng.") },
                    404
                );
            }

            // Lấy meanings với phân trang và filter
            var (meanings, totalCount) = await _meaningRepository.GetPagedMeaningsByWordIdAsync(
                word.WordId,
                request.PageNumber,
                request.PageSize,
                request.TopicId,
                request.Status
            );

            // Build response
            var meaningDtos = new List<MeaningDto>();

            foreach (var meaning in meanings)
            {
                // Get topics for this meaning
                var meaningTopics = await _meaningTopicRepository.GetByMeaningIdAsync(meaning.MeaningId);
                var activeTopics = meaningTopics.Where(mt => mt.Status == MeaningTopicStatus.Active).ToList();

                var topics = new List<TopicInfoDto>();
                foreach (var mt in activeTopics)
                {
                    var topic = await _topicRepository.GetByIdAsync(mt.TopicId);
                    if (topic != null)
                    {
                        topics.Add(new TopicInfoDto
                        {
                            TopicId = topic.TopicId,
                            TopicName = topic.TopicName
                        });
                    }
                }

                meaningDtos.Add(new MeaningDto
                {
                    MeaningId = meaning.MeaningId,
                    WordId = word.WordId,
                    WordText = word.Text,
                    Pronunciation = word.Pronunciation,
                    AudioURL = word.AudioURL,
                    Definition = meaning.Definition,
                    ExampleSentence = meaning.ExampleSentence,
                    ImgURL = meaning.ImgURL,
                    Topics = topics,
                    CreateDate = meaning.CreateDate,
                    CreateBy = meaning.CreateBy,
                    UpdateDate = meaning.UpdateDate,
                    UpdateBy = meaning.UpdateBy,
                    Status = meaning.Status
                });
            }

            var pagedResult = PagedResult<MeaningDto>.Create(
                meaningDtos,
                totalCount,
                request.PageNumber,
                request.PageSize
            );

            var message = totalCount > 0
                ? $"Tìm thấy {totalCount} nghĩa cho từ '{word.Text}'."
                : $"Từ '{word.Text}' chưa có nghĩa nào.";

            return OperationResult<PagedResult<MeaningDto>>.Success(
                pagedResult,
                200,
                message
            );
        }
    }

}
