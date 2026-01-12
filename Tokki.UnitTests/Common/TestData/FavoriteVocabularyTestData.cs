using Tokki.Application.UseCases.FavoriteVocabulary.Commands.AddFavoriteVocabulary;
using Tokki.Application.UseCases.FavoriteVocabulary.Commands.RemoveFavoriteVocabulary;
using Tokki.Application.UseCases.FavoriteVocabulary.Queries.GetFavoriteVocabularies;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.UnitTests.Common.TestData
{
    public static class FavoriteVocabularyTestData
    {
        // =======================
        // Commands
        // =======================

        public static AddFavoriteVocabularyCommand GetAddCommand(string? vocabularyId = null)
        {
            return new AddFavoriteVocabularyCommand
            {
                VocabularyId = vocabularyId ?? "vocab-01"
            };
        }

        public static RemoveFavoriteVocabularyCommand GetRemoveCommand(string? vocabularyId = null)
        {
            return new RemoveFavoriteVocabularyCommand
            {
                VocabularyId = vocabularyId ?? "vocab-01"
            };
        }

        // =======================
        // Backward-compatible aliases (nếu code/test cũ đang dùng)
        // =======================

        public static AddFavoriteVocabularyCommand GetValidAddCommand(string? vocabularyId = null)
            => GetAddCommand(vocabularyId);

        public static RemoveFavoriteVocabularyCommand GetValidRemoveCommand(string? vocabularyId = null)
            => GetRemoveCommand(vocabularyId);

        public static RemoveFavoriteVocabularyCommand GetValidCommand(string? vocabularyId = null)
            => GetRemoveCommand(vocabularyId);

        // =======================
        // Queries
        // =======================

        public static GetFavoriteVocabulariesQuery GetGetFavoritesQuery(
            string? topicId = null,
            int pageNumber = 1,
            int pageSize = 10,
            string? searchTerm = null)
        {
            return new GetFavoriteVocabulariesQuery
            {
                TopicId = topicId,
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = searchTerm
            };
        }
        public static Topic GetTopicWithStatus(TopicStatus status, string? topicId = null)
        {
            return new Topic
            {
                TopicId = topicId ?? "topic-01",
                Status = status
            };
        }
        public static Vocabulary GetActiveVocabulary(string? vocabularyId = null)
        {
            return new Vocabulary
            {
                VocabularyId = vocabularyId ?? "vocab-01",
                Text = "hello",
                Definition = "xin chào",
                Pronunciation = "həˈləʊ",
                ImgURL = "img",
                AudioURL = "audio",
                Status = VocabularyStatus.Active
            };
        }
    }
}
