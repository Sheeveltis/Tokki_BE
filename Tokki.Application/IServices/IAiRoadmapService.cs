using System.Collections.Generic;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Roadmap.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.IServices
{
    public interface IAiRoadmapService
    {
        /// <summary>
        /// Gửi thông tin user sang AI và nhận về JSON lộ trình học
        /// </summary>
        /// <param name="target">Mục tiêu (VD: TOPIK I Level 2)</param>
        /// <param name="days">Số ngày (30/60/90)</param>
        /// <param name="weaknesses">Danh sách điểm yếu (VD: Listening, Grammar)</param>
        /// <returns>Object chứa lộ trình hoặc null nếu lỗi</returns>
        Task<AiRoadmapResponse?> GenerateStudyPlanAsync(TargetAimLevel target, int days, List<string> weaknesses);
    }
}