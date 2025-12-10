using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.EmailTemplates.Commands.UpdateEmailTemplate;

namespace Tokki.Application.UseCases.EmailTemplates.Handlers
{
    public class UpdateEmailTemplateCommandHandler : IRequestHandler<UpdateEmailTemplateCommand, OperationResult<string>>
    {
        private readonly IEmailTemplateRepository _repository;

        public UpdateEmailTemplateCommandHandler(IEmailTemplateRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<string>> Handle(UpdateEmailTemplateCommand request, CancellationToken cancellationToken)
        {
            var template = await _repository.GetByIdAsync(request.TemplateId);
            if (template == null)
            {
                return OperationResult<string>.Failure(new List<Error> { AppErrors.EmailTemplateNotFound });
            }


            template.Subject = request.Subject;
            template.Body = request.Body;
            template.Description = request.Description;
            template.UpdatedAt = DateTime.UtcNow.AddHours(7);

            await _repository.UpdateAsync(template);
            await _repository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success("Cập nhật template thành công!", 200);
        }
    }
}