
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.QuestionBanks.DTOs;

namespace Tokki.Application.UseCases.QuestionBanks.Queries.GetQuestionBankById
{
    public class GetQuestionBankByIdQueryHandler : IRequestHandler<GetQuestionBankByIdQuery, OperationResult<QuestionBankDto>>
    {
        private readonly IQuestionBankRepository _questionBankRepository;

        public GetQuestionBankByIdQueryHandler(IQuestionBankRepository questionBankRepository)
        {
            _questionBankRepository = questionBankRepository;
        }

        public async Task<OperationResult<QuestionBankDto>> Handle(
            GetQuestionBankByIdQuery request,
            CancellationToken cancellationToken)
        {
            var questionBank = await _questionBankRepository.GetByIdWithDetailsAsync(request.QuestionBankId, cancellationToken);

            if (questionBank == null)
            {
                return OperationResult<QuestionBankDto>.Failure(
                     new List<Tokki.Application.Common.Models.Error> { AppErrors.QuestionBankNotFound },
                    404,
                    AppErrors.QuestionBankNotFound.Description
                );
            }

            var dto = new QuestionBankDto
            {
                QuestionBankId = questionBank.QuestionBankId,
                PassageId = questionBank.PassageId,
                PassageTitle = questionBank.Passage?.Title,
                QuestionTypeId = questionBank.QuestionTypeId,
                QuestionTypeName = questionBank.QuestionType?.Name,
                Content = questionBank.Content,
                MediaUrl = questionBank.MediaUrl,
                Explanation = questionBank.Explanation,
                Status = questionBank.Status,
                Options = questionBank.QuestionOptions.Select(o => new QuestionOptionDto
                {
                    OptionId = o.OptionId,
                    KeyOption = o.KeyOption,
                    Content = o.Content,
                    ImageUrl = o.ImageUrl,
                    IsCorrect = o.IsCorrect
                }).OrderBy(o => o.KeyOption).ToList()
            };

            return OperationResult<QuestionBankDto>.Success(
                dto,
                200,
                "Lấy thông tin câu hỏi thành công"
            );
        }
    }
}
