using Microsoft.EntityFrameworkCore;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Services
{
    public class KnowledgeBaseService : IKnowledgeBaseService
    {
        private readonly TokkiDbContext _context;

        public KnowledgeBaseService(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<List<KnowledgeMetadata>> GetContentForWeaknessesAsync(List<string> weaknesses, CurrentTopikLevel level)
        {
            if (weaknesses == null || !weaknesses.Any())
                return new List<KnowledgeMetadata>();

            var query = _context.KnowledgeMetadatas.AsQueryable();

            var result = new List<KnowledgeMetadata>();

            foreach (var weak in weaknesses)
            {
                var matches = await query
                    .Where(k => k.Level <= level && k.SearchTags.Contains(weak))
                    .Take(5) 
                    .ToListAsync();

                result.AddRange(matches);
            }

            return result.DistinctBy(x => x.Id).ToList();
        }

        public async Task<List<KnowledgeMetadata>> GetGeneralContentForLevelAsync(CurrentTopikLevel level)
        {
           
            var nextLevel = (CurrentTopikLevel)((int)level + 1);

            return await _context.KnowledgeMetadatas
                .Where(k => k.Level == level || k.Level == nextLevel)
                .OrderBy(r => Guid.NewGuid()) 
                .Take(20) 
                .ToListAsync();
        }
    }
}