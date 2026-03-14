using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.UserExam.DTOs;

namespace Tokki.Application.UseCases.UserExam.Queries.GetExamAnalysis
{
    public record GetExamAnalysisQuery(string UserExamId) : IRequest<OperationResult<ExamAnalysisResponse>>;
}
