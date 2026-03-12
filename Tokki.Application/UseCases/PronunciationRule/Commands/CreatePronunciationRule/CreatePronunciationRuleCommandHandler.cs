using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.PronunciationRule.Commands.CreatePronunciationRule
{
    public class CreatePronunciationRuleCommandHandler : IRequestHandler<CreatePronunciationRuleCommand, OperationResult<string>>
    {
        private readonly IPronunciationRuleRepository _repository;
        private readonly IIdGeneratorService _idGenerator;

        public CreatePronunciationRuleCommandHandler(
            IPronunciationRuleRepository repository,
            IIdGeneratorService idGenerator)
        {
            _repository = repository;
            _idGenerator = idGenerator;
        }

        public async Task<OperationResult<string>> Handle(CreatePronunciationRuleCommand request, CancellationToken cancellationToken)
        {
            bool isDuplicate = await _repository.IsRuleNameExistsAsync(request.RuleName.Trim());
            if (isDuplicate)
            {
                return OperationResult<string>.Failure($"Tên quy tắc '{request.RuleName}' đã tồn tại.", 400);
            }

            var ruleId = _idGenerator.GenerateCustom(10);
            var entity = new Domain.Entities.PronunciationRule
            {
                PronunciationRuleId = ruleId,
                RuleName = request.RuleName.Trim(),
                Description = request.Description?.Trim(),
                Content = request.Content,
                IsDeleted = false,
                CreateBy = request.CreateBy,
                CreateDate = DateTime.UtcNow
            };

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success(ruleId,200 ,OperationMessages.CreateSuccess("quy tắc phát âm"));
        }
    }
}
