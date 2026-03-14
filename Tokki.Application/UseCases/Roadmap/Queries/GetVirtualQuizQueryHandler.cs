using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Roadmap.DTOs;

namespace Tokki.Application.UseCases.Roadmap.Queries.GetVirtualQuiz
{
    public class GetVirtualQuizQueryHandler
        : IRequestHandler<GetVirtualQuizQuery, OperationResult<List<VirtualQuizQuestionViewModel>>>
    {
        private readonly IUserRoadmapRepository _userRoadmapRepository;

        public GetVirtualQuizQueryHandler(IUserRoadmapRepository userRoadmapRepository)
        {
            _userRoadmapRepository = userRoadmapRepository;
        }

        public async Task<OperationResult<List<VirtualQuizQuestionViewModel>>> Handle(
            GetVirtualQuizQuery request, CancellationToken cancellationToken)
        {
            var typeExists = await _userRoadmapRepository
                .QuestionTypeExistsAsync(request.QuestionTypeId, cancellationToken);

            if (!typeExists)
                return OperationResult<List<VirtualQuizQuestionViewModel>>
                    .Failure("Không tìm thấy loại câu hỏi này.", 404);

            var questions = await _userRoadmapRepository
                .GetRandomQuestionsByTypeAsync(request.QuestionTypeId, request.Count, cancellationToken);

            if (!questions.Any())
                return OperationResult<List<VirtualQuizQuestionViewModel>>
                    .Failure("Không có câu hỏi nào cho loại này.", 404);

            var result = questions.Select(q => new VirtualQuizQuestionViewModel
            {
                QuestionId = q.QuestionBankId,
                Content = q.Content,
                MediaUrl = q.MediaUrl,
                PassageContent = q.Passage?.Content,
                Options = q.QuestionOptions.Select(o => new VirtualQuizOptionViewModel
                {
                    OptionId = o.OptionId,
                    KeyOption = o.KeyOption,
                    Content = o.Content,
                    ImageUrl = o.ImageUrl
                }).ToList()
            }).ToList();

            return OperationResult<List<VirtualQuizQuestionViewModel>>.Success(result);
        }
    }
}