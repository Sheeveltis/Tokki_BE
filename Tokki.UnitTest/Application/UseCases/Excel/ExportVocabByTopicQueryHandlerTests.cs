using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Excel.DTOs;
using Tokki.Application.UseCases.Excel.Queries.ExportVocabByTopic;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Excel
{
    public class ExportVocabByTopicQueryHandlerTests
    {
        private ExportVocabByTopicQueryHandler CreateHandler(
            Mock<IVocabularyTopicRepository>? vocabTopicRepo = null,
            Mock<ITopicRepository>? topicRepo = null,
            Mock<IExcelService>? excelService = null)
        {
            return new ExportVocabByTopicQueryHandler(
                (vocabTopicRepo ?? new Mock<IVocabularyTopicRepository>()).Object,
                (topicRepo ?? new Mock<ITopicRepository>()).Object,
                (excelService ?? new Mock<IExcelService>()).Object);
        }

        [Fact]
        public async Task Handle_TopicNotFound_ShouldReturnFailure()
        {
            var query = new ExportVocabByTopicQuery { TopicId = "TOPIC-INVALID" };

            var mockTopicRepo = new Mock<ITopicRepository>();
            mockTopicRepo.Setup(x => x.GetTopicNameAsync(It.IsAny<string>()))
                         .ReturnsAsync(string.Empty);

            var handler = CreateHandler(topicRepo: mockTopicRepo);
            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_TopicHasNoVocab_ShouldReturnFailure()
        {
            var query = new ExportVocabByTopicQuery { TopicId = "TOPIC-001" };

            var mockTopicRepo = new Mock<ITopicRepository>();
            mockTopicRepo.Setup(x => x.GetTopicNameAsync("TOPIC-001"))
                         .ReturnsAsync("Chào hỏi cơ bản");

            var mockVocabTopicRepo = new Mock<IVocabularyTopicRepository>();
            mockVocabTopicRepo.Setup(x => x.GetVocabsByTopicIdAsync("TOPIC-001"))
                              .ReturnsAsync(new List<VocabularyExportDTO>());

            var handler = CreateHandler(
                vocabTopicRepo: mockVocabTopicRepo,
                topicRepo: mockTopicRepo);

            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_ValidTopic_ShouldReturnExcelFile()
        {
            var query = new ExportVocabByTopicQuery { TopicId = "TOPIC-001" };

            var mockTopicRepo = new Mock<ITopicRepository>();
            mockTopicRepo.Setup(x => x.GetTopicNameAsync("TOPIC-001"))
                         .ReturnsAsync("Chào hỏi cơ bản");

            var vocabs = new List<VocabularyExportDTO>
            {
                new VocabularyExportDTO
                {
                    Text = "안녕",
                    Pronunciation = "annyeong",
                    ImgURL = "img.png",
                    Definition = "Xin chào"
                }
            };

            var mockVocabTopicRepo = new Mock<IVocabularyTopicRepository>();
            mockVocabTopicRepo.Setup(x => x.GetVocabsByTopicIdAsync("TOPIC-001"))
                              .ReturnsAsync(vocabs);

            var mockExcel = new Mock<IExcelService>();
            mockExcel.Setup(x => x.ExportVocabularyToExcelAsync(
                        It.IsAny<List<VocabularyExportDTO>>(),
                        It.IsAny<string>()))
                     .ReturnsAsync(new byte[] { 0x01, 0x02 });

            var handler = CreateHandler(
                vocabTopicRepo: mockVocabTopicRepo,
                topicRepo: mockTopicRepo,
                excelService: mockExcel);

            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data.FileName.Should().Contain("Chào hỏi cơ bản");
            result.Data.FileContent.Should().NotBeEmpty();
        }
    }
}