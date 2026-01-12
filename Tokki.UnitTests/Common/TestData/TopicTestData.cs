using System;
using System.Collections.Generic;
using Tokki.Application.UseCases.Topics.Commands.AddVocabulariesToTopic;
using Tokki.Application.UseCases.Topics.Commands.ApproveTopic;
using Tokki.Application.UseCases.Topics.Commands.CreateTopic;
using Tokki.Application.UseCases.Topics.Commands.CreateTopicByStaff;
using Tokki.Application.UseCases.Topics.Commands.DeleteTopic;
using Tokki.Application.UseCases.Topics.Commands.PublishTopic;
using Tokki.Application.UseCases.Topics.Commands.RejectTopic;
using Tokki.Application.UseCases.Topics.Commands.RemoveVocabulariesFromTopic;
using Tokki.Application.UseCases.Topics.Commands.SubmitTopicForApproval;
using Tokki.Application.UseCases.Topics.Commands.UpdateTopic;
using Tokki.Application.UseCases.Topics.Queries.GetById;
using Tokki.Application.UseCases.Topics.Queries;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.UnitTests.Common.TestData
{
    public static class TopicTestData
    {
        public const string DefaultTopicId = "topic-01";
        public const string DefaultTopicName = "Topic A";
        public const string DefaultUserId = "user-test-01";

        public const string Vocab01 = "vocab-01";
        public const string Vocab02 = "vocab-02";

        // ---------- Commands ----------
        public static AddVocabulariesToTopicCommand BuildAddVocabsCommand(
            string? topicId = null,
            List<string>? vocabIds = null)
        {
            return new AddVocabulariesToTopicCommand
            {
                TopicId = topicId ?? DefaultTopicId,
                VocabularyIds = vocabIds ?? new List<string> { Vocab01, Vocab02 }
            };
        }

        public static RemoveVocabulariesFromTopicCommand BuildRemoveVocabsCommand(
            string? topicId = null,
            List<string>? vocabIds = null)
        {
            return new RemoveVocabulariesFromTopicCommand
            {
                TopicId = topicId ?? DefaultTopicId,
                VocabularyIds = vocabIds ?? new List<string> { Vocab01, Vocab02 }
            };
        }

        public static ApproveTopicCommand BuildApproveCommand(string? topicId = null)
            => new ApproveTopicCommand { TopicId = topicId ?? DefaultTopicId };

        public static RejectTopicCommand BuildRejectCommand(string? topicId = null, string? reason = "not ok")
            => new RejectTopicCommand { TopicId = topicId ?? DefaultTopicId, RejectReason = reason ?? "not ok" };

        public static PublishTopicCommand BuildPublishCommand(string? topicId = null)
            => new PublishTopicCommand { TopicId = topicId ?? DefaultTopicId };

        public static SubmitTopicForApprovalCommand BuildSubmitCommand(string? topicId = null)
            => new SubmitTopicForApprovalCommand { TopicId = topicId ?? DefaultTopicId };

        public static DeleteTopicCommand BuildDeleteCommand(string? topicId = null)
            => new DeleteTopicCommand { TopicId = topicId ?? DefaultTopicId };

        public static CreateTopicCommand BuildCreateTopicCommand(
            string? name = null,
            TopicLevel level = TopicLevel.Level1,
            string? description = "desc",
            string? imgUrl = "img")
        {
            return new CreateTopicCommand
            {
                TopicName = name ?? DefaultTopicName,
                Level = level,
                Description = description,
                ImgUrl = imgUrl
            };
        }

        public static CreateTopicByStaffCommand BuildCreateTopicByStaffCommand(
            string? name = null,
            TopicLevel level = TopicLevel.Level1,
            string? description = "desc",
            string? imgUrl = "img")
        {
            return new CreateTopicByStaffCommand
            {
                TopicName = name ?? DefaultTopicName,
                Level = level,
                Description = description,
                ImgUrl = imgUrl
            };
        }

        public static UpdateTopicCommand BuildUpdateTopicCommand(
            string? topicId = null,
            string? updatedBy = DefaultUserId,
            string? topicName = "Topic New",
            string? description = "desc new",
            TopicLevel? level = TopicLevel.Level3,
            TopicStatus? status = null,
            string? imgUrl = "img-new")
        {
            return new UpdateTopicCommand
            {
                TopicId = topicId ?? DefaultTopicId,
                UpdatedBy = updatedBy ?? DefaultUserId,
                TopicName = topicName,
                Description = description,
                Level = level,
                Status = status,
                ImgUrl = imgUrl
            };
        }

        public static GetTopicDetailByIdQuery BuildGetDetailQuery(string? topicId = null)
            => new GetTopicDetailByIdQuery { TopicId = topicId ?? DefaultTopicId };

        public static GetAllTopicsQuery BuildGetAllTopicsQuery(int page = 1, int size = 10, string? search = null, TopicStatus? status = null, TopicLevel? level = null)
            => new GetAllTopicsQuery { PageNumber = page, PageSize = size, SearchTerm = search, Status = status, Level = level };

        public static GetAllTopicsForUserQuery BuildGetAllForUserQuery(int page = 1, int size = 10, string? search = null, TopicLevel? level = null)
            => new GetAllTopicsForUserQuery { PageNumber = page, PageSize = size, SearchTerm = search, Level = level };

        // ---------- Entities ----------
        public static Topic BuildTopic(
            string? topicId = null,
            string? topicName = null,
            TopicStatus status = TopicStatus.Draft,
            TopicLevel level = TopicLevel.Level1,
            string? createBy = DefaultUserId)
        {
            return new Topic
            {
                TopicId = topicId ?? DefaultTopicId,
                TopicName = topicName ?? DefaultTopicName,
                Description = "desc",
                ImgUrl = "img",
                Level = level,
                Status = status,
                CreateBy = createBy,
                CreateDate = DateTime.UtcNow.AddHours(7)
            };
        }

        public static Vocabulary BuildVocabulary(string id, VocabularyStatus status = VocabularyStatus.Active)
        {
            return new Vocabulary
            {
                VocabularyId = id,
                Text = $"text-{id}",
                Pronunciation = "pro",
                Definition = "def",
                ImgURL = "img",
                Status = status
            };
        }

        public static VocabularyTopic BuildVocabularyTopic(
            string topicId,
            Vocabulary vocab,
            VocabularyTopicStatus status = VocabularyTopicStatus.Active)
        {
            return new VocabularyTopic
            {
                TopicId = topicId,
                VocabularyId = vocab.VocabularyId,
                Status = status,
                Vocabulary = vocab
            };
        }

        public static Account BuildAccount(string userId, string email = "a@b.com", string fullName = "User A")
        {
            return new Account
            {
                UserId = userId,
                Email = email,
                FullName = fullName
            };
        }
    }
}
