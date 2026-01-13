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
            var qbId = request.QuestionBankId?.Trim();
            if (string.IsNullOrWhiteSpace(qbId))
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ValidationFailed },
                    400,
                    "QuestionBankId không hợp lệ."
                );
            }

            var questionBank = await _questionBankRepository.GetByIdWithDetailsAsync(qbId, cancellationToken);
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

            // ===== PATCH RULES (NEW) =====
            // - null => không update
            // - ""/whitespace => update thành ""
            static string PatchTextAllowEmpty(string? incoming, string current)
                => incoming == null ? current : (string.IsNullOrWhiteSpace(incoming) ? string.Empty : incoming);

            static string PatchTrimAllowEmpty(string? incoming, string current)
                => incoming == null ? current : (incoming.Trim()); // Trim để "   " => ""

            // PassageId special:
            // - null => không update
            // - ""/whitespace => clear (null)
            // - value => trim + update
            static string? PatchPassageId(string? incoming, string? current)
            {
                if (incoming == null) return current;
                if (string.IsNullOrWhiteSpace(incoming)) return null; // gỡ passage
                return incoming.Trim();
            }

            // old skill
            var oldSkill = questionBank.QuestionType?.Skill;
            if (oldSkill == null && !string.IsNullOrWhiteSpace(questionBank.QuestionTypeId))
            {
                var oldQt = await _questionTypeRepository.GetByIdAsync(questionBank.QuestionTypeId.Trim(), cancellationToken);
                oldSkill = oldQt?.Skill;
            }

            // final values after patch
            var finalQuestionTypeId = PatchTrimAllowEmpty(request.QuestionTypeId, questionBank.QuestionTypeId); // "" => ""
            var finalPassageId = PatchPassageId(request.PassageId, questionBank.PassageId);                     // "" => null

            var finalContent = PatchTextAllowEmpty(request.Content, questionBank.Content);
            var finalMediaUrl = PatchTrimAllowEmpty(request.MediaUrl, questionBank.MediaUrl ?? string.Empty);
            var finalExplanation = PatchTextAllowEmpty(request.Explanation, questionBank.Explanation ?? string.Empty);

            // normalize nullable fields back to null where appropriate
            // (MediaUrl/Explanation trong entity của bạn là nullable)
            string? finalMediaUrlNullable = finalMediaUrl;        // "" được phép lưu ""
            string? finalExplanationNullable = finalExplanation;  // "" được phép lưu ""

            if (string.IsNullOrWhiteSpace(finalQuestionTypeId))
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ValidationFailed },
                    400,
                    "QuestionTypeId sau patch không hợp lệ (rỗng)."
                );
            }

            var newQt = await _questionTypeRepository.GetByIdAsync(finalQuestionTypeId.Trim(), cancellationToken);
            if (newQt == null)
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.QuestionTypeNotFound },
                    404,
                    AppErrors.QuestionTypeNotFound.Description
                );
            }

            if (!newQt.IsActive)
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ValidationFailed },
                    400,
                    "Loại câu hỏi đang bị vô hiệu hóa."
                );
            }

            var newSkill = newQt.Skill;

            // options semantics: null => no update; [] => delete; [..] => replace
            var hasExistingOptions = questionBank.QuestionOptions != null && questionBank.QuestionOptions.Any();
            var optionsProvided = request.Options != null;
            var optionsCount = request.Options?.Count ?? 0;

            // ===== VALIDATE THEO SKILL CUỐI =====
            if (newSkill == QuestionSkill.Listening && string.IsNullOrWhiteSpace(finalMediaUrlNullable))
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ValidationFailed },
                    400,
                    "Câu hỏi Listening bắt buộc phải có MediaUrl."
                );
            }

            if (newSkill == QuestionSkill.Reading && string.IsNullOrWhiteSpace(finalContent))
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ValidationFailed },
                    400,
                    "Câu hỏi Reading bắt buộc phải có Content."
                );
            }

            if (newSkill == QuestionSkill.Writing)
            {
                // Writing không được có đáp án
                if (optionsProvided && optionsCount > 0)
                {
                    return OperationResult<string>.Failure(
                        new List<Error> { AppErrors.WritingNoOptions },
                        400,
                        AppErrors.WritingNoOptions.Description
                    );
                }

                // nếu DB đang có options mà không gửi options: [] để xóa => lỗi
                if (hasExistingOptions && (!optionsProvided || optionsCount != 0))
                {
                    return OperationResult<string>.Failure(
                        new List<Error> { AppErrors.ValidationFailed },
                        400,
                        "Chuyển sang Writing yêu cầu xóa toàn bộ đáp án: phải gửi options: []."
                    );
                }
            }

            // ===== VALIDATE THEO CHUYỂN SKILL (theo rule bạn đã chốt trước đó) =====
            if (oldSkill.HasValue)
            {
                if (oldSkill.Value == QuestionSkill.Reading && newSkill == QuestionSkill.Writing)
                {
                    if (!optionsProvided || optionsCount != 0)
                    {
                        return OperationResult<string>.Failure(
                            new List<Error> { AppErrors.ValidationFailed },
                            400,
                            "Đọc sang Viết: bắt buộc gửi options: [] để xóa toàn bộ đáp án cũ."
                        );
                    }
                }

                if (oldSkill.Value == QuestionSkill.Listening && newSkill == QuestionSkill.Reading)
                {
                    if (string.IsNullOrWhiteSpace(finalContent))
                    {
                        return OperationResult<string>.Failure(
                            new List<Error> { AppErrors.ValidationFailed },
                            400,
                            "Nghe sang Đọc: Content là bắt buộc."
                        );
                    }

                    if (!optionsProvided || optionsCount != 0)
                    {
                        return OperationResult<string>.Failure(
                            new List<Error> { AppErrors.ValidationFailed },
                            400,
                            "Nghe sang Đọc: bắt buộc gửi options: [] để xóa toàn bộ đáp án cũ."
                        );
                    }
                }

                if (oldSkill.Value == QuestionSkill.Writing && newSkill == QuestionSkill.Reading)
                {
                    if (string.IsNullOrWhiteSpace(finalContent))
                    {
                        return OperationResult<string>.Failure(
                            new List<Error> { AppErrors.ValidationFailed },
                            400,
                            "Viết sang Đọc: Content là bắt buộc."
                        );
                    }

                    if (!optionsProvided || optionsCount < 2)
                    {
                        return OperationResult<string>.Failure(
                            new List<Error> { AppErrors.ValidationFailed },
                            400,
                            "Viết sang Đọc: bắt buộc gửi tối thiểu 2 đáp án trong options."
                        );
                    }
                }

                if (oldSkill.Value == QuestionSkill.Writing && newSkill == QuestionSkill.Listening)
                {
                    if (string.IsNullOrWhiteSpace(finalMediaUrlNullable))
                    {
                        return OperationResult<string>.Failure(
                            new List<Error> { AppErrors.ValidationFailed },
                            400,
                            "Viết sang Nghe: MediaUrl là bắt buộc."
                        );
                    }

                    if (!optionsProvided || optionsCount < 2)
                    {
                        return OperationResult<string>.Failure(
                            new List<Error> { AppErrors.ValidationFailed },
                            400,
                            "Viết sang Nghe: bắt buộc gửi tối thiểu 2 đáp án trong options."
                        );
                    }
                }

                if (oldSkill.Value == QuestionSkill.Reading && newSkill == QuestionSkill.Listening)
                {
                    if (string.IsNullOrWhiteSpace(finalMediaUrlNullable))
                    {
                        return OperationResult<string>.Failure(
                            new List<Error> { AppErrors.ValidationFailed },
                            400,
                            "Đọc sang Nghe: MediaUrl là bắt buộc."
                        );
                    }
                }
            }

            // ===== VALIDATE OPTIONS (khi request có gửi) =====
            if (optionsProvided)
            {
                if (newSkill == QuestionSkill.Writing)
                {
                    if (optionsCount != 0)
                    {
                        return OperationResult<string>.Failure(
                            new List<Error> { AppErrors.WritingNoOptions },
                            400,
                            AppErrors.WritingNoOptions.Description
                        );
                    }
                }
                else
                {
                    // options: [] => cho phép (xóa)
                    // options có phần tử => validate trắc nghiệm
                    if (optionsCount > 0)
                    {
                        if (optionsCount < 2 || optionsCount > 4)
                        {
                            return OperationResult<string>.Failure(
                                new List<Error> { AppErrors.QuestionBankInvalidOptions },
                                400,
                                AppErrors.QuestionBankInvalidOptions.Description
                            );
                        }

                        var validKeys = new HashSet<string> { "1", "2", "3", "4" };
                        var keys = new List<string>();
                        var correctCount = 0;

                        foreach (var o in request.Options!)
                        {
                            var key = o.KeyOption?.Trim();
                            if (string.IsNullOrWhiteSpace(key) || !validKeys.Contains(key))
                            {
                                return OperationResult<string>.Failure(
                                    new List<Error> { AppErrors.QuestionBankInvalidKeyOption },
                                    400,
                                    AppErrors.QuestionBankInvalidKeyOption.Description
                                );
                            }
                            keys.Add(key);

                            var hasText = !string.IsNullOrWhiteSpace(o.Content);
                            var hasImage = !string.IsNullOrWhiteSpace(o.ImageUrl);
                            if (!hasText && !hasImage)
                            {
                                return OperationResult<string>.Failure(
                                    new List<Error> { AppErrors.ValidationFailed },
                                    400,
                                    "Đáp án phải có nội dung text hoặc ảnh."
                                );
                            }

                            if (o.IsCorrect) correctCount++;
                        }

                        if (keys.Distinct().Count() != keys.Count)
                        {
                            return OperationResult<string>.Failure(
                                new List<Error> { AppErrors.QuestionBankDuplicateKeyOption },
                                400,
                                AppErrors.QuestionBankDuplicateKeyOption.Description
                            );
                        }

                        if (correctCount == 0)
                        {
                            return OperationResult<string>.Failure(
                                new List<Error> { AppErrors.QuestionBankNoCorrectAnswer },
                                400,
                                AppErrors.QuestionBankNoCorrectAnswer.Description
                            );
                        }

                        if (correctCount > 1)
                        {
                            return OperationResult<string>.Failure(
                                new List<Error> { AppErrors.QuestionBankMultipleCorrectAnswers },
                                400,
                                AppErrors.QuestionBankMultipleCorrectAnswers.Description
                            );
                        }
                    }
                }
            }

            // ===== VALIDATE PASSAGE THEO SKILL CUỐI (NEW RULES) =====
            // finalPassageId == null => đã gỡ hoặc không có => bỏ qua validate
            if (!string.IsNullOrWhiteSpace(finalPassageId))
            {
                var passage = await _passageRepository.GetByIdAsync(finalPassageId.Trim(), cancellationToken);
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
                    QuestionSkill.Reading => passage.MediaType == PassageMediaType.Text || passage.MediaType == PassageMediaType.Image,
                    QuestionSkill.Writing => passage.MediaType == PassageMediaType.Text
                                             || passage.MediaType == PassageMediaType.Image
                                             || passage.MediaType == PassageMediaType.Audio,
                    _ => false
                };

                if (!isMediaTypeValid)
                {
                    return OperationResult<string>.Failure(
                        new List<Error> { AppErrors.PassageMediaTypeMismatch(passage.MediaType, newSkill) },
                        400,
                        $"Passage.MediaType ({passage.MediaType}) không hợp lệ cho kỹ năng {newSkill}."
                    );
                }
            }

            try
            {
                // Apply
                questionBank.QuestionTypeId = finalQuestionTypeId.Trim();
                questionBank.PassageId = finalPassageId; // "" => null (gỡ), null => giữ, value => update
                questionBank.Content = finalContent;     // "" => lưu ""
                questionBank.MediaUrl = finalMediaUrlNullable;       // "" => lưu ""
                questionBank.Explanation = finalExplanationNullable; // "" => lưu ""

                await _questionBankRepository.UpdateAsync(questionBank);

                // options update only when provided
                if (optionsProvided)
                {
                    await _questionOptionRepository.DeleteByQuestionBankIdAsync(questionBank.QuestionBankId, cancellationToken);

                    if (optionsCount > 0)
                    {
                        var newOptions = request.Options!.Select(o => new QuestionOption
                        {
                            OptionId = _idGeneratorService.GenerateCustom(10),
                            QuestionBankId = questionBank.QuestionBankId,
                            KeyOption = o.KeyOption.Trim(),
                            Content = o.Content,
                            ImageUrl = o.ImageUrl,
                            IsCorrect = o.IsCorrect
                        }).ToList();

                        await _questionOptionRepository.AddRangeAsync(newOptions);
                    }
                }

                await _questionBankRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<string>.Success(
                    questionBank.QuestionBankId,
                    200,
                    "Cập nhật câu hỏi thành công."
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
