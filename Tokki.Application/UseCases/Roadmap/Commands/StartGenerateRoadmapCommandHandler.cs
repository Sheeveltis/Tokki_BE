using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Roadmap.Commands.GenerateRoadmap;
using Tokki.Application.UseCases.Roadmap.DTOs;

namespace Tokki.Application.UseCases.Roadmap.Commands.StartGenerateRoadmap
{
    public class StartGenerateRoadmapCommandHandler
        : IRequestHandler<StartGenerateRoadmapCommand, OperationResult<string>>
    {
        private readonly IRoadmapProgressService _progressService;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<StartGenerateRoadmapCommandHandler> _logger;

        public StartGenerateRoadmapCommandHandler(
            IRoadmapProgressService progressService,
            IIdGeneratorService idGeneratorService,
            IServiceScopeFactory scopeFactory,
            ILogger<StartGenerateRoadmapCommandHandler> logger)
        {
            _progressService = progressService;
            _idGeneratorService = idGeneratorService;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public Task<OperationResult<string>> Handle(
            StartGenerateRoadmapCommand request,
            CancellationToken cancellationToken)
        {
            var jobId = _idGeneratorService.GenerateCustom(15);

            _progressService.Set(jobId, new RoadmapProgressState
            {
                JobId = jobId,
                Percent = 0,
                Step = "Đang khởi động..."
            });

            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var generateCommand = new GenerateRoadmapCommand
                {
                    UserId = request.UserId,
                    TargetAim = request.TargetAim,
                    DurationDays = request.DurationDays,
                    UserExamId = request.UserExamId,
                    JobId = jobId
                };

                try
                {
                    var result = await mediator.Send(generateCommand, CancellationToken.None);

                    if (result.IsSuccess)
                    {
                        _progressService.Set(jobId, new RoadmapProgressState
                        {
                            JobId = jobId,
                            Percent = 100,
                            Step = "Lộ trình đã sẵn sàng!",
                            IsCompleted = true,
                            RoadmapId = result.Data
                        });
                    }
                    else
                    {
                        _progressService.Set(jobId, new RoadmapProgressState
                        {
                            JobId = jobId,
                            Percent = 0,
                            Step = "Tạo lộ trình thất bại.",
                            IsError = true,
                            ErrorMessage = result.Message
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background job {JobId} gặp lỗi không xử lý được", jobId);

                    _progressService.Set(jobId, new RoadmapProgressState
                    {
                        JobId = jobId,
                        Percent = 0,
                        Step = "Lỗi hệ thống.",
                        IsError = true,
                        ErrorMessage = "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại."
                    });
                }
            });

            return Task.FromResult(
                OperationResult<string>.Success(jobId, 202, "Đang tạo lộ trình, vui lòng theo dõi tiến trình."));
        }
    }
}