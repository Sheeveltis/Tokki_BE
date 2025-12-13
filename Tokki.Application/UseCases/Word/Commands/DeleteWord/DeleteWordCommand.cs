using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Word.Commands.DeleteWord
{
    public class DeleteWordCommand : IRequest<OperationResult<bool>>
    {
        public string WordId { get; set; } = string.Empty;
        public bool ForceDelete { get; set; } = false;
    }
}
