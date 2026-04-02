using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.UserExam.DTOs;

namespace Tokki.Application.UseCases.UserExam.Queries.GetGradingProgress
{
    public record GetGradingProgressQuery(string UserExamId) : IRequest<OperationResult<GradingProgressResponse>>;
}
