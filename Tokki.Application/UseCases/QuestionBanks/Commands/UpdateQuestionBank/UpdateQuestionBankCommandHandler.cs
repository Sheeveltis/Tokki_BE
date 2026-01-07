using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.UpdateQuestionBank
{
    public class UpdateQuestionBankCommandHandler : IRequestHandler<UpdateQuestionBankCommand, OperationResult<string>>
    {
        private readonly IQuestionBankRepository _questionBankRepository;
        private readonly IQuestionTypeRepository _questionTypeRepository;
        private readonly IPassageRepository _passageRepository;

        public UpdateQuestionBankCommandHandler(
            IQuestionBankRepository questionBankRepository,
            IQuestionTypeRepository questionTypeRepository,
            IPassageRepository passageRepository)
        {
            _questionBankRepository = questionBankRepository;
            _questionTypeRepository = questionTypeRepository;
            _passageRepository = passageRepository;
        }

        public async Task<OperationResult<string>> Handle(UpdateQuestionBankCommand request, CancellationToken cancellationToken)
        {
            var questionBank = await _questionBankRepository.GetByIdWithDetailsAsync(request.QuestionBankId, cancellationToken);
            if (questionBank == null)
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.QuestionBankNotFound },
                    404,
                    AppErrors.QuestionBankNotFound.Description
                );
            }

            // Chỉ được update khi DB đang Draft
            if (questionBank.Status != QuestionBankStatus.Draft)
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.Forbidden },
                    403,
                    "Chỉ được phép cập nhật khi câu hỏi đang ở trạng thái Draft."
                );
            }

            // Xác định QuestionTypeId cuối cùng sẽ lưu
            var finalQuestionTypeId = string.IsNullOrWhiteSpace(request.QuestionTypeId)
                ? questionBank.QuestionTypeId
                : request.QuestionTypeId.Trim();

            if (string.IsNullOrWhiteSpace(finalQuestionTypeId))
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ValidationFailed },
                    400,
                    "Không xác định được QuestionTypeId để kiểm tra kỹ năng."
                );
            }

            // Lấy QuestionType mới (final) để biết skill mới
            var newQuestionType = await _questionTypeRepository.GetByIdAsync(finalQuestionTypeId, cancellationToken);
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

            // --- CHECK ĐỔI SKILL LIÊN QUAN OPTIONS ---
            var isChangingQuestionType =
                !string.IsNullOrWhiteSpace(request.QuestionTypeId) &&
                !string.Equals(questionBank.QuestionTypeId, finalQuestionTypeId, StringComparison.OrdinalIgnoreCase);

            if (isChangingQuestionType)
            {
                // Lấy skill hiện tại theo QuestionTypeId cũ (không tin hoàn toàn vào navigation)
                QuestionSkill? oldSkill = null;
                if (!string.IsNullOrWhiteSpace(questionBank.QuestionTypeId))
                {
                    var oldQuestionType = await _questionTypeRepository.GetByIdAsync(questionBank.QuestionTypeId.Trim(), cancellationToken);
                    oldSkill = oldQuestionType?.Skill;
                }

                var hasOptions = questionBank.QuestionOptions != null && questionBank.QuestionOptions.Any();

                // Từ Reading/Listening -> Writing: nếu đã có options thì chặn
                if (newSkill == QuestionSkill.Writing && hasOptions)
                {
                    return OperationResult<string>.Failure(
                        new List<Error> { AppErrors.ValidationFailed },
                        400,
                        "Không thể chuyển sang Writing vì câu hỏi đang có đáp án trắc nghiệm. Vui lòng xóa đáp án trước khi đổi loại câu hỏi."
                    );
                }

                // Từ Writing -> Reading/Listening: nếu chưa có options thì chặn (vì endpoint này không tạo options)
                if (newSkill != QuestionSkill.Writing && !hasOptions)
                {
                    return OperationResult<string>.Failure(
                        new List<Error> { AppErrors.ValidationFailed },
                        400,
                        "Không thể chuyển sang câu hỏi trắc nghiệm vì hiện tại chưa có đáp án. Vui lòng thêm đáp án trắc nghiệm trước."
                    );
                }

                // Nếu muốn chặt hơn: chỉ cho phép đổi skill khi oldSkill xác định rõ
                // (tùy bạn, đoạn này có thể bỏ)
                if (oldSkill == null)
                {
                    // Không bắt buộc, nhưng giúp tránh dữ liệu lộn xộn
                    // return OperationResult<string>.Failure(...);
                }
            }

            // --- Validate Passage theo skill mới (nếu có PassageId) ---
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
                questionBank.PassageId = finalPassageId;
                questionBank.QuestionTypeId = finalQuestionTypeId;

                questionBank.Content = request.Content;
                questionBank.MediaUrl = request.MediaUrl;
                questionBank.Explanation = request.Explanation;

                await _questionBankRepository.UpdateAsync(questionBank);
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
