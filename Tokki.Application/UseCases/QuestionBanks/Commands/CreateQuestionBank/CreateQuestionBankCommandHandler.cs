using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.CreateQuestionBank
{
    public class CreateQuestionBankCommandHandler : IRequestHandler<CreateQuestionBankCommand, OperationResult<string>>
    {
        private readonly IQuestionBankRepository _questionBankRepository;
        private readonly IQuestionOptionRepository _questionOptionRepository;
        private readonly IQuestionTypeRepository _questionTypeRepository;
        private readonly IPassageRepository _passageRepository;
        private readonly IIdGeneratorService _idGeneratorService;

        public CreateQuestionBankCommandHandler(
            IQuestionBankRepository questionBankRepository,
            IQuestionOptionRepository questionOptionRepository,
            IQuestionTypeRepository questionTypeRepository,
            IPassageRepository passageRepository,
            IIdGeneratorService idGeneratorService)
        {
            _questionBankRepository = questionBankRepository;
            _questionOptionRepository = questionOptionRepository;
            _questionTypeRepository = questionTypeRepository;
            _passageRepository = passageRepository;
            _idGeneratorService = idGeneratorService;
        }

        public async Task<OperationResult<string>> Handle(CreateQuestionBankCommand request, CancellationToken cancellationToken)
        {
            // Validator đã check null/empty + options theo skill.
            // Handler vẫn cần lấy QuestionType để biết skill và để tạo options hay không.
            var questionType = await _questionTypeRepository.GetByIdAsync(request.QuestionTypeId!.Trim(), cancellationToken);
            if (questionType == null)
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.QuestionTypeNotFound },
                    404,
                    AppErrors.QuestionTypeNotFound.Description
                );
            }

            var skill = questionType.Skill;

            // Validate Passage nếu có + check MediaType theo skill (nghiệp vụ)
            if (!string.IsNullOrWhiteSpace(request.PassageId))
            {
                var passage = await _passageRepository.GetByIdAsync(request.PassageId.Trim(), cancellationToken);
                if (passage == null)
                {
                    return OperationResult<string>.Failure(
                        new List<Error> { AppErrors.PassageNotFound },
                        404,
                        AppErrors.PassageNotFound.Description
                    );
                }

                bool isMediaTypeValid = skill switch
                {
                    QuestionSkill.Listening => passage.MediaType == PassageMediaType.Audio,
                    QuestionSkill.Reading => passage.MediaType == PassageMediaType.Text || passage.MediaType == PassageMediaType.Image,
                    QuestionSkill.Writing => passage.MediaType == PassageMediaType.Text || passage.MediaType == PassageMediaType.Image,
                    _ => false
                };

                if (!isMediaTypeValid)
                {
                    return OperationResult<string>.Failure(
                        new List<Error> { AppErrors.PassageMediaTypeMismatch(passage.MediaType, skill) },
                        400,
                        "Thất bại."
                    );
                }
            }

            try
            {
                var questionBankId = _idGeneratorService.GenerateCustom(10);
                var vietnamNow = DateTime.UtcNow.AddHours(7);

                var questionBank = new QuestionBank
                {
                    QuestionBankId = questionBankId,
                    PassageId = string.IsNullOrWhiteSpace(request.PassageId) ? null : request.PassageId.Trim(),
                    QuestionTypeId = request.QuestionTypeId!.Trim(),
                    Content = request.Content,
                    MediaUrl = request.MediaUrl,
                    Explanation = request.Explanation,
                    Status = QuestionBankStatus.Draft,
                    CreatedAt = vietnamNow
                };

                await _questionBankRepository.AddAsync(questionBank);

                // Writing: không tạo options
                if (skill != QuestionSkill.Writing)
                {
                    var options = request.Options.Select(o => new QuestionOption
                    {
                        OptionId = _idGeneratorService.GenerateCustom(10),
                        QuestionBankId = questionBankId,
                        KeyOption = o.KeyOption.Trim(),
                        Content = o.Content,
                        ImageUrl = o.ImageUrl,
                        IsCorrect = o.IsCorrect
                    }).ToList();

                    await _questionOptionRepository.AddRangeAsync(options);
                }

                await _questionBankRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<string>.Success(
                    questionBankId,
                    201,
                    "Tạo câu hỏi thành công."
                );
            }
            catch
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }
    }
}
