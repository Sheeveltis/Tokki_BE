// Infrastructure/Services/WritingGradingBackgroundService.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.TopikWriting.Question51.DTOs;
using Tokki.Application.UseCases.TopikWriting.Question52.DTOs;
using Tokki.Application.UseCases.TopikWriting.Question53.DTOs;
using Tokki.Application.UseCases.TopikWriting.Question54.DTOs;

namespace Tokki.Infrastructure.Services
{
    public class WritingGradingBackgroundService : IWritingGradingBackgroundService
    {
        private readonly IQuestion51Pipeline _q51Pipeline;
        private readonly IQuestion52Pipeline _q52Pipeline;
        private readonly IQuestion53Pipeline _q53Pipeline;
        private readonly IQuestion54Pipeline _q54Pipeline;

        public WritingGradingBackgroundService(
            IQuestion51Pipeline q51Pipeline,
            IQuestion52Pipeline q52Pipeline,
            IQuestion53Pipeline q53Pipeline,
            IQuestion54Pipeline q54Pipeline)
        {
            _q51Pipeline = q51Pipeline;
            _q52Pipeline = q52Pipeline;
            _q53Pipeline = q53Pipeline;
            _q54Pipeline = q54Pipeline;
        }

        public async Task GradeQuestion51Async(string userExamWritingAnswerId)
        {
            try
            {
                Console.WriteLine($"🔄 [Q51] Bắt đầu chấm: {userExamWritingAnswerId}");

                var request = new Question51RequestDto
                {
                    UserExamWritingAnswerId = userExamWritingAnswerId
                };

                await _q51Pipeline.SolveAsync(request, CancellationToken.None);

                Console.WriteLine($"✅ [Q51] Đã chấm xong: {userExamWritingAnswerId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [Q51] Lỗi chấm bài {userExamWritingAnswerId}: {ex.Message}");
                throw; // Re-throw để Hangfire retry
            }
        }

        public async Task GradeQuestion52Async(string userExamWritingAnswerId)
        {
            try
            {
                Console.WriteLine($"🔄 [Q52] Bắt đầu chấm: {userExamWritingAnswerId}");

                var request = new Question52RequestDto
                {
                    UserExamWritingAnswerId = userExamWritingAnswerId
                };

                await _q52Pipeline.SolveAsync(request, CancellationToken.None);

                Console.WriteLine($"✅ [Q52] Đã chấm xong: {userExamWritingAnswerId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [Q52] Lỗi chấm bài {userExamWritingAnswerId}: {ex.Message}");
                throw;
            }
        }

        public async Task GradeQuestion53Async(string userExamWritingAnswerId)
        {
            try
            {
                Console.WriteLine($"🔄 [Q53] Bắt đầu chấm: {userExamWritingAnswerId}");

                var request = new Question53RequestDto
                {
                    UserExamWritingAnswerId = userExamWritingAnswerId
                };

                await _q53Pipeline.SolveAsync(request, CancellationToken.None);

                Console.WriteLine($"✅ [Q53] Đã chấm xong: {userExamWritingAnswerId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [Q53] Lỗi chấm bài {userExamWritingAnswerId}: {ex.Message}");
                throw;
            }
        }

        public async Task GradeQuestion54Async(string userExamWritingAnswerId)
        {
            try
            {
                Console.WriteLine($"🔄 [Q54] Bắt đầu chấm: {userExamWritingAnswerId}");

                var request = new Question54RequestDto
                {
                    UserExamWritingAnswerId = userExamWritingAnswerId
                };

                await _q54Pipeline.SolveAsync(request, CancellationToken.None);

                Console.WriteLine($"✅ [Q54] Đã chấm xong: {userExamWritingAnswerId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [Q54] Lỗi chấm bài {userExamWritingAnswerId}: {ex.Message}");
                throw;
            }
        }
    }
}