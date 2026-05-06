using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.TopikLevelConfigs.Queries.GetTargetLevelByScore
{
    public class GetTargetLevelByScoreQueryHandler : IRequestHandler<GetTargetLevelByScoreQuery, OperationResult<TargetLevelDto>>
    {
        private readonly ITopikLevelConfigRepository _repository;

        public GetTargetLevelByScoreQueryHandler(ITopikLevelConfigRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<TargetLevelDto>> Handle(GetTargetLevelByScoreQuery request, CancellationToken cancellationToken)
        {
            var configs = await _repository.GetAllAsync();

            var matchingConfig = configs
                .Where(c => c.IsActive && c.ExamGroup == request.ExamGroup && c.PassScore <= request.Score)
                .OrderByDescending(c => c.PassScore) // Lấy mức cao nhất đạt được
                .FirstOrDefault();

            if (matchingConfig == null)
            {
                return OperationResult<TargetLevelDto>.Success(null!);
            }

            var dto = new TargetLevelDto
            {
                DisplayName = matchingConfig.DisplayName,
                ExamGroup = matchingConfig.ExamGroup,
                TargetAimLevel = matchingConfig.TargetAimLevel
            };

            return OperationResult<TargetLevelDto>.Success(dto);
        }
    }
}
