using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.QuestionBanks.Commands.CreateQuestionBank;
using Tokki.Domain.Entities;

public class CreateQuestionBankCommandHandler : IRequestHandler<CreateQuestionBankCommand, OperationResult<string>>
{
    private readonly IQuestionBankRepository _questionBankRepository;
    private readonly IQuestionOptionRepository _questionOptionRepository;
    private readonly IQuestionTypeRepository _questionTypeRepository;
    private readonly IPassageRepository _passageRepository;
    private readonly IIdGeneratorService _idGeneratorService;
    private readonly ILogger<CreateQuestionBankCommandHandler> _logger;

    public CreateQuestionBankCommandHandler(
        IQuestionBankRepository questionBankRepository,
        IQuestionOptionRepository questionOptionRepository,
        IQuestionTypeRepository questionTypeRepository,
        IPassageRepository passageRepository,
        IIdGeneratorService idGeneratorService,
        ILogger<CreateQuestionBankCommandHandler> logger)
    {
        _questionBankRepository = questionBankRepository;
        _questionOptionRepository = questionOptionRepository;
        _questionTypeRepository = questionTypeRepository;
        _passageRepository = passageRepository;
        _idGeneratorService = idGeneratorService;
        _logger = logger;
    }

    public async Task<OperationResult<string>> Handle(CreateQuestionBankCommand request, CancellationToken cancellationToken)
    {
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
        }

        if (request.Options.Count < 2 || request.Options.Count > 4)
        {
            return OperationResult<string>.Failure(
                new List<Error> { AppErrors.QuestionBankInvalidOptions },
                400,
                AppErrors.QuestionBankInvalidOptions.Description
            );
        }

        var validKeys = new HashSet<string> { "1", "2", "3", "4" };
        if (request.Options.Any(o => !validKeys.Contains(o.KeyOption)))
        {
            return OperationResult<string>.Failure(
                new List<Error> { AppErrors.QuestionBankInvalidKeyOption },
                400,
                AppErrors.QuestionBankInvalidKeyOption.Description
            );
        }

        if (request.Options.Select(o => o.KeyOption).Distinct().Count() != request.Options.Count)
        {
            return OperationResult<string>.Failure(
                new List<Error> { AppErrors.QuestionBankDuplicateKeyOption },
                400,
                AppErrors.QuestionBankDuplicateKeyOption.Description
            );
        }

        var correctCount = request.Options.Count(o => o.IsCorrect);
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

        try
        {
            string questionBankId = _idGeneratorService.GenerateCustom(10);

            var questionBank = new QuestionBank
            {
                QuestionBankId = questionBankId,
                PassageId = string.IsNullOrEmpty(request.PassageId) ? null : request.PassageId,
                QuestionTypeId = string.IsNullOrEmpty(request.QuestionTypeId) ? null : request.QuestionTypeId,
                Skill = request.Skill,
                Content = request.Content,
                MediaUrl = request.MediaUrl,
                Explanation = request.Explanation,
                DifficultyLevel = request.DifficultyLevel,
                IsActive = true
            };

            await _questionBankRepository.AddAsync(questionBank);

            var options = request.Options.Select(o => new QuestionOption
            {
                OptionId = _idGeneratorService.GenerateCustom(10),
                QuestionBankId = questionBankId,
                KeyOption = o.KeyOption,
                Content = o.Content,
                ImageUrl = o.ImageUrl,
                IsCorrect = o.IsCorrect
            }).ToList();

            await _questionOptionRepository.AddRangeAsync(options);
            await _questionBankRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success(
                questionBankId,
                201,
                "Tạo câu hỏi thành công"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo câu hỏi");
            return OperationResult<string>.Failure(
                new List<Error> { AppErrors.ServerError },
                500,
                AppErrors.ServerError.Description
            );
        }
    }
}