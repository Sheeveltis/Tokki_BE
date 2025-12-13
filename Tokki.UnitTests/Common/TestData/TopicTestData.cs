using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Topics.Commands.CreateTopic;
using Tokki.Application.UseCases.Topics.Commands.DeleteTopic;
using Tokki.Application.UseCases.Topics.Commands.UpdateTopic;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.UnitTests.Common.TestData
{
    public static class TopicTestData
    {
        public static CreateTopicCommand GetValidCreateTopicCommand()
        {
            return new CreateTopicCommand
            {
                TopicName = "Gia đình",
                Description = "Từ vựng về gia đình trong tiếng Hàn",
                CreateBy = "ACC-Test-User"
            };
        }

        public static CreateTopicCommand GetDuplicateTopicCommand()
        {
            return new CreateTopicCommand
            {
                TopicName = "Gia đình", // Tên đã tồn tại
                CreateBy = "ACC-Test-User"
            };
        }

        public static UpdateTopicCommand GetValidUpdateTopicCommand()
        {
            return new UpdateTopicCommand
            {
                TopicId = "topic-123",
                TopicName = "Gia đình (Cập nhật)",
                Description = "Mô tả đã được cập nhật",
                UpdatedBy = "ACC-Updater",
                Status = TopicStatus.Active
            };
        }

        public static UpdateTopicCommand GetUpdateWithDuplicateNameCommand()
        {
            return new UpdateTopicCommand
            {
                TopicId = "topic-123",
                TopicName = "Động vật", // Tên đã tồn tại ở topic khác
                UpdatedBy = "ACC-Updater",
                Status = TopicStatus.Active
            };
        }

        public static DeleteTopicCommand GetValidDeleteTopicCommand()
        {
            return new DeleteTopicCommand
            {
                TopicId = "topic-123"
            };
        }

        public static Topic GetFakeTopicEntity()
        {
            return new Topic
            {
                TopicId = "topic-123",
                TopicName = "Gia đình",
                Description = "Từ vựng về gia đình",
                CreateBy = "ACC-Test-User",
                CreateDate = DateTime.UtcNow,
                Status = TopicStatus.Active
            };
        }

        public static List<Topic> GetFakeTopicList()
        {
            return new List<Topic>
            {
                new Topic
                {
                    TopicId = "topic-1",
                    TopicName = "Gia đình",
                    Description = "Từ vựng về gia đình",
                    Status = TopicStatus.Active,
                    CreateBy = "ACC-User1",
                    CreateDate = DateTime.UtcNow.AddDays(-5)
                },
                new Topic
                {
                    TopicId = "topic-2",
                    TopicName = "Động vật",
                    Description = "Từ vựng về động vật",
                    Status = TopicStatus.Active,
                    CreateBy = "ACC-User2",
                    CreateDate = DateTime.UtcNow.AddDays(-3)
                },
                new Topic
                {
                    TopicId = "topic-3",
                    TopicName = "Thức ăn",
                    Status = TopicStatus.Inactive,
                    CreateBy = "ACC-User1",
                    CreateDate = DateTime.UtcNow.AddDays(-1)
                }
            };
        }
    }

}
