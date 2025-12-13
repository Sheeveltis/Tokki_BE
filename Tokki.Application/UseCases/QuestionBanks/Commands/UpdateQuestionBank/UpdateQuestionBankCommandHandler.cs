using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.UpdateQuestionBank
{
    public class UpdateQuestionBankCommandHandler : IRequestHandler<UpdateQuestionBankCommand, OperationResult<string>>
    {
        private readonly IQuestionBankRepository _questionBankRepository;
        private readonly IQuestionOptionRepository _questionOptionRepository;
        private readonly IQuestionTypeRepository _questionTypeRepository;
        private readonly IPassageRepository _passageRepository;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly ILogger<UpdateQuestionBankCommandHandler> _logger;

        public UpdateQuestionBankCommandHandler(
            IQuestionBankRepository questionBankRepository,
            IQuestionOptionRepository questionOptionRepository,
            IQuestionTypeRepository questionTypeRepository,
            IPassageRepository passageRepository,
            IIdGeneratorService idGeneratorService,
            ILogger<UpdateQuestionBankCommandHandler> logger)
        {
            _questionBankRepository = questionBankRepository;
            _questionOptionRepository = questionOptionRepository;
            _questionTypeRepository = questionTypeRepository;
            _passageRepository = passageRepository;
            _idGeneratorService = idGeneratorService;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(UpdateQuestionBankCommand request, CancellationToken cancellationToken)
        {
            var questionBank = await _questionBankRepository.GetByIdWithDetailsAsync(request.QuestionBankId, cancellationToken);
            if (questionBank == null)
            {
                return OperationResult<string>.Failure(
                    new List<Tokki.Application.Common.Models.Error> { AppErrors.QuestionBankNotFound },
                    404,
                    AppErrors.QuestionBankNotFound.Description
                );
            }

            if (!string.IsNullOrEmpty(request.QuestionTypeId))
            {
                var questionType = await _questionTypeRepository.GetByIdAsync(request.QuestionTypeId, cancellationToken);
                if (questionType == null)
                {
                    return OperationResult<string>.Failure(
                         new List<Tokki.Application.Common.Models.Error> { AppErrors.QuestionTypeNotFound },
                        404,
                        AppErrors.QuestionTypeNotFound.Description
                    );
                }
            }

            if (!string.IsNullOrEmpty(request.PassageId))
            {
                var passage = await _passageRepository.GetByIdAsync(request.PassageId, cancellationToken);
                if (passage == null)
                {
                    return OperationResult<string>.Failure(
                         new List<Tokki.Application.Common.Models.Error> { AppErrors.PassageNotFound },
                        404,
                        AppErrors.PassageNotFound.Description
                    );
                }
            }

            if (request.Options.Count < 2 || request.Options.Count > 4)
            {
                return OperationResult<string>.Failure(
                     new List<Tokki.Application.Common.Models.Error> { AppErrors.QuestionBankInvalidOptions },
                    400,
                    AppErrors.QuestionBankInvalidOptions.Description
                );
            }

            var validKeys = new HashSet<string> { "1", "2", "3", "4" };
            if (request.Options.Any(o => !validKeys.Contains(o.KeyOption)))
            {
                return OperationResult<string>.Failure(
                     new List<Tokki.Application.Common.Models.Error> { AppErrors.QuestionBankInvalidKeyOption },
                    400,
                    AppErrors.QuestionBankInvalidKeyOption.Description
                );
            }

            if (request.Options.Select(o => o.KeyOption).Distinct().Count() != request.Options.Count)
            {
                return OperationResult<string>.Failure(
                    new List<Tokki.Application.Common.Models.Error> { AppErrors.QuestionBankDuplicateKeyOption },
                    400,
                    AppErrors.QuestionBankDuplicateKeyOption.Description
                );
            }

            var correctCount = request.Options.Count(o => o.IsCorrect);
            if (correctCount == 0)
            {
                return OperationResult<string>.Failure(
                     new List<Tokki.Application.Common.Models.Error> { AppErrors.QuestionBankNoCorrectAnswer },
                    400,
                    AppErrors.QuestionBankNoCorrectAnswer.Description
                );
            }
            if (correctCount > 1)
            {
                return OperationResult<string>.Failure(
                     new List<Tokki.Application.Common.Models.Error> { AppErrors.QuestionBankMultipleCorrectAnswers },
                    400,
                    AppErrors.QuestionBankMultipleCorrectAnswers.Description
                );
            }

            try
            {
                questionBank.PassageId = string.IsNullOrEmpty(request.PassageId) ? null : request.PassageId;
                questionBank.QuestionTypeId = string.IsNullOrEmpty(request.QuestionTypeId) ? null : request.QuestionTypeId;
                questionBank.Skill = request.Skill;
                questionBank.Content = request.Content;
                questionBank.MediaUrl = request.MediaUrl;
                questionBank.Explanation = request.Explanation;
                questionBank.DifficultyLevel = request.DifficultyLevel;
                questionBank.IsActive = request.IsActive;

                await _questionBankRepository.UpdateAsync(questionBank);

                var existingOptions = questionBank.QuestionOptions.ToList();
                await _questionOptionRepository.DeleteRangeAsync(existingOptions);

                var newOptions = request.Options.Select(o => new QuestionOption
                {
                    OptionId = string.IsNullOrEmpty(o.OptionId) ? _idGeneratorService.GenerateCustom(10) : o.OptionId,
                    QuestionBankId = request.QuestionBankId,
                    KeyOption = o.KeyOption,
                    Content = o.Content,
                    ImageUrl = o.ImageUrl,
                    IsCorrect = o.IsCorrect
                }).ToList();

                await _questionOptionRepository.AddRangeAsync(newOptions);
                await _questionBankRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<string>.Success(
                    request.QuestionBankId,
                    200,
                    "Cập nhật câu hỏi thành công"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật câu hỏi: {QuestionBankId}", request.QuestionBankId);
                return OperationResult<string>.Failure(
                     new List<Tokki.Application.Common.Models.Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }
    }
}
