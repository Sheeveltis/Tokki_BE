using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.FavoriteVocabulary.Commands.AddFavoriteVocabulary
{
    public class AddFavoriteVocabularyCommand : IRequest<OperationResult<bool>>
    {
        public string VocabularyId { get; set; } = string.Empty;
    }
}
