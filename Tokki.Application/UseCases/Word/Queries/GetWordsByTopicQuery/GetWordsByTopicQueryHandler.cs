using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Word.DTOs;

namespace Tokki.Application.UseCases.Word.Queries.GetWordsByTopicQuery
{
    public class GetWordsByTopicQueryHandler : IRequestHandler<GetWordsByTopicQuery, OperationResult<PagedResult<WordWithMeaningsDto>>>
    {
        private readonly IWordRepository _wordRepository;
        private readonly IMeaningRepository _meaningRepository;
        private readonly ITopicRepository _topicRepository;
        private readonly IMeaningTopicRepository _meaningTopicRepository;

        public GetWordsByTopicQueryHandler(
            IWordRepository wordRepository,
            IMeaningRepository meaningRepository,
            ITopicRepository topicRepository,
            IMeaningTopicRepository meaningTopicRepository)
        {
            _wordRepository = wordRepository;
            _meaningRepository = meaningRepository;
            _topicRepository = topicRepository;
            _meaningTopicRepository = meaningTopicRepository;
        }

        public async Task<OperationResult<PagedResult<WordWithMeaningsDto>>> Handle(
            GetWordsByTopicQuery request,
            CancellationToken cancellationToken)
        {
            // Validate Topic tồn tại
            var topic = await _topicRepository.GetByIdAsync(request.TopicId);
            if (topic == null)
            {
                return OperationResult<PagedResult<WordWithMeaningsDto>>.Failure(
                    new List<Error> { AppErrors.TopicNotFound },
                    404,
                    "Topic không tồn tại."
                );
            }

            // Lấy danh sách words với phân trang và filter
            var (words, totalCount) = await _wordRepository.GetPagedWordsByTopicIdAsync(
                request.TopicId,
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.Status
            );

            var wordDtos = new List<WordWithMeaningsDto>();

            foreach (var word in words)
            {
                // Lấy meanings của word trong topic này
                var meanings = await _meaningRepository.GetMeaningsByWordIdAndTopicIdAsync(
                    word.WordId,
                    request.TopicId
                );

                var meaningDtos = meanings.Select(m => new WordMeaningDto
                {
                    MeaningId = m.MeaningId,
                    Definition = m.Definition,
                    ExampleSentence = m.ExampleSentence,
                    ImgURL = m.ImgURL,
                    Status = m.Status
                }).ToList();

                wordDtos.Add(new WordWithMeaningsDto
                {
                    WordId = word.WordId,
                    Text = word.Text,
                    Pronunciation = word.Pronunciation,
                    AudioURL = word.AudioURL,
                    Meanings = meaningDtos,
                    CreateDate = word.CreateDate,
                    CreateBy = word.CreateBy,
                    UpdateDate = word.UpdateDate,
                    UpdateBy = word.UpdateBy,
                    Status = word.Status
                });
            }

            var pagedResult = PagedResult<WordWithMeaningsDto>.Create(
                wordDtos,
                totalCount,
                request.PageNumber,
                request.PageSize
            );

            var message = totalCount > 0
                ? $"Tìm thấy {totalCount} từ vựng trong topic '{topic.TopicName}'."
                : $"Topic '{topic.TopicName}' chưa có từ vựng nào.";

            return OperationResult<PagedResult<WordWithMeaningsDto>>.Success(
                pagedResult,
                200,
                message
            );
        }
    }
}
