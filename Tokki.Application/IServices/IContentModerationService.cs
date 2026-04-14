using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tokki.Application.IServices
{
    public class ModerationResult
    {
        public bool IsClean { get; set; }
        public List<string> BadWordsFound { get; set; } = new();
        public bool IsError { get; set; }
        public string? ErrorMessage { get; set; }
    }
 
    public interface IContentModerationService
    {
        Task<ModerationResult> CheckContentAsync(string content);
    }
}
