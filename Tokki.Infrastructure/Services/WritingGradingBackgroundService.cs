// Infrastructure/BackgroundJobs/WritingGradingBackgroundService.cs
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.TopikWriting.Question51.DTOs;
using Tokki.Application.UseCases.TopikWriting.Question52.DTOs;
using Tokki.Application.UseCases.TopikWriting.Question53.DTOs;
using Tokki.Application.UseCases.TopikWriting.Question54.DTOs;

namespace Tokki.Infrastructure.BackgroundJobs
{
    public class WritingGradingBackgroundService : IWritingGradingBackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public WritingGradingBackgroundService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 60, 120 })]
        public async Task GradeQuestion51Async(string userExamWritingAnswerId)
        {
            using var scope = _scopeFactory.CreateScope();
            var pipeline = scope.ServiceProvider.GetRequiredService<IQuestion51Pipeline>();
            await pipeline.SolveAsync(
                new Question51RequestDto { UserExamWritingAnswerId = userExamWritingAnswerId },
                CancellationToken.None);
        }

        [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 60, 120 })]
        public async Task GradeQuestion52Async(string userExamWritingAnswerId)
        {
            using var scope = _scopeFactory.CreateScope();
            var pipeline = scope.ServiceProvider.GetRequiredService<IQuestion52Pipeline>();
            await pipeline.SolveAsync(
                new Question52RequestDto { UserExamWritingAnswerId = userExamWritingAnswerId },
                CancellationToken.None);
        }

        [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 60, 120 })]
        public async Task GradeQuestion53Async(string userExamWritingAnswerId)
        {
            using var scope = _scopeFactory.CreateScope();
            var pipeline = scope.ServiceProvider.GetRequiredService<IQuestion53Pipeline>();
            await pipeline.SolveAsync(
                new Question53RequestDto { UserExamWritingAnswerId = userExamWritingAnswerId },
                CancellationToken.None);
        }

        [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 60, 120 })]
        public async Task GradeQuestion54Async(string userExamWritingAnswerId)
        {
            using var scope = _scopeFactory.CreateScope();
            var pipeline = scope.ServiceProvider.GetRequiredService<IQuestion54Pipeline>();
            await pipeline.SolveAsync(
                new Question54RequestDto { UserExamWritingAnswerId = userExamWritingAnswerId },
                CancellationToken.None);
        }

        public async Task GradeAllWritingByUserExamAsync(string userExamId)
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IUserExamRepository>();
            var backgroundJobs = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();

            var session = await repository.GetByIdWithWritingDetailsAsync(userExamId, CancellationToken.None);
            if (session == null) return;

            var writingParts = session.Exam.ExamTemplate.TemplateParts
                .Where(p => p.Skill == Domain.Enums.QuestionSkill.Writing)
                .ToList();

            var writingAnswers = session.UserExamWritingAnswers.ToList();

            foreach (var part in writingParts)
            {
                var matchingAnswer = writingAnswers
                    .FirstOrDefault(a => a.OrderIndex == part.QuestionFrom);

                if (matchingAnswer == null) continue;

                var code = part.QuestionType?.Code ?? string.Empty;
                var answerId = matchingAnswer.UserExamWritingAnswerId;

                switch (code)
                {
                    case "TOPIK2_W_Q51":
                        backgroundJobs.Enqueue<IWritingGradingBackgroundService>(
                            s => s.GradeQuestion51Async(answerId));
                        break;
                    case "TOPIK2_W_Q52":
                        backgroundJobs.Enqueue<IWritingGradingBackgroundService>(
                            s => s.GradeQuestion52Async(answerId));
                        break;
                    case "TOPIK2_W_Q53":
                        backgroundJobs.Enqueue<IWritingGradingBackgroundService>(
                            s => s.GradeQuestion53Async(answerId));
                        break;
                    case "TOPIK2_W_Q54":
                        backgroundJobs.Enqueue<IWritingGradingBackgroundService>(
                            s => s.GradeQuestion54Async(answerId));
                        break;
                }
            }
        }
    }
}