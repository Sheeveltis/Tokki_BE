using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.TopikLevelConfigs.Commands.Update
{
    public class UpdateTopikLevelConfigCommand : IRequest<OperationResult<bool>>
    {
        public int TopikLevelConfigID { get; set; }
        public int TargetAimLevel { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public int PassScore { get; set; }
        public int TotalScore { get; set; }
        public int ExamGroup { get; set; }
        public string ConfigKey { get; set; } = string.Empty;
        public int ListeningMaxQuestions { get; set; }
        public int ListeningMaxScore { get; set; }
        public int ReadingMaxQuestions { get; set; }
        public int ReadingMaxScore { get; set; }
        public int WritingMaxQuestions { get; set; }
        public int WritingMaxScore { get; set; }
        public int TargetListeningQuestions { get; set; }
        public int TargetListeningScore { get; set; }
        public int TargetReadingQuestions { get; set; }
        public int TargetReadingScore { get; set; }
        public int TargetWritingQuestions { get; set; }
        public int TargetWritingScore { get; set; }
        public string? Strategy { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
    }
}
