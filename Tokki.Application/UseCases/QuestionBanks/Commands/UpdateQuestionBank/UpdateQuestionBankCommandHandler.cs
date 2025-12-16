using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<UpdateQuestionBankCommandHandler> _logger;

        public UpdateQuestionBankCommandHandler(
            IQuestionBankRepository questionBankRepository,
            IQuestionTypeRepository questionTypeRepository,
            IPassageRepository passageRepository,
            ILogger<UpdateQuestionBankCommandHandler> logger)
        {
            _questionBankRepository = questionBankRepository;
            _questionTypeRepository = questionTypeRepository;
            _passageRepository = passageRepository;
            _logger = logger;
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

            if (!string.IsNullOrEmpty(request.QuestionTypeId))
            {
                var questionType = await _questionTypeRepository.GetByIdAsync(request.QuestionTypeId, cancellationToken);
                if (questionType == null)
                {
                    return OperationResult<string>.Failure(
                        new List<Error> { AppErrors.QuestionTypeNotFound },
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
                        new List<Error> { AppErrors.PassageNotFound },
                        404,
                        AppErrors.PassageNotFound.Description
                    );
                }

                bool isMediaTypeValid = false;

                switch (request.Skill)
                {
                    case QuestionSkill.Listening:
                        isMediaTypeValid = passage.MediaType == PassageMediaType.Audio;
                        break;

                    case QuestionSkill.Reading:
                        isMediaTypeValid = passage.MediaType == PassageMediaType.Text ||
                                           passage.MediaType == PassageMediaType.Image;
                        break;

                    case QuestionSkill.Writing:
                        isMediaTypeValid = passage.MediaType == PassageMediaType.Text ||
                                           passage.MediaType == PassageMediaType.Image;
                        break;

                    default:
                        isMediaTypeValid = false;
                        break;
                }

                if (!isMediaTypeValid)
                {
                    return OperationResult<string>.Failure(
                        new List<Error> { AppErrors.PassageMediaTypeMismatch(passage.MediaType, request.Skill) },
                        400,
                        "Thất bại."
                    );
                }
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
                    new List<Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }
    }
}