using MediatR;
using Tokki.Application.Common.Models;

using Tokki.Application.UseCases.Exam.DTOs;

namespace Tokki.Application.UseCases.Exam.Commands.ExportExamToPdf
{
    public class ExportExamToPdfCommand : IRequest<OperationResult<ExportExamPdfResponse>>
    {
        public string ExamId { get; set; } = string.Empty;
        public bool ShowExplanation { get; set; } = false;

        public ExportExamToPdfCommand(string examId, bool showExplanation)
        {
            ExamId = examId;
            ShowExplanation = showExplanation;
        }
    }
}
