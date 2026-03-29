using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.IRepositories
{
    public class ExamStatProjection
    {
        public string ExamId { get; set; } = string.Empty;
        public string ExamTemplateId { get; set; } = string.Empty;
        public string? ExamTemplateName { get; set; }
        public string Title { get; set; } = string.Empty;
        public ExamType Type { get; set; }
        public ExamStatus Status { get; set; }
        public int Duration { get; set; }
        public string? SkillDurations { get; set; }
        public DateTime CreatedAt { get; set; }
        public int PdfDownloadCount { get; set; }

        public int TotalParticipants { get; set; }
        public double AverageScore { get; set; }
        public int TopScore { get; set; }
        public double AverageDurationMinutes { get; set; }
        public int InProgressCount { get; set; }
        public int CompletedCount { get; set; }
        public int TotalQuestions { get; set; }

        public List<TemplatePartStatProjection> TemplateParts { get; set; } = new();
        public List<int> QuestionNumbers { get; set; } = new();
    }

    public class TemplatePartStatProjection
    {
        public QuestionSkill Skill { get; set; }
        public int QuestionFrom { get; set; }
        public int QuestionTo { get; set; }
    }

    public interface IExamRepository
    {
        Task<Exam?> GetByIdAsync(string examId, CancellationToken cancellationToken = default);
        Task<Exam?> GetByIdWithDetailsAsync(string examId, CancellationToken cancellationToken = default);
        Task<(IEnumerable<Exam> items, int totalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            ExamType? type = null,
            ExamStatus? status = null,
            string? examTemplateId = null,
            ExamCreatorFilter creatorFilter = ExamCreatorFilter.All,
            CancellationToken cancellationToken = default);

        Task<(IEnumerable<ExamStatProjection> items, int totalCount)> GetPagedWithStatsAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            ExamType? type = null,
            ExamStatus? status = null,
            ExamCreatorFilter creatorFilter = ExamCreatorFilter.All,
            CancellationToken cancellationToken = default);

        Task<bool> IsTitleExistsAsync(string title, string? excludeId = null, CancellationToken cancellationToken = default);
        Task<int> GetQuestionCountAsync(string examId, CancellationToken cancellationToken = default);
        Task AddAsync(Exam exam);
        Task UpdateAsync(Exam exam);
        Task DeleteAsync(Exam exam);
        Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<Exam?> GetExamWithFullDetailsAsync(string examId, CancellationToken cancellationToken);
        Task<Exam?> GetEntranceExamByTypeAsync(
            ExamType examType,
            CancellationToken cancellationToken = default);
        Task<List<string>> GetRecentQuestionIdsAsync(int examCount, CancellationToken cancellationToken = default);
    }
}
