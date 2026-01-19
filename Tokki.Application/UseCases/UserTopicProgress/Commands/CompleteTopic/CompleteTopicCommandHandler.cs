using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;

namespace Tokki.Application.UseCases.UserTopicProgress.Commands.CompleteTopic
{
    public class CompleteTopicCommandHandler : IRequestHandler<CompleteTopicCommand, OperationResult<bool>>
    {
        private readonly IUserTopicProgressRepository _progressRepo; 
        private readonly ITopicRepository _topicRepo; 
        private readonly IIdGeneratorService _idGen;

        public CompleteTopicCommandHandler(
            IUserTopicProgressRepository progressRepo,
            ITopicRepository topicRepo,
            IIdGeneratorService idGen)
        {
            _progressRepo = progressRepo;
            _topicRepo = topicRepo;
            _idGen = idGen;
        }

        public async Task<OperationResult<bool>> Handle(CompleteTopicCommand request, CancellationToken cancellationToken)
        {
            var topic = await _topicRepo.GetByIdAsync(request.TopicId);

            if (topic == null)
            {
                return OperationResult<bool>.Failure(
                    AppErrors.TopicNotFound,
                    404
                );
            }
            var progress = await _progressRepo.GetByUserIdAndTopicIdAsync(request.UserId, request.TopicId);
            if (progress == null)
            {
                progress = new Domain.Entities.UserTopicProgress
                {
                    UserTopicProgressId = _idGen.GenerateCustom(15),
                    UserId = request.UserId,
                    TopicId = request.TopicId,
                    IsLearned = true,
                    CompletedAt = DateTime.UtcNow,
                    LastActivityAt = DateTime.UtcNow
                };

                await _progressRepo.AddAsync(progress);
            }
            else
            {
                progress.IsLearned = true;
                progress.CompletedAt = DateTime.UtcNow;
                progress.LastActivityAt = DateTime.UtcNow;
                _progressRepo.Update(progress);
            }

            try
            {
                await _progressRepo.SaveChangesAsync(cancellationToken);
                return OperationResult<bool>.Success(true,200,OperationMessages.CreateSuccess("tiến độ học topic"));
            }
            catch (Exception ex)
            {
                return OperationResult<bool>.Failure(
                    new Error("Database.SaveError", ex.Message),
                    500
                );
            }
        }
    }
}
