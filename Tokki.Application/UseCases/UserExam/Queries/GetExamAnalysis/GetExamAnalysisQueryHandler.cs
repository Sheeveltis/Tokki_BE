using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.UserExam.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.UserExam.Queries.GetExamAnalysis
{
    public class GetExamAnalysisQueryHandler : IRequestHandler<GetExamAnalysisQuery, OperationResult<ExamAnalysisResponse>>
    {
        private readonly IUserExamRepository _userExamRepository;

        public GetExamAnalysisQueryHandler(IUserExamRepository userExamRepository)
        {
            _userExamRepository = userExamRepository;
        }

        public async Task<OperationResult<ExamAnalysisResponse>> Handle(GetExamAnalysisQuery request, CancellationToken cancellationToken)
        {
            var session = await _userExamRepository.GetByIdAsync(request.UserExamId, cancellationToken);
            if (session == null)
                return OperationResult<ExamAnalysisResponse>.Failure("Không tìm thấy kết quả bài thi.", 404);

            if (session.Status == UserExamStatus.InProgress)
                return OperationResult<ExamAnalysisResponse>.Failure("Bạn chưa nộp bài thi nên chưa thể xem phân tích.", 400);

            var analysis = await _userExamRepository.GetExamAnalysisSummaryAsync(request.UserExamId, cancellationToken);

            var response = new ExamAnalysisResponse();

            if (analysis == null || !analysis.Any())
            {
                return OperationResult<ExamAnalysisResponse>.Success(response, 200, "Không tìm thấy dữ liệu để phân tích.");
            }

            foreach (var dto in analysis)
            {
                switch (dto.Skill)
                {
                    case QuestionSkill.Reading:
                        response.ReadingAnalysis.Add(dto);
                        break;
                    case QuestionSkill.Listening:
                        response.ListeningAnalysis.Add(dto);
                        break;
                    case QuestionSkill.Writing:
                        response.WritingAnalysis.Add(dto);
                        break;
                }
            }

            return OperationResult<ExamAnalysisResponse>.Success(response, 200, "Phân tích bài thi hoàn tất.");
        }
    }
}
