using MediatR;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.UserExam.Commands.SubmitUserExam;

namespace Tokki.WebAPI.BackgroundServices
{
    public class ExamDeadlineWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ExamDeadlineWorker> _logger;

        public ExamDeadlineWorker(IServiceProvider serviceProvider, ILogger<ExamDeadlineWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Exam Deadline Worker is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var repository = scope.ServiceProvider.GetRequiredService<IUserExamRepository>();
                        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                        var expiredSessions = await repository.GetExpiredSessionsAsync(stoppingToken);

                        foreach (var session in expiredSessions)
                        {
                            try
                            {
                                _logger.LogInformation($"Auto-submitting expired exam: {session.UserExamId}");

                                await mediator.Send(new SubmitUserExamCommand
                                {
                                    UserExamId = session.UserExamId,
                                    UserId = session.UserId 
                                }, stoppingToken);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Error auto-submitting exam {session.UserExamId}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in ExamDeadlineWorker while processing expired sessions.");
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}
