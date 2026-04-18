using System.Threading;
using System.Threading.Tasks;

namespace Tokki.Application.IServices
{
    public interface IPronunciationProgressService
    {
        /// <summary>
        /// Cập nhật tiến độ luyện tập cho một Pronunciation Example.
        /// Thường được gọi sau khi user Evaluate phát âm thành công hoặc click Practice.
        /// </summary>
        Task UpdatePracticeProgressAsync(string userId, string exampleId, CancellationToken cancellationToken = default);
    }
}
