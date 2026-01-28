using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.MiniGame.DTOs;

namespace Tokki.Application.UseCases.MiniGame.Queries.Solitaire
{
    public class GetSolitaireTopicsQueryHandler : IRequestHandler<GetSolitaireTopicsQuery, OperationResult<List<SolitaireTopicDTO>>>
    {
        private readonly IMiniGameRepository _miniGameRepository;

        public GetSolitaireTopicsQueryHandler(IMiniGameRepository miniGameRepository)
        {
            _miniGameRepository = miniGameRepository;
        }

        public async Task<OperationResult<List<SolitaireTopicDTO>>> Handle(GetSolitaireTopicsQuery request, CancellationToken cancellationToken)
        {
            var random = new Random();

            if (request.Quantity < 20)
            {
                return OperationResult<List<SolitaireTopicDTO>>.Failure("Số lượng từ vựng yêu cầu tối thiểu là 20 để bắt đầu game.", 400);
            }

            var topics = await _miniGameRepository.GetSolitaireTopicsWithVocabsAsync(cancellationToken);

            var resultDTOs = new List<SolitaireTopicDTO>();
            int totalVocabCollected = 0;

            foreach (var topic in topics)
            {
                if (totalVocabCollected >= request.Quantity) break;

                var validVocabs = topic.VocabularyTopics
                    .Where(vt => vt.Vocabulary != null)
                    .Select(vt => vt.Vocabulary)
                    .ToList();

                if (validVocabs.Count < 3) continue;

                int maxTake = Math.Min(validVocabs.Count, 8);
                int countToTake = random.Next(3, maxTake + 1);

                var selectedVocabs = validVocabs
                    .OrderBy(x => random.Next())
                    .Take(countToTake)
                    .ToList();

                var topicDto = new SolitaireTopicDTO
                {
                    TopicId = topic.TopicId,
                    TopicName = topic.TopicName,
                    ImgUrl = topic.ImgUrl ?? "",
                    Vocabularies = selectedVocabs.Select(v => new SolitaireVocabDTO
                    {
                        VocabId = v.VocabularyId,
                        Text = v.Text,
                        ImgUrl = v.ImgURL ?? "",
                        Definition = v.Definition
                    }).ToList()
                };

                resultDTOs.Add(topicDto);
                totalVocabCollected += selectedVocabs.Count;
            }

            if (totalVocabCollected < 20)
            {
                return OperationResult<List<SolitaireTopicDTO>>.Failure(
                    $"Dữ liệu hiện tại quá ít ({totalVocabCollected} từ). Hệ thống cần tối thiểu 20 từ vựng để tạo game.",
                    400);
            }

            if ((request.Quantity - totalVocabCollected) > 5)
            {
                return OperationResult<List<SolitaireTopicDTO>>.Failure(
                    $"Không đủ từ vựng theo yêu cầu. Bạn cần {request.Quantity} từ, nhưng hệ thống chỉ tìm được {totalVocabCollected} từ (Thiếu {request.Quantity - totalVocabCollected} từ).",
                    400);
            }

            return OperationResult<List<SolitaireTopicDTO>>.Success(resultDTOs);
        }
    }
}
