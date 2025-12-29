using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.VocabularyExample.Queries.GetByVocabularyId
{
    public class GetVocabularyExamplesByVocabularyIdQuery
          : IRequest<OperationResult<List<VocabularyExampleResponse>>>
    {
        public string VocabularyId { get; set; } = string.Empty;

    }
}
