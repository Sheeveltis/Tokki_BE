using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Categories.Commands.DeleteCategory
{
    public class DeleteCategoryCommand : IRequest<OperationResult<bool>>
    {
        public string Id { get; set; } = string.Empty;
    }
}
