using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;
using Tokki.Domain.Enums;

namespace Tokki.Infrastructure.Repositories
{
    public class UserExamRepository : IUserExamRepository
    {
        private readonly TokkiDbContext _context;

        public UserExamRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<UserExam?> GetInProgressSessionAsync(string userId, string examId, CancellationToken token)
        {
            return await _context.UserExams
                .Include(ue => ue.Exam)
                    .ThenInclude(e => e.ExamTemplate)
                        .ThenInclude(t => t.TemplateParts)
                .Include(ue => ue.UserExamAnswers)
                    .ThenInclude(ua => ua.Question)
                        .ThenInclude(q => q.QuestionOptions)
                .Include(ue => ue.UserExamWritingAnswers) 
                    .ThenInclude(wa => wa.Question)
                .FirstOrDefaultAsync(ue => ue.UserId == userId
                                        && ue.ExamId == examId
                                        && ue.Status == UserExamStatus.InProgress, token);
        }

        public async Task<Exam?> GetExamWithFullStructureAsync(string examId, CancellationToken token)
        {
            return await _context.Exams
                .Include(e => e.ExamTemplate)
                    .ThenInclude(t => t.TemplateParts)

                .Include(e => e.ExamQuestions)
                    .ThenInclude(eq => eq.QuestionBank)
                        .ThenInclude(qb => qb.QuestionOptions)

                .FirstOrDefaultAsync(e => e.ExamId == examId, token);
        }

        public async Task AddSessionAsync(UserExam session, CancellationToken token)
        {
            await _context.UserExams.AddAsync(session, token);
            await _context.SaveChangesAsync(token);
        }
        public async Task SaveChangesAsync(CancellationToken token)
        {
            await _context.SaveChangesAsync(token);
        }
        public async Task<UserExam?> GetByIdAsync(string userExamId, CancellationToken token)
        {
            return await _context.UserExams
                .Include(ue => ue.Exam)
                    .ThenInclude(e => e.ExamTemplate)
                        .ThenInclude(et => et.TemplateParts)
                .Include(ue => ue.UserExamAnswers)
                    .ThenInclude(ua => ua.Question)
                        .ThenInclude(q => q.QuestionOptions)
                .Include(ue => ue.UserExamWritingAnswers)
                    .ThenInclude(uwa => uwa.Question)
                .FirstOrDefaultAsync(ue => ue.UserExamId == userExamId, token);
        }
        public async Task<UserExam?> GetReviewByIdAsync(string userExamId, CancellationToken cancellationToken)
        {
            return await _context.UserExams
                .Include(ue => ue.Exam)
                    .ThenInclude(e => e.ExamTemplate)
                        .ThenInclude(et => et.TemplateParts)
                .Include(ue => ue.UserExamAnswers)
                    .ThenInclude(ua => ua.Question)
                        .ThenInclude(q => q.QuestionOptions)
                .Include(ue => ue.UserExamWritingAnswers)
                    .ThenInclude(uwa => uwa.Question)
                .AsNoTracking() 
                .FirstOrDefaultAsync(ue => ue.UserExamId == userExamId, cancellationToken);
        }
    }
}