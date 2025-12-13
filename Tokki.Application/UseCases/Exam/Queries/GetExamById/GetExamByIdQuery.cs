using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Exam.DTOs;

namespace Tokki.Application.UseCases.Exam.Queries.GetExamById
{
    public class GetExamByIdQuery : IRequest<OperationResult<ExamDto>>
    {
        public string ExamId { get; set; } = string.Empty;
    }
}
