using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.EmailTemplates.Commands.DeleteEmailTemplate
{
    public class DeleteEmailTemplateCommandHandler : IRequestHandler<DeleteEmailTemplateCommand, OperationResult<string>>
    {
        private readonly IEmailTemplateRepository _repository;

        public DeleteEmailTemplateCommandHandler(IEmailTemplateRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<string>> Handle(DeleteEmailTemplateCommand request, CancellationToken cancellationToken)
        {
            var template = await _repository.GetByIdAsync(request.TemplateId);

            if (template == null)
            {
                return OperationResult<string>.Failure("Không tìm thấy template!", 404);
            }

            await _repository.DeleteAsync(request.TemplateId);
            await _repository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success("Xóa template thành công!", 200);
        }
    }
}
