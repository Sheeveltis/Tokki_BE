using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;

namespace  Tokki.Application.UseCases.Categories.Commands.CreateCategory
{
    public class CreateCategoryCommand : IRequest<OperationResult<string>>
    {
        public string Name { get; set; } = string.Empty;
    }
}
