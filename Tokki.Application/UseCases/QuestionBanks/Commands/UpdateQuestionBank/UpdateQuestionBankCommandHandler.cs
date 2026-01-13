using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.UpdateQuestionBank
{
    public class UpdateQuestionBankCommandHandler : IRequestHandler<UpdateQuestionBankCommand, OperationResult<string>>
    {
        private readonly IQuestionBankRepository _questionBankRepository;
        private readonly IQuestionOptionRepository _questionOptionRepository;
        private readonly IQuestionTypeRepository _questionTypeRepository;
        private readonly IPassageRepository _passageRepository;
        private readonly IIdGeneratorService _idGeneratorService;

        public UpdateQuestionBankCommandHandler(
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

        public async Task<OperationResult<string>> Handle(UpdateQuestionBankCommand request, CancellationToken cancellationToken)
        {
            var questionBank = await _questionBankRepository.GetByIdWithDetailsAsync(request.QuestionBankId.Trim(), cancellationToken);
            if (questionBank == null)
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.QuestionBankNotFound },
                    404,
                    AppErrors.QuestionBankNotFound.Description
                );
            }

            if (questionBank.Status != QuestionBankStatus.Draft)
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.Forbidden },
                    403,
                    "Chỉ được phép cập nhật khi câu hỏi đang ở trạng thái Draft."
                );
            }

            // final QuestionTypeId
            var finalQuestionTypeId = string.IsNullOrWhiteSpace(request.QuestionTypeId)
                ? questionBank.QuestionTypeId
                : request.QuestionTypeId.Trim();

            var newQuestionType = await _questionTypeRepository.GetByIdAsync(finalQuestionTypeId.Trim(), cancellationToken);
            if (newQuestionType == null)
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.QuestionTypeNotFound },
                    404,
                    AppErrors.QuestionTypeNotFound.Description
                );
            }

            if (!newQuestionType.IsActive)
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ValidationFailed },
                    400,
                    "Loại câu hỏi đang bị vô hiệu hóa."
                );
            }

            var newSkill = newQuestionType.Skill;

            // old skill
            QuestionSkill? oldSkill = null;
            if (!string.IsNullOrWhiteSpace(questionBank.QuestionTypeId))
            {
                var oldQuestionType = await _questionTypeRepository.GetByIdAsync(questionBank.QuestionTypeId.Trim(), cancellationToken);
                oldSkill = oldQuestionType?.Skill;
            }

            // Backstop rule theo skill
            if (newSkill == QuestionSkill.Listening && string.IsNullOrWhiteSpace(request.MediaUrl))
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ValidationFailed },
                    400,
                    "Câu hỏi Listening bắt buộc phải có MediaUrl."
                );
            }

            if (newSkill == QuestionSkill.Writing && request.Options != null && request.Options.Any())
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.WritingNoOptions },
                    400,
                    AppErrors.WritingNoOptions.Description
                );
            }

            // Backstop rule theo chuyển skill
            if (oldSkill.HasValue)
            {
                // Reading -> Writing: bắt buộc options rỗng để xóa đáp án cũ
                if (oldSkill.Value == QuestionSkill.Reading && newSkill == QuestionSkill.Writing)
                {
                    if (request.Options != null && request.Options.Any())
                    {
                        return OperationResult<string>.Failure(
                            new List<Error> { AppErrors.ValidationFailed },
                            400,
                            "Đọc sang Viết: Options phải truyền rỗng để xóa toàn bộ đáp án cũ."
                        );
                    }
                }

                // Listening -> Reading: bắt buộc options rỗng để xóa đáp án cũ, và phải có content
                if (oldSkill.Value == QuestionSkill.Listening && newSkill == QuestionSkill.Reading)
                {
                    if (request.Options != null && request.Options.Any())
                    {
                        return OperationResult<string>.Failure(
                            new List<Error> { AppErrors.ValidationFailed },
                            400,
                            "Nghe sang Đọc: Options phải truyền rỗng để xóa toàn bộ đáp án cũ."
                        );
                    }

                    if (string.IsNullOrWhiteSpace(request.Content))
                    {
                        return OperationResult<string>.Failure(
                            new List<Error> { AppErrors.ValidationFailed },
                            400,
                            "Nghe sang Đọc: Content là bắt buộc."
                        );
                    }
                }

                // Writing -> Listening: phải có MediaUrl
                if (oldSkill.Value == QuestionSkill.Writing && newSkill == QuestionSkill.Listening)
                {
                    if (string.IsNullOrWhiteSpace(request.MediaUrl))
                    {
                        return OperationResult<string>.Failure(
                            new List<Error> { AppErrors.ValidationFailed },
                            400,
                            "Viết sang Nghe: MediaUrl là bắt buộc."
                        );
                    }
                }

                // Writing -> Reading: phải có Content
                if (oldSkill.Value == QuestionSkill.Writing && newSkill == QuestionSkill.Reading)
                {
                    if (string.IsNullOrWhiteSpace(request.Content))
                    {
                        return OperationResult<string>.Failure(
                            new List<Error> { AppErrors.ValidationFailed },
                            400,
                            "Viết sang Đọc: Content là bắt buộc."
                        );
                    }
                }

                // Reading -> Listening: phải có MediaUrl (đã check theo skill, giữ lại cho rõ)
                if (oldSkill.Value == QuestionSkill.Reading && newSkill == QuestionSkill.Listening)
                {
                    if (string.IsNullOrWhiteSpace(request.MediaUrl))
                    {
                        return OperationResult<string>.Failure(
                            new List<Error> { AppErrors.ValidationFailed },
                            400,
                            "Đọc sang Nghe: MediaUrl là bắt buộc."
                        );
                    }
                }
            }

            // Validate Passage theo newSkill (nếu có PassageId)
            var finalPassageId = string.IsNullOrWhiteSpace(request.PassageId) ? null : request.PassageId.Trim();
            if (finalPassageId != null)
            {
                var passage = await _passageRepository.GetByIdAsync(finalPassageId, cancellationToken);
                if (passage == null)
                {
                    return OperationResult<string>.Failure(
                        new List<Error> { AppErrors.PassageNotFound },
                        404,
                        AppErrors.PassageNotFound.Description
                    );
                }

                bool isMediaTypeValid = newSkill switch
                {
                    QuestionSkill.Listening => passage.MediaType == PassageMediaType.Audio,
                    QuestionSkill.Reading or QuestionSkill.Writing =>
                        passage.MediaType == PassageMediaType.Text || passage.MediaType == PassageMediaType.Image,
                    _ => false
                };

                if (!isMediaTypeValid)
                {
                    return OperationResult<string>.Failure(
                        new List<Error> { AppErrors.PassageMediaTypeMismatch(passage.MediaType, newSkill) },
                        400,
                        "Thất bại."
                    );
                }
            }

            try
            {
                // Update question
                questionBank.PassageId = finalPassageId;
                questionBank.QuestionTypeId = finalQuestionTypeId;

                questionBank.Content = request.Content;
                questionBank.MediaUrl = request.MediaUrl;
                questionBank.Explanation = request.Explanation;

                await _questionBankRepository.UpdateAsync(questionBank);

                // Update answers
                if (newSkill == QuestionSkill.Writing)
                {
                    // Writing => xóa toàn bộ đáp án
                    await _questionOptionRepository.DeleteByQuestionBankIdAsync(questionBank.QuestionBankId, cancellationToken);
                }
                else
                {
                    // Reading/Listening => replace-all (nếu request.Options rỗng thì sẽ xóa hết và không add lại)
                    await _questionOptionRepository.DeleteByQuestionBankIdAsync(questionBank.QuestionBankId, cancellationToken);

                    if (request.Options != null && request.Options.Any())
                    {
                        var options = request.Options.Select(o => new QuestionOption
                        {
                            OptionId = _idGeneratorService.GenerateCustom(10),
                            QuestionBankId = questionBank.QuestionBankId,
                            KeyOption = o.KeyOption.Trim(),
                            Content = o.Content,
                            ImageUrl = o.ImageUrl,
                            IsCorrect = o.IsCorrect
                        }).ToList();

                        await _questionOptionRepository.AddRangeAsync(options);
                    }
                }

                await _questionBankRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<string>.Success(
                    request.QuestionBankId,
                    200,
                    "Cập nhật câu hỏi thành công"
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
