using Tokki.Application.UseCases.TopikWriting.DTOs;

namespace Tokki.Application.IRepositories
{
    public interface IQuestion51Repository
    {
        Task<Question51Data?> GetQuestion51DataAsync(string questionBankId, CancellationToken ct);
    }
  
}
