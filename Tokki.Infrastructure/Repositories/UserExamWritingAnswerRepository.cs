// Infrastructure/Repositories/UserExamWritingAnswerRepository.cs
using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class UserExamWritingAnswerRepository : IUserExamWritingAnswerRepository
    {
        private readonly TokkiDbContext _context;

        public UserExamWritingAnswerRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(UserExamWritingAnswer answer, CancellationToken ct = default)
        {
            await _context.UserExamWritingAnswers.AddAsync(answer, ct);
        }

        public async Task<UserExamWritingAnswer?> GetByIdAsync(
            string userExamWritingAnswerId, CancellationToken ct = default)
        {
            return await _context.UserExamWritingAnswers
                .Include(w => w.Question)
                .Include(w => w.UserExam)
                .FirstOrDefaultAsync(w => w.UserExamWritingAnswerId == userExamWritingAnswerId, ct);
        }

        public async Task<UserExamWritingAnswer?> GetByExamAndOrderAsync(
            string userExamId, int orderIndex, CancellationToken ct = default)
        {
            return await _context.UserExamWritingAnswers
                .FirstOrDefaultAsync(w => w.UserExamId == userExamId
                                       && w.OrderIndex == orderIndex, ct);
        }

        public async Task<List<UserExamWritingAnswer>> GetByUserExamIdAsync(
            string userExamId, CancellationToken ct = default)
        {
            return await _context.UserExamWritingAnswers
                .Where(w => w.UserExamId == userExamId)
                .OrderBy(w => w.OrderIndex)
                .ToListAsync(ct);
        }

        public Task UpdateAsync(UserExamWritingAnswer answer)
        {
            _context.UserExamWritingAnswers.Update(answer);
            return Task.CompletedTask;
        }

        public async Task<bool> SaveChangesAsync(CancellationToken ct = default)
        {
            return await _context.SaveChangesAsync(ct) > 0;
        }
    }
}