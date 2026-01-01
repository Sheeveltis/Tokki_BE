using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Excel.DTOs;

namespace Tokki.Application.UseCases.Excel.Queries.ExportVocabByTopic
{
    public class ExportVocabByTopicQuery : IRequest<OperationResult<ExportFileDTO>>
    {
        public string TopicId { get; set; }
    }
}
