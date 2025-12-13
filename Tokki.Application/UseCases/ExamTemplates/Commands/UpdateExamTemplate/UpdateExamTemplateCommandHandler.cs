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

namespace Tokki.Application.UseCases.ExamTemplates.Commands.UpdateExamTemplate
{
    public class UpdateExamTemplateCommandHandler : IRequestHandler<UpdateExamTemplateCommand, OperationResult<string>>
    {
        private readonly IExamTemplateRepository _examTemplateRepository;
        private readonly ITemplatePartRepository _templatePartRepository;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly ILogger<UpdateExamTemplateCommandHandler> _logger;

        public UpdateExamTemplateCommandHandler(
            IExamTemplateRepository examTemplateRepository,
            ITemplatePartRepository templatePartRepository,
            IIdGeneratorService idGeneratorService,
            ILogger<UpdateExamTemplateCommandHandler> logger)
        {
            _examTemplateRepository = examTemplateRepository;
            _templatePartRepository = templatePartRepository;
            _idGeneratorService = idGeneratorService;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(UpdateExamTemplateCommand request, CancellationToken cancellationToken)
        {
            var examTemplate = await _examTemplateRepository.GetByIdWithPartsAsync(request.ExamTemplateId, cancellationToken);
            if (examTemplate == null)
            {
                return OperationResult<string>.Failure(
                    new List<Tokki.Application.Common.Models.Error> { AppErrors.ExamTemplateNotFound },
                    404,
                    AppErrors.ExamTemplateNotFound.Description
                );
            }

            // Validate tên không trùng
            bool nameExists = await _examTemplateRepository.IsNameExistsAsync(request.Name, request.ExamTemplateId);
            if (nameExists)
            {
                return OperationResult<string>.Failure(
                    new List<Tokki.Application.Common.Models.Error> { AppErrors.ExamTemplateNameDuplicated },
                    409,
                    AppErrors.ExamTemplateNameDuplicated.Description
                );
            }

            // Validate phải có ít nhất 1 part
            if (request.Parts.Count == 0)
            {
                return OperationResult<string>.Failure(
                     new List<Tokki.Application.Common.Models.Error> { AppErrors.ExamTemplateNoParts },
                    400,
                    AppErrors.ExamTemplateNoParts.Description
                );
            }

            // Validate range của từng part
            foreach (var part in request.Parts)
            {
                if (part.QuestionFrom > part.QuestionTo)
                {
                    return OperationResult<string>.Failure(
                        new List<Tokki.Application.Common.Models.Error> { AppErrors.TemplatePartInvalidRange },
                        400,
                        $"Phần '{part.PartTitle}': {AppErrors.TemplatePartInvalidRange.Description}"
                    );
                }

                if (part.QuestionFrom <= 0 || part.QuestionTo <= 0)
                {
                    return OperationResult<string>.Failure(
                         new List<Tokki.Application.Common.Models.Error> { AppErrors.TemplatePartQuestionRangeInvalid },
                        400,
                        $"Phần '{part.PartTitle}': {AppErrors.TemplatePartQuestionRangeInvalid.Description}"
                    );
                }
            }

            // Validate không có range bị overlap
            var sortedParts = request.Parts.OrderBy(p => p.QuestionFrom).ToList();
            for (int i = 0; i < sortedParts.Count - 1; i++)
            {
                if (sortedParts[i].QuestionTo >= sortedParts[i + 1].QuestionFrom)
                {
                    return OperationResult<string>.Failure(
                        new List<Tokki.Application.Common.Models.Error> { AppErrors.TemplatePartRangeOverlap },
                        400,
                        $"Phần '{sortedParts[i].PartTitle}' và '{sortedParts[i + 1].PartTitle}': {AppErrors.TemplatePartRangeOverlap.Description}"
                    );
                }
            }

            try
            {
                examTemplate.Name = request.Name;
                examTemplate.Description = request.Description;
                examTemplate.Status = request.Status;

                await _examTemplateRepository.UpdateAsync(examTemplate);

                // Xóa tất cả parts cũ
                var existingParts = examTemplate.TemplateParts.ToList();
                await _templatePartRepository.DeleteRangeAsync(existingParts);

                // Thêm parts mới
                var newParts = request.Parts.Select(p => new TemplatePart
                {
                    TemplatePartId = string.IsNullOrEmpty(p.TemplatePartId)
                        ? _idGeneratorService.GenerateCustom(10)
                        : p.TemplatePartId,
                    ExamTemplateId = request.ExamTemplateId,
                    Skill = p.Skill,
                    QuestionFrom = p.QuestionFrom,
                    QuestionTo = p.QuestionTo,
                    PartTitle = p.PartTitle,
                    Instruction = p.Instruction,
                    ExampleType = p.ExampleType,
                    ExampleData = p.ExampleData
                }).ToList();

                await _templatePartRepository.AddRangeAsync(newParts);
                await _examTemplateRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<string>.Success(
                    request.ExamTemplateId,
                    200,
                    "Cập nhật mẫu đề thi thành công"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật mẫu đề thi: {ExamTemplateId}", request.ExamTemplateId);
                return OperationResult<string>.Failure(
                    new List<Tokki.Application.Common.Models.Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }
    }
    }
