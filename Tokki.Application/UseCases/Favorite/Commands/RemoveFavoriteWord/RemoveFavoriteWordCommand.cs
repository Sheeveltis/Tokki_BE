using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Favorite.Commands.RemoveFavoriteWord
{
    public class RemoveFavoriteWordCommand : IRequest<OperationResult<bool>>
    {
        public string WordId { get; set; } = string.Empty;
        public bool ForceDelete { get; set; } = false;
    }
}
