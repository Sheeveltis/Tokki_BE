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
            var questionTypeId = request.QuestionTypeId?.Trim();
            if (string.IsNullOrWhiteSpace(questionTypeId))
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ValidationFailed },
                    400,
                    "QuestionTypeId không hợp lệ."
                );
            }

            var questionType = await _questionTypeRepository.GetByIdAsync(questionTypeId, cancellationToken);
            if (questionType == null)
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.QuestionTypeNotFound },
                    404,
                    AppErrors.QuestionTypeNotFound.Description
                );
            }

            if (!questionType.IsActive)
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ValidationFailed },
                    400,
                    "Loại câu hỏi đang bị vô hiệu hóa."
                );
            }

            var skill = questionType.Skill;

            // ===== Đồng bộ rule giống Update =====
            if (skill == QuestionSkill.Listening && string.IsNullOrWhiteSpace(request.MediaUrl))
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ValidationFailed },
                    400,
                    "Câu hỏi Listening bắt buộc phải có MediaUrl."
                );
            }

            if (skill == QuestionSkill.Reading && string.IsNullOrWhiteSpace(request.Content))
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ValidationFailed },
                    400,
                    "Câu hỏi Reading bắt buộc phải có Content."
                );
            }

            // ===== Validate Passage nếu có + check MediaType theo skill (đồng bộ với Update) =====
            string? passageId = string.IsNullOrWhiteSpace(request.PassageId) ? null : request.PassageId.Trim();

            if (!string.IsNullOrWhiteSpace(passageId))
            {
                var passage = await _passageRepository.GetByIdAsync(passageId, cancellationToken);
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
                    // CHANGED: Writing cho phép Audio giống Update
                    QuestionSkill.Writing => passage.MediaType == PassageMediaType.Text
                                             || passage.MediaType == PassageMediaType.Image
                                             || passage.MediaType == PassageMediaType.Audio,
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

                // Normalize một số field giống tinh thần Update (trim + cho phép empty)
                var mediaUrlNormalized = request.MediaUrl == null ? null : request.MediaUrl.Trim();
                var explanationNormalized = request.Explanation; // có thể giữ nguyên format
                var contentNormalized = request.Content ?? string.Empty;

                var questionBank = new QuestionBank
                {
                    QuestionBankId = questionBankId,
                    PassageId = passageId,
                    QuestionTypeId = questionTypeId,
                    Content = contentNormalized,
                    MediaUrl = mediaUrlNormalized,
                    Explanation = explanationNormalized,
                    Status = QuestionBankStatus.Draft,
                    CreatedAt = vietnamNow,
                    CreateBy = request.CreateBy

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
