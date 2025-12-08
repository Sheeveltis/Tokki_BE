using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.EmailTemplates.Queries
{
    public class GetAllEmailTemplatesQuery : IRequest<OperationResult<List<EmailTemplate>>>
    {
    }
}
