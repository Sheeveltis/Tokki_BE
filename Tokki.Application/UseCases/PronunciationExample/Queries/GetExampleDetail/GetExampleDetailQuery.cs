using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.PronunciationExample.DTOs;

namespace Tokki.Application.UseCases.PronunciationExample.Queries.GetExampleDetail
{
    public class GetExampleDetailQuery : IRequest<OperationResult<ExampleDetailDTO>>
    {
        public string ExampleId { get; set; }
        public GetExampleDetailQuery(string exampleId)
        {
            ExampleId = exampleId;
        }
    }
}
