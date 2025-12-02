using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Categories.DTOs;

namespace Tokki.Application.UseCases.Categories.Queries
{
    public class GetAllCategoriesQuery : IRequest<OperationResult<IEnumerable<CategoryDTO>>>
    {
    }
}
