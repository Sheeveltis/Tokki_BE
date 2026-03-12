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
            try
            {
                var questionTypes = await _userExamRepository.GetIncorrectQuestionTypesByExamIdAsync(request.UserExamId, cancellationToken);

                var response = new ExamAnalysisResponse();

                foreach (var qt in questionTypes)
                {
                    var dto = new QuestionTypeDto
                    {
                        QuestionTypeId = qt.QuestionTypeId,
                        Code = qt.Code,
                        Name = qt.Name
                    };
                    switch (qt.Skill)
                    {
                        case QuestionSkill.Reading:
                            response.ReadingIssues.Add(dto);
                            break;
                        case QuestionSkill.Listening:
                            response.ListeningIssues.Add(dto);
                            break;
                        case QuestionSkill.Writing:
                            response.WritingIssues.Add(dto);
                            break;
                    }
                }

                return OperationResult<ExamAnalysisResponse>.Success(response, 200, "Phân tích điểm yếu hoàn tất.");
            }
            catch (Exception ex)
            {
                return OperationResult<ExamAnalysisResponse>.Failure(new Error("ANALYSIS_ERROR", ex.Message));
            }
        }
    }
}
