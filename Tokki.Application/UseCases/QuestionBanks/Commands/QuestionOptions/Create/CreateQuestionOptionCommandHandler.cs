using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.QuestionOptions.Create
{
    public class CreateQuestionOptionCommandHandler : IRequestHandler<CreateQuestionOptionCommand, OperationResult<string>>
    {
        private readonly IQuestionBankRepository _questionBankRepository;
        private readonly IQuestionOptionRepository _questionOptionRepository;
        private readonly IQuestionTypeRepository _questionTypeRepository;
        private readonly IIdGeneratorService _idGenerator;

        public CreateQuestionOptionCommandHandler(
            IQuestionBankRepository questionBankRepository,
            IQuestionOptionRepository questionOptionRepository,
            IQuestionTypeRepository questionTypeRepository,
            IIdGeneratorService idGenerator)
        {
            _questionBankRepository = questionBankRepository;
            _questionOptionRepository = questionOptionRepository;
            _questionTypeRepository = questionTypeRepository;
            _idGenerator = idGenerator;
        }

        public async Task<OperationResult<string>> Handle(CreateQuestionOptionCommand request, CancellationToken cancellationToken)
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

            var key = request.KeyOption.Trim();
            var currentOptions = qb.QuestionOptions?.ToList() ?? new List<QuestionOption>();

            if (currentOptions.Count >= 4)
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ValidationFailed },
                    400,
                    "Không thể thêm quá 4 đáp án.");
            }

            if (currentOptions.Any(o => o.KeyOption == key))
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ValidationFailed },
                    400,
                    $"KeyOption '{key}' đã tồn tại trong câu hỏi này.");
            }

            // Nếu set đáp án đúng => gỡ đúng của option khác
            if (request.IsCorrect)
            {
                foreach (var opt in currentOptions.Where(o => o.IsCorrect))
                {
                    opt.IsCorrect = false;
                    await _questionOptionRepository.UpdateAsync(opt);
                }
            }

            var newOptionId = _idGenerator.GenerateCustom(10);

            var option = new QuestionOption
            {
                OptionId = newOptionId,
                QuestionBankId = qb.QuestionBankId,
                KeyOption = key,
                Content = string.IsNullOrWhiteSpace(request.Content) ? null : request.Content,
                ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl,
                IsCorrect = request.IsCorrect
            };

            await _questionOptionRepository.AddAsync(option);
            await _questionOptionRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success(newOptionId, 201, "Thêm đáp án thành công.");
        }
    }
}
