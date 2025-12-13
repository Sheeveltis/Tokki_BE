using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Topics.Commands.CreateTopic;
using Tokki.Application.UseCases.Topics.Commands.DeleteTopic;
using Tokki.Application.UseCases.Topics.Commands.UpdateTopic;
using Tokki.Application.UseCases.Topics.Queries.GetById;
using Tokki.Application.UseCases.Topics.Queries;

namespace Tokki.UnitTests.Common.Bases
{
    public class TopicTestBase
    {
        protected readonly Mock<ITopicRepository> _mockRepo;
        protected readonly Mock<IIdGeneratorService> _mockIdGen;
        protected readonly Mock<ILogger<CreateTopicCommandHandler>> _mockCreateLogger;
        protected readonly Mock<ILogger<UpdateTopicCommandHandler>> _mockUpdateLogger;
        protected readonly Mock<ILogger<DeleteTopicCommandHandler>> _mockDeleteLogger;

        protected readonly CreateTopicCommandHandler _createHandler;
        protected readonly UpdateTopicCommandHandler _updateHandler;
        protected readonly DeleteTopicCommandHandler _deleteHandler;
        protected readonly GetTopicByIdQueryHandler _getByIdHandler;
        protected readonly GetAllTopicsQueryHandler _getAllHandler;

        public TopicTestBase()
        {
            _mockRepo = new Mock<ITopicRepository>();
            _mockIdGen = new Mock<IIdGeneratorService>();
            _mockCreateLogger = new Mock<ILogger<CreateTopicCommandHandler>>();
            _mockUpdateLogger = new Mock<ILogger<UpdateTopicCommandHandler>>();
            _mockDeleteLogger = new Mock<ILogger<DeleteTopicCommandHandler>>();

            _createHandler = new CreateTopicCommandHandler(
                _mockRepo.Object,
                _mockIdGen.Object,
                _mockCreateLogger.Object
            );

            _updateHandler = new UpdateTopicCommandHandler(
                _mockRepo.Object,
                _mockUpdateLogger.Object
            );

            _deleteHandler = new DeleteTopicCommandHandler(
                _mockRepo.Object,
                _mockDeleteLogger.Object
            );

            _getByIdHandler = new GetTopicByIdQueryHandler(_mockRepo.Object);
            _getAllHandler = new GetAllTopicsQueryHandler(_mockRepo.Object);
        }
    }
}
