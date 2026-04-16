using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Roadmap.DTOs;

namespace Tokki.Application.UseCases.Roadmap.Queries.GetCurrentWeekProgress
{
    public class GetCurrentWeekProgressQueryHandler : IRequestHandler<GetCurrentWeekProgressQuery, OperationResult<CurrentWeekProgressViewModel>>
    {
        private readonly IUserRoadmapRepository _userRoadmapRepository;

        public GetCurrentWeekProgressQueryHandler(IUserRoadmapRepository userRoadmapRepository)
        {
            _userRoadmapRepository = userRoadmapRepository;
        }

        public async Task<OperationResult<CurrentWeekProgressViewModel>> Handle(GetCurrentWeekProgressQuery request, CancellationToken cancellationToken)
        {
            var roadmap = await _userRoadmapRepository.GetActiveRoadmapByUserIdAsync(request.UserId, cancellationToken);

            if (roadmap == null)
            {
                return OperationResult<CurrentWeekProgressViewModel>.Failure("Người dùng chưa có lộ trình học nào đang kích hoạt.", 404);
            }

            var now = DateTime.UtcNow;

            // 1. Ưu tiên tuần Đang học (InProgress)
            var currentWeek = roadmap.Weeks
                .OrderBy(w => w.WeekIndex)
                .FirstOrDefault(w => w.Status == Tokki.Domain.Enums.RoadmapWeekStatus.InProgress);

            // 2. Nếu không có tuần InProgress, lấy tuần gần nhất đã mở (khác Locked, ví dụ là tuần vừa hoàn thành)
            if (currentWeek == null)
            {
                currentWeek = roadmap.Weeks
                    .OrderBy(w => w.WeekIndex)
                    .LastOrDefault(w => w.Status != Tokki.Domain.Enums.RoadmapWeekStatus.Locked);
            }

            // 3. Nếu vẫn không thấy, mới tìm theo Ngày thực tế
            if (currentWeek == null)
            {
                currentWeek = roadmap.Weeks
                    .OrderBy(w => w.WeekIndex)
                    .FirstOrDefault(w => now >= w.FromDate && now <= w.ToDate);
            }

            // 4. Fallback cuối cùng: Lấy tuần 1
            if (currentWeek == null)
            {
                currentWeek = roadmap.Weeks.OrderBy(w => w.WeekIndex).FirstOrDefault();
            }

            if (currentWeek == null)
            {
                return OperationResult<CurrentWeekProgressViewModel>.Failure("Không tìm thấy tuần học hiện tại.", 404);
            }

            var result = new CurrentWeekProgressViewModel
            {
                RoadmapWeekId = currentWeek.RoadmapWeekId,
                WeekIndex = currentWeek.WeekIndex,
                FromDate = currentWeek.FromDate,
                ToDate = currentWeek.ToDate,
                TotalTasks = currentWeek.DailyTasks.Count,
                CompletedTasks = currentWeek.DailyTasks.Count(t => t.IsCompleted),
                ProgressPercent = currentWeek.DailyTasks.Count == 0 ? 0 
                    : (int)((double)currentWeek.DailyTasks.Count(t => t.IsCompleted) / currentWeek.DailyTasks.Count * 100)
            };

            return OperationResult<CurrentWeekProgressViewModel>.Success(result);
        }
    }
}
