using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.IServices
{
    public interface IKnowledgeBaseService
    {
        Task<List<KnowledgeMetadata>> GetContentForWeaknessesAsync(List<string> weaknesses, CurrentTopikLevel level);

        Task<List<KnowledgeMetadata>> GetGeneralContentForLevelAsync(CurrentTopikLevel level);
    }
}