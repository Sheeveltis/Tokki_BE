using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.EmailTemplates.Commands.DeleteEmailTemplate
{
    public class DeleteEmailAutoTemplateCommand : IRequest<OperationResult<string>>
    {
        public string TemplateId { get; set; } = string.Empty; 
    }
}