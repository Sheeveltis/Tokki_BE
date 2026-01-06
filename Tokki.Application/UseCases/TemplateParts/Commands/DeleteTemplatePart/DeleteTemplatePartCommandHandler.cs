using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.TemplateParts.Commands.DeleteTemplatePart
{
    public class DeleteTemplatePartCommandHandler : IRequestHandler<DeleteTemplatePartCommand, OperationResult<string>>
    {
        private readonly ITemplatePartRepository _templatePartRepository;
        private readonly ILogger<DeleteTemplatePartCommandHandler> _logger;

        public DeleteTemplatePartCommandHandler(
            ITemplatePartRepository templatePartRepository,
            ILogger<DeleteTemplatePartCommandHandler> logger)
        {
            _templatePartRepository = templatePartRepository;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(DeleteTemplatePartCommand request, CancellationToken cancellationToken)
        {
            var part = await _templatePartRepository.GetByIdAsync(request.TemplatePartId, cancellationToken);

            if (part == null) return OperationResult<string>.Failure(AppErrors.TemplatePartNotFound);
            try
            {
                await _templatePartRepository.DeleteAsync(part);
                await _templatePartRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<string>.Success(request.TemplatePartId, 200, "Xóa phần thi thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa Template Part: {Id}", request.TemplatePartId);
                return OperationResult<string>.Failure(AppErrors.ServerError);
            }
        }
    }
}