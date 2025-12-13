using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Word.Commands.BulkCreateWords;
using Tokki.Application.UseCases.Word.Commands.DeleteWord;
using Tokki.Application.UseCases.Word.Commands.UpdateWord;
using Tokki.Application.UseCases.Word.DTOs;
using Tokki.Application.UseCases.Word.Queries.GetWordsByTopicQuery;
using Tokki.Application.UseCases.Word.Queries;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.UnitTests.Common.TestData
{
    #region Test Data
    public static class WordTestData
    {
        public static BulkCreateWordsCommand GetValidBulkCreateWordsCommand()
        {
            return new BulkCreateWordsCommand
            {
                TopicId = "TOPIC-123",
                Words = new List<WordCreateDto>
                {
                    new WordCreateDto
                    {
                        Text = "안녕하세요",
                        Pronunciation = "an-nyeong-ha-se-yo",
                        Meanings = new List<MeaningCreateDto>
                        {
                            new MeaningCreateDto
                            {
                                Definition = "Xin chào (trang trọng)",
                                ExampleSentence = "안녕하세요, 선생님.",
                                ImgURL = "https://example.com/img1.jpg"
                            }
                        }
                    },
                    new WordCreateDto
                    {
                        Text = "감사합니다",
                        Pronunciation = "gam-sa-ham-ni-da",
                        Meanings = new List<MeaningCreateDto>
                        {
                            new MeaningCreateDto
                            {
                                Definition = "Cảm ơn (trang trọng)",
                                ExampleSentence = "도와주셔서 감사합니다."
                            }
                        }
                    }
                }
            };
        }

        public static UpdateWordCommand GetValidUpdateWordCommand()
        {
            return new UpdateWordCommand
            {
                WordId = "WORD-123",
                Text = "안녕하세요",
                Pronunciation = "an-nyeong-ha-se-yo (updated)",
                Meanings = new List<MeaningUpdateDto>
                {
                    new MeaningUpdateDto
                    {
                        MeaningId = "MEANING-123",
                        Definition = "Xin chào (cập nhật)",
                        ExampleSentence = "안녕하세요, 만나서 반갑습니다.",
                        ImgURL = "https://example.com/updated.jpg"
                    }
                }
            };
        }

        public static DeleteWordCommand GetValidDeleteWordCommand(bool forceDelete = false)
        {
            return new DeleteWordCommand
            {
                WordId = "WORD-123",
                ForceDelete = forceDelete
            };
        }

        public static GetWordsByTopicQuery GetValidGetWordsByTopicQuery()
        {
            return new GetWordsByTopicQuery
            {
                TopicId = "TOPIC-123",
                Status = WordStatus.Active,
                SearchTerm = "",
                PageNumber = 1,
                PageSize = 10
            };
        }

        public static GetWordMeaningsQuery GetValidGetWordMeaningsQuery()
        {
            return new GetWordMeaningsQuery
            {
                WordId = "WORD-123",
                TopicId = "TOPIC-123",
                Status = MeaningStatus.Active,
                PageNumber = 1,
                PageSize = 10
            };
        }

        public static Word GetFakeWord()
        {
            return new Word
            {
                WordId = "WORD-123",
                Text = "안녕하세요",
                Pronunciation = "an-nyeong-ha-se-yo",
                AudioURL = "https://example.com/audio.mp3",
                CreateBy = "USER-TEST-123",
                CreateDate = DateTime.UtcNow,
                Status = WordStatus.Active,
                Meanings = new List<Meaning>
                {
                    new Meaning
                    {
                        MeaningId = "MEANING-123",
                        WordId = "WORD-123",
                        Definition = "Xin chào",
                        ExampleSentence = "안녕하세요, 선생님.",
                        Status = MeaningStatus.Active
                    }
                }
            };
        }

        public static Topic GetFakeTopic()
        {
            return new Topic
            {
                TopicId = "TOPIC-123",
                TopicName = "Giao tiếp hàng ngày",
                Description = "Từ vựng giao tiếp cơ bản",
                Status = TopicStatus.Active,
                CreateBy = "USER-TEST-123",
                CreateDate = DateTime.UtcNow
            };
        }

        public static List<Meaning> GetFakeMeanings()
        {
            return new List<Meaning>
            {
                new Meaning
                {
                    MeaningId = "MEANING-123",
                    WordId = "WORD-123",
                    Definition = "Xin chào (trang trọng)",
                    ExampleSentence = "안녕하세요, 선생님.",
                    Status = MeaningStatus.Active,
                    CreateBy = "USER-TEST-123",
                    CreateDate = DateTime.UtcNow
                }
            };
        }

        public static List<Word> GetFakeWordsList()
        {
            return new List<Word>
            {
                new Word
                {
                    WordId = "WORD-123",
                    Text = "안녕하세요",
                    Pronunciation = "an-nyeong-ha-se-yo",
                    Status = WordStatus.Active
                },
                new Word
                {
                    WordId = "WORD-456",
                    Text = "감사합니다",
                    Pronunciation = "gam-sa-ham-ni-da",
                    Status = WordStatus.Active
                }
            };
        }
    }
}
    #endregion