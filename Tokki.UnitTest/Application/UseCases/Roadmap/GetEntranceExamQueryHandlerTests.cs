using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Roadmap.Queries.GetEntranceExam;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Mocks.Repositories;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Roadmap
{
    public class GetEntranceExamQueryHandlerTests
    {
        private static GetEntranceExamQueryHandler CreateHandler(
            Mock<IUserRoadmapRepository>? repo = null,
            Mock<Tokki.Application.UseCases.Roadmap.Constants.ITopikLevelConfigService>? configService = null)
        {
            var examRepo = new Mock<IExamRepository>();
            var mockConfig = configService ?? new Mock<Tokki.Application.UseCases.Roadmap.Constants.ITopikLevelConfigService>();
            
            if (configService == null)
            {
                mockConfig.Setup(x => x.GetByLevelAsync(TargetAimLevel.Topik_I_Level1, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(new Tokki.Application.UseCases.Roadmap.DTOs.TopikLevelConfigDto { ConfigKey = "ENTRANCE_EXAM_TOPIK_1", ExamGroup = "TOPIK_I", PassScore = 80, TotalScore = 200 });

                mockConfig.Setup(x => x.GetByLevelAsync(TargetAimLevel.Topik_I_Level2, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(new Tokki.Application.UseCases.Roadmap.DTOs.TopikLevelConfigDto { ConfigKey = "ENTRANCE_EXAM_TOPIK_1", ExamGroup = "TOPIK_I", PassScore = 140, TotalScore = 200 });
                          
                mockConfig.Setup(x => x.GetByLevelAsync(TargetAimLevel.Topik_II_Level3, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(new Tokki.Application.UseCases.Roadmap.DTOs.TopikLevelConfigDto { ConfigKey = "ENTRANCE_EXAM_TOPIK_2", ExamGroup = "TOPIK_II", PassScore = 120, TotalScore = 300 });

                mockConfig.Setup(x => x.GetByLevelAsync((TargetAimLevel)99, It.IsAny<CancellationToken>()))
                          .ReturnsAsync((Tokki.Application.UseCases.Roadmap.DTOs.TopikLevelConfigDto?)null);
            }

            return new GetEntranceExamQueryHandler(
                examRepo.Object,
                (repo ?? MockUserRoadmapRepository.GetMock()).Object,
                mockConfig.Object);
        }

        // GetEntranceExam_01 | A | Invalid TargetAim (not in TopikLevelConfig) → 400
        [Fact]
        public async Task Handle_InvalidTargetAim_ShouldReturn400()
        {
            // TargetAimLevel is an enum so we cast invalid value
            var handler = CreateHandler();
            var result  = await handler.Handle(new GetEntranceExamQuery { TargetAim = (TargetAimLevel)99 }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(400);
            QACollector.LogTestCase("Roadmap - Get Entrance Exam", new TestCaseDetail { FunctionGroup = "GetEntranceExam", TestCaseID = "GetEntranceExam_01", Description = "Invalid TargetAim (not in Levels dict) → 400", ExpectedResult = "IsSuccess=false, 400", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "TargetAimLevel=99 (invalid)", "TryGetValue fails" } });
        }

        // GetEntranceExam_02 | A | Valid aim but no exam configured → 404
        [Fact]
        public async Task Handle_ValidAimButNoExam_ShouldReturn404()
        {
            var repo   = MockUserRoadmapRepository.GetMock(entranceExam: null);
            var result = await CreateHandler(repo).Handle(new GetEntranceExamQuery { TargetAim = TargetAimLevel.Topik_I_Level1 }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);
            QACollector.LogTestCase("Roadmap - Get Entrance Exam", new TestCaseDetail { FunctionGroup = "GetEntranceExam", TestCaseID = "GetEntranceExam_02", Description = "Valid TargetAim but exam not configured → 404", ExpectedResult = "IsSuccess=false, 404", StatusRound1 = "Passed", TestCaseType = "A", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "GetEntranceExamByConfigKeyAsync returns null", "404" } });
        }

        // GetEntranceExam_03 | N | Happy path: valid aim + exam exists → EntranceExamResult returned
        [Fact]
        public async Task Handle_ValidAimAndExamExists_ShouldReturnExamResult()
        {
            var exam   = MockUserRoadmapRepository.GetSampleEntranceExam("EXAM-ENT-01");
            var repo   = MockUserRoadmapRepository.GetMock(entranceExam: exam);
            var result = await CreateHandler(repo).Handle(new GetEntranceExamQuery { TargetAim = TargetAimLevel.Topik_I_Level1 }, CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data!.ExamId.Should().Be("EXAM-ENT-01");
            result.Data.ExamGroup.Should().Be("TOPIK_I");
            result.Data.PassScore.Should().Be(80);
            result.Data.TotalScore.Should().Be(200);
            QACollector.LogTestCase("Roadmap - Get Entrance Exam", new TestCaseDetail { FunctionGroup = "GetEntranceExam", TestCaseID = "GetEntranceExam_03", Description = "Happy path: valid aim + exam → EntranceExamResult with correct fields", ExpectedResult = "IsSuccess=true, ExamId, PassScore=80, TotalScore=200", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "TargetAim=Topik_I_Level1", "Exam found" } });
        }

        // GetEntranceExam_04 | N | Topik II Level3 → ExamGroup=TOPIK_II, PassScore=120
        [Fact]
        public async Task Handle_TopikIILevel3_ShouldReturnCorrectPassScore()
        {
            var exam   = MockUserRoadmapRepository.GetSampleEntranceExam("EXAM-ENT-02");
            var repo   = MockUserRoadmapRepository.GetMock(entranceExam: exam);
            var result = await CreateHandler(repo).Handle(new GetEntranceExamQuery { TargetAim = TargetAimLevel.Topik_II_Level3 }, CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
            result.Data!.ExamGroup.Should().Be("TOPIK_II");
            result.Data.PassScore.Should().Be(120);
            result.Data.TotalScore.Should().Be(300);
            QACollector.LogTestCase("Roadmap - Get Entrance Exam", new TestCaseDetail { FunctionGroup = "GetEntranceExam", TestCaseID = "GetEntranceExam_04", Description = "Topik II Level3 → ExamGroup=TOPIK_II, PassScore=120, TotalScore=300", ExpectedResult = "ExamGroup=TOPIK_II, PassScore=120", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "TargetAim=Topik_II_Level3", "correct config values" } });
        }

        // GetEntranceExam_05 | B | GetEntranceExamByConfigKeyAsync called with correct config key
        [Fact]
        public async Task Handle_ValidAim_GetEntranceExamCalledWithConfigKey()
        {
            var exam = MockUserRoadmapRepository.GetSampleEntranceExam();
            var repo = MockUserRoadmapRepository.GetMock(entranceExam: exam);
            await CreateHandler(repo).Handle(new GetEntranceExamQuery { TargetAim = TargetAimLevel.Topik_I_Level1 }, CancellationToken.None);
            repo.Verify(x => x.GetEntranceExamByConfigKeyAsync("ENTRANCE_EXAM_TOPIK_1", It.IsAny<CancellationToken>()), Times.Once);
            QACollector.LogTestCase("Roadmap - Get Entrance Exam", new TestCaseDetail { FunctionGroup = "GetEntranceExam", TestCaseID = "GetEntranceExam_05", Description = "GetEntranceExamByConfigKeyAsync called with config key 'ENTRANCE_EXAM_TOPIK_1'", ExpectedResult = "Times.Once with correct key", StatusRound1 = "Passed", TestCaseType = "B", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "TopikLevelConfig key used" } });
        }

        // GetEntranceExam_06 | N | Exam Title and Duration returned in result
        [Fact]
        public async Task Handle_ValidExam_TitleAndDurationMappedCorrectly()
        {
            var exam = new Tokki.Domain.Entities.Exam { ExamId = "EXAM-TDT-01", Title = "TOPIK I Practice", Duration = 100 };
            var repo = MockUserRoadmapRepository.GetMock(entranceExam: exam);
            var result = await CreateHandler(repo).Handle(new GetEntranceExamQuery { TargetAim = TargetAimLevel.Topik_I_Level2 }, CancellationToken.None);
            result.Data!.Title.Should().Be("TOPIK I Practice");
            result.Data.Duration.Should().Be(100);
            QACollector.LogTestCase("Roadmap - Get Entrance Exam", new TestCaseDetail { FunctionGroup = "GetEntranceExam", TestCaseID = "GetEntranceExam_06", Description = "Exam Title and Duration mapped to result", ExpectedResult = "Title='TOPIK I Practice', Duration=100", StatusRound1 = "Passed", TestCaseType = "N", TestDate = DateTime.Now.ToString("dd/MM/yyyy"), AppliedConditions = new List<string> { "Exam.Title, Duration verified" } });
        }
    }
}
