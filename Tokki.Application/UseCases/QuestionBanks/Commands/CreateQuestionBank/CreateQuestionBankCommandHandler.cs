using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.QuestionBanks.Commands.CreateQuestionBank;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

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
        // Validate QuestionType nếu có
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

        // Validate Passage nếu có
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

            // --- BẮT ĐẦU ĐOẠN VALIDATE LOGIC KHỚP SKILL & MEDIA TYPE ---
            bool isMediaTypeValid = false;

            switch (request.Skill)
            {
                case QuestionSkill.Listening:
                    // Kỹ năng Nghe -> Bắt buộc Passage là Audio
                    isMediaTypeValid = passage.MediaType == PassageMediaType.Audio;
                    break;


                case QuestionSkill.Reading:
                    // Kỹ năng Đọc -> Cho phép Passage là Văn bản (Text) HOẶC Hình ảnh (Image)
                    isMediaTypeValid = passage.MediaType == PassageMediaType.Text || passage.MediaType == PassageMediaType.Image;
                    break; // Thêm break để kết thúc case này


                case QuestionSkill.Writing:
                    // Kỹ năng Viết -> Thường dựa trên Text hoặc Image
                    isMediaTypeValid = passage.MediaType == PassageMediaType.Text ||
                                       passage.MediaType == PassageMediaType.Image;
                    break;

                default:
                    // Các trường hợp skill khác (nếu có)
                    isMediaTypeValid = false;
                    break;
            }

            if (!isMediaTypeValid)
            {
                if (!isMediaTypeValid)
                {
                    return OperationResult<string>.Failure(
                        new List<Error> { AppErrors.PassageMediaTypeMismatch(passage.MediaType, request.Skill) },
                        400,
                        "Thất bại."
                    );
                }
            }
        }

        // Nếu là câu hỏi Writing (tự luận), không được có Options
        if (request.Skill == QuestionSkill.Writing)
        {
            if (request.Options != null && request.Options.Any())
            {
                return OperationResult<string>.Failure(
                     new List<Error> { AppErrors.WritingNoOptions },
                     400,
                     "Thất bại."
                 );
            }
        }
        else
        {
            // Nếu KHÔNG phải Writing, bắt buộc phải có Options
            if (request.Options == null || request.Options.Count < 2 || request.Options.Count > 4)
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.QuestionBankInvalidOptions },
                    400,
                    "Thất bại."
                );
            }

            // Validate KeyOption
            var validKeys = new HashSet<string> { "1", "2", "3", "4" };
            if (request.Options.Any(o => !validKeys.Contains(o.KeyOption)))
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.QuestionBankInvalidKeyOption },
                    400,
                    "Thất bại."
                );
            }

            // Validate không trùng KeyOption
            if (request.Options.Select(o => o.KeyOption).Distinct().Count() != request.Options.Count)
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.QuestionBankDuplicateKeyOption },
                    400,
                    "Thất bại."
                );
            }

            // Validate phải có đúng 1 đáp án đúng
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

            // Chỉ tạo Options nếu KHÔNG phải câu hỏi Writing
            if (request.Skill != QuestionSkill.Writing && request.Options != null && request.Options.Any())
            {
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
            }

            await _questionBankRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success(
                questionBankId,
                201,
                "Thành công"
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