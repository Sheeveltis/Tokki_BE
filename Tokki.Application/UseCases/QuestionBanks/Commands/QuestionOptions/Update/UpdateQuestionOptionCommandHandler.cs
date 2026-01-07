using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.QuestionOptions.Update
{
    public class UpdateQuestionOptionCommandHandler : IRequestHandler<UpdateQuestionOptionCommand, OperationResult<string>>
    {
        private readonly IQuestionBankRepository _questionBankRepository;
        private readonly IQuestionOptionRepository _questionOptionRepository;
        private readonly IQuestionTypeRepository _questionTypeRepository;

        public UpdateQuestionOptionCommandHandler(
            IQuestionBankRepository questionBankRepository,
            IQuestionOptionRepository questionOptionRepository,
            IQuestionTypeRepository questionTypeRepository)
        {
            _questionBankRepository = questionBankRepository;
            _questionOptionRepository = questionOptionRepository;
            _questionTypeRepository = questionTypeRepository;
        }

        public async Task<OperationResult<string>> Handle(UpdateQuestionOptionCommand request, CancellationToken cancellationToken)
        {
            var qb = await _questionBankRepository.GetByIdWithDetailsAsync(request.QuestionBankId, cancellationToken);
            if (qb == null)
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.QuestionBankNotFound },
                    404,
                    AppErrors.QuestionBankNotFound.Description);
            }

            if (qb.Status != QuestionBankStatus.Draft)
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.Forbidden },
                    403,
                    "Chỉ được phép chỉnh đáp án khi câu hỏi đang ở trạng thái Draft.");
            }

            if (string.IsNullOrWhiteSpace(qb.QuestionTypeId))
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ValidationFailed },
                    400,
                    "QuestionTypeId của câu hỏi đang rỗng.");
            }

            var qt = await _questionTypeRepository.GetByIdAsync(qb.QuestionTypeId.Trim(), cancellationToken);
            if (qt == null)
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.QuestionTypeNotFound },
                    404,
                    AppErrors.QuestionTypeNotFound.Description);
            }

            if (qt.Skill == QuestionSkill.Writing)
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ValidationFailed },
                    400,
                    "Câu hỏi Writing không được có đáp án trắc nghiệm.");
            }

            var option = qb.QuestionOptions.FirstOrDefault(o => o.OptionId == request.OptionId);
            if (option == null)
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.QuestionOptionNotFound },
                    404,
                    AppErrors.QuestionOptionNotFound.Description);
            }

            // =========================
            // 1) KEY OPTION (chỉ update nếu request có KeyOption khác rỗng)
            // =========================
            if (!string.IsNullOrWhiteSpace(request.KeyOption))
            {
                var newKey = request.KeyOption.Trim();

                if (!string.Equals(option.KeyOption, newKey, StringComparison.Ordinal))
                {
                    var duplicated = qb.QuestionOptions.Any(o =>
                        o.OptionId != option.OptionId &&
                        string.Equals(o.KeyOption, newKey, StringComparison.Ordinal));

                    if (duplicated)
                    {
                        return OperationResult<string>.Failure(
                            new List<Error> { AppErrors.ValidationFailed },
                            400,
                            $"KeyOption '{newKey}' đã tồn tại trong câu hỏi này.");
                    }
                }

                option.KeyOption = newKey;
            }

            // =========================
            // 2) CONTENT / IMAGE URL ("" => không đổi)
            // =========================
            if (!string.IsNullOrWhiteSpace(request.Content))
            {
                // Không trim để tránh mất định dạng nội dung
                option.Content = request.Content;
            }

            if (!string.IsNullOrWhiteSpace(request.ImageUrl))
            {
                option.ImageUrl = request.ImageUrl.Trim();
            }

            // Sau khi áp dụng update (nếu có), đảm bảo đáp án còn "có dữ liệu"
            if (string.IsNullOrWhiteSpace(option.Content) && string.IsNullOrWhiteSpace(option.ImageUrl))
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ValidationFailed },
                    400,
                    "Đáp án phải có nội dung text hoặc ảnh.");
            }

            // =========================
            // 3) IS CORRECT (null => không đổi)
            // =========================
            if (request.IsCorrect.HasValue)
            {
                var newIsCorrect = request.IsCorrect.Value;

                // Nếu đang là đáp án đúng duy nhất mà set false => chặn
                if (!newIsCorrect && option.IsCorrect)
                {
                    var hasOtherCorrect = qb.QuestionOptions.Any(o => o.OptionId != option.OptionId && o.IsCorrect);
                    if (!hasOtherCorrect)
                    {
                        return OperationResult<string>.Failure(
                            new List<Error> { AppErrors.ValidationFailed },
                            400,
                            "Câu hỏi phải có ít nhất một đáp án đúng. Không thể bỏ chọn đáp án đúng duy nhất.");
                    }
                }

                // Nếu set true => gỡ đúng của option khác
                if (newIsCorrect)
                {
                    foreach (var opt in qb.QuestionOptions.Where(o => o.OptionId != option.OptionId && o.IsCorrect))
                    {
                        opt.IsCorrect = false;
                        await _questionOptionRepository.UpdateAsync(opt);
                    }
                }

                option.IsCorrect = newIsCorrect;
            }

            await _questionOptionRepository.UpdateAsync(option);
            await _questionOptionRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success(option.OptionId, 200, "Cập nhật đáp án thành công.");
        }
    }
}
