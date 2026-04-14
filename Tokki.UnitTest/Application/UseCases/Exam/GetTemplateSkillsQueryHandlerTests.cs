using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Exam.Queries.GetTemplateSkills;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.UnitTest.Utilities;
using Xunit;

namespace Tokki.UnitTest.Application.UseCases.Exam
{
    public class GetTemplateSkillsQueryHandlerTests
    {
        private static GetTemplateSkillsQueryHandler CreateHandler(Mock<ITemplatePartRepository>? repo = null)
            => new((repo ?? new Mock<ITemplatePartRepository>()).Object);

        private static List<TemplatePart> GetSampleParts() => new()
        {
            new() { TemplatePartId = "P1", Skill = QuestionSkill.Listening },
            new() { TemplatePartId = "P2", Skill = QuestionSkill.Reading },
            new() { TemplatePartId = "P3", Skill = QuestionSkill.Listening } // duplicate skill
        };

        // TC-EXTS-01 | A | Template has no parts → 404
        [Fact]
        public async Task Handle_NoParts_ShouldReturn404()
        {
            var mock = new Mock<ITemplatePartRepository>();
            mock.Setup(x => x.GetByExamTemplateIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<TemplatePart>());

            var query = new GetTemplateSkillsQuery { TemplateId = "TMPL-EMPTY" };
            var result = await CreateHandler(mock).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Exam - Get Template Skills", new TestCaseDetail
            {
                FunctionGroup = "Get Template Skills", TestCaseID = "TC-EXTS-01",
                Description = "Template exists but has no parts configured",
                ExpectedResult = "Return 404", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "parts.Any() returns false" }
            });
        }

        // TC-EXTS-02 | A | Template returns null → 404
        [Fact]
        public async Task Handle_NullParts_ShouldReturn404()
        {
            var mock = new Mock<ITemplatePartRepository>();
            mock.Setup(x => x.GetByExamTemplateIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((List<TemplatePart>?)null!);

            var query = new GetTemplateSkillsQuery { TemplateId = "TMPL-NULL" };
            var result = await CreateHandler(mock).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.StatusCode.Should().Be(404);

            QACollector.LogTestCase("Exam - Get Template Skills", new TestCaseDetail
            {
                FunctionGroup = "Get Template Skills", TestCaseID = "TC-EXTS-02",
                Description = "Repository returns null instead of a list",
                ExpectedResult = "Return 404", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "parts == null → 404" }
            });
        }

        // TC-EXTS-03 | N | Valid template with parts → distinct skills returned
        [Fact]
        public async Task Handle_ValidTemplate_ShouldReturnDistinctSkills()
        {
            var mock = new Mock<ITemplatePartRepository>();
            mock.Setup(x => x.GetByExamTemplateIdAsync("TMPL-001", It.IsAny<CancellationToken>())).ReturnsAsync(GetSampleParts());

            var query = new GetTemplateSkillsQuery { TemplateId = "TMPL-001" };
            var result = await CreateHandler(mock).Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            // 3 parts but only 2 distinct skills
            result.Data.Should().HaveCount(2);
            result.Data.Should().Contain("Listening");
            result.Data.Should().Contain("Reading");

            QACollector.LogTestCase("Exam - Get Template Skills", new TestCaseDetail
            {
                FunctionGroup = "Get Template Skills", TestCaseID = "TC-EXTS-03",
                Description = "3 parts with 2 distinct skills → return 2 skills",
                ExpectedResult = "Return 200 with [Listening, Reading]", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { ".Distinct() deduplicates skills" }
            });
        }

        // TC-EXTS-04 | N | Skills returned as strings
        [Fact]
        public async Task Handle_ValidTemplate_ShouldReturnSkillsAsStrings()
        {
            var mock = new Mock<ITemplatePartRepository>();
            mock.Setup(x => x.GetByExamTemplateIdAsync("TMPL-001", It.IsAny<CancellationToken>())).ReturnsAsync(GetSampleParts());

            var result = await CreateHandler(mock).Handle(new GetTemplateSkillsQuery { TemplateId = "TMPL-001" }, CancellationToken.None);

            result.Data!.ForEach(s => s.Should().BeOneOf("Listening", "Reading", "Writing"));

            QACollector.LogTestCase("Exam - Get Template Skills", new TestCaseDetail
            {
                FunctionGroup = "Get Template Skills", TestCaseID = "TC-EXTS-04",
                Description = "Skills returned as string representation of enum values",
                ExpectedResult = "All items are valid enum string names", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Skill.ToString()" }
            });
        }

        // TC-EXTS-05 | N | Single-skill template returns 1 entry
        [Fact]
        public async Task Handle_SingleSkillTemplate_ShouldReturnOne()
        {
            var mock = new Mock<ITemplatePartRepository>();
            mock.Setup(x => x.GetByExamTemplateIdAsync("TMPL-001", It.IsAny<CancellationToken>())).ReturnsAsync(new List<TemplatePart>
            {
                new() { Skill = QuestionSkill.Writing },
                new() { Skill = QuestionSkill.Writing }
            });

            var result = await CreateHandler(mock).Handle(new GetTemplateSkillsQuery { TemplateId = "TMPL-001" }, CancellationToken.None);

            result.Data!.Should().HaveCount(1);
            result.Data.Should().Contain("Writing");

            QACollector.LogTestCase("Exam - Get Template Skills", new TestCaseDetail
            {
                FunctionGroup = "Get Template Skills", TestCaseID = "TC-EXTS-05",
                Description = "Multiple Writing parts → deduplicated to 1 entry",
                ExpectedResult = "Return ['Writing']", StatusRound1 = "Passed", TestCaseType = "N",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "Distinct() reduces 2 to 1" }
            });
        }

        // TC-EXTS-06 | A | Repository throws → exception propagates
        [Fact]
        public async Task Handle_RepositoryThrows_ShouldPropagateException()
        {
            var mock = new Mock<ITemplatePartRepository>();
            mock.Setup(x => x.GetByExamTemplateIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("DB Error"));

            await Assert.ThrowsAsync<Exception>(() => CreateHandler(mock).Handle(new GetTemplateSkillsQuery { TemplateId = "X" }, CancellationToken.None));

            QACollector.LogTestCase("Exam - Get Template Skills", new TestCaseDetail
            {
                FunctionGroup = "Get Template Skills", TestCaseID = "TC-EXTS-06",
                Description = "Repository fails; exception propagates unhandled",
                ExpectedResult = "Exception thrown", StatusRound1 = "Passed", TestCaseType = "A",
                TestDate = DateTime.Now.ToString("dd/MM/yyyy"),
                AppliedConditions = new List<string> { "ThrowsAsync" }
            });
        }
    }
}
