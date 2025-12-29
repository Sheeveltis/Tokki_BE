using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Vocabulary.DTOs;

namespace Tokki.Application.UseCases.Vocabulary.Queries.GetByIdForUser
{
    public class GetVocabularyDetailByIdForAdminQuery : IRequest<OperationResult<VocabularyDetailForAdminDto>>
    {
        public string VocabularyId { get; set; } = string.Empty;
    }
}
