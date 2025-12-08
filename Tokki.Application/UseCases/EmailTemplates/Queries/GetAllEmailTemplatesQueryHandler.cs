using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.EmailTemplates.Queries
{
    public class GetAllEmailTemplatesQueryHandler : IRequestHandler<GetAllEmailTemplatesQuery, OperationResult<List<EmailTemplate>>>
    {
        private readonly IEmailTemplateRepository _repository;

        public GetAllEmailTemplatesQueryHandler(IEmailTemplateRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<List<EmailTemplate>>> Handle(GetAllEmailTemplatesQuery request, CancellationToken cancellationToken)
        {
            var templates = await _repository.GetAllAsync();
            return OperationResult<List<EmailTemplate>>.Success(templates, 200);
        }
    }
}