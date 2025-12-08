using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices; 
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.Reports.Commands.CreateReport
{
    public class CreateReportHandler : IRequestHandler<CreateReportCommand, OperationResult<string>>
    {
        private readonly IReportRepository _reportRepository;
        private readonly IIdGeneratorService _idGenerator;

        public CreateReportHandler(IReportRepository reportRepository, IIdGeneratorService idGenerator)
        {
            _reportRepository = reportRepository;
            _idGenerator = idGenerator;
        }

        public async Task<OperationResult<string>> Handle(CreateReportCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var report = new Report
                {
                    Id = _idGenerator.GenerateCustom(21), 
                    UserId = request.UserId,
                    Description = request.Description,
                    ImageUrl = request.ImageUrl,
                    TargetUrl = request.TargetUrl,
                    Status = 0, 
                    UserHasRead = true, 
                    CreatedAt = DateTime.UtcNow
                };

                await _reportRepository.AddAsync(report);
                return OperationResult<string>.Success(report.Id);
            }
            catch (Exception ex)
            {
                return OperationResult<string>.Failure("Lỗi tạo báo cáo: " + ex.Message);
            }
        }
    }
}