using System;
using System.Collections.Generic;
using Tokki.Application.UseCases.QuestionBanks.Commands.ActivateQuestionBanks;
using Tokki.Application.UseCases.QuestionBanks.Commands.CreateQuestionBank;
using Tokki.Application.UseCases.QuestionBanks.Commands.DeleteQuestionBank;
using Tokki.Application.UseCases.QuestionBanks.Commands.UpdateQuestionBank;
using Tokki.Application.UseCases.QuestionBanks.DTOs;
using Tokki.Application.UseCases.QuestionBanks.Queries.GetByQuestionTypeId;
using Tokki.Application.UseCases.QuestionBanks.Queries.GetQuestionBankById;
using Tokki.Application.UseCases.QuestionBanks.Queries.GetQuestionBanks;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.UnitTests.Common.TestData
{
    public static class QuestionBankTestData
    {
        public const string DefaultQuestionBankId = "qb-01";
        public const string DefaultQuestionTypeId = "qt-01";
        public const string DefaultPassageId = "p-01";

        // ----------------------
        // Commands / Queries
        // ----------------------
        public static ActivateQuestionBanksCommand GetActivateCommand(params string[] ids)
        {
            return new ActivateQuestionBanksCommand
            {
                QuestionBankIds = new List<string>(ids)
            };
        }

        public static CreateQuestionBankCommand GetCreateCommand(
            string? questionTypeId = null,
            string? passageId = null,
            string content = "content",
            List<CreateQuestionOptionDto>? options = null,
            string? mediaUrl = null,
            string? explanation = null)
        {
            return new CreateQuestionBankCommand
            {
                QuestionTypeId = questionTypeId ?? DefaultQuestionTypeId,
                PassageId = passageId,
                Content = content,
                MediaUrl = mediaUrl,
                Explanation = explanation,
                Options = options ?? new List<CreateQuestionOptionDto>()
            };
        }

        public static DeleteQuestionBankCommand GetDeleteCommand(string? id = null)
        {
            return new DeleteQuestionBankCommand
            {
                QuestionBankId = id ?? DefaultQuestionBankId
            };
        }

        public static UpdateQuestionBankCommand GetUpdateCommand(
            string? id = null,
            string? passageId = null,
            string? questionTypeId = null,
            string content = "new content",
            string? mediaUrl = null,
            string? explanation = null)
        {
            return new UpdateQuestionBankCommand
            {
                QuestionBankId = id ?? DefaultQuestionBankId,
                PassageId = passageId,
                QuestionTypeId = questionTypeId,
                Content = content,
                MediaUrl = mediaUrl,
                Explanation = explanation
            };
        }

        public static GetQuestionBanksByQuestionTypeIdQuery GetByQuestionTypeIdQuery(string questionTypeId, bool? isActive = true)
        {
            return new GetQuestionBanksByQuestionTypeIdQuery
            {
                QuestionTypeId = questionTypeId,
                IsActive = isActive
            };
        }

        public static GetQuestionBankByIdQuery GetByIdQuery(string id)
        {
            return new GetQuestionBankByIdQuery
            {
                QuestionBankId = id
            };
        }

        public static GetQuestionBanksQuery GetPagedQuery(
            int pageNumber = 1,
            int pageSize = 10,
            string? searchTerm = null,
            string? questionTypeId = null,
            string? passageId = null,
            QuestionBankStatus? status = null)
        {
            return new GetQuestionBanksQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                QuestionTypeId = questionTypeId,
                PassageId = passageId,
                Status = status
            };
        }

        // ----------------------
        // Domain entities builders
        // ----------------------
        public static QuestionType BuildQuestionType(
            string? id = null,
            QuestionSkill skill = QuestionSkill.Reading,
            bool isActive = true,
            string name = "Type Name")
        {
            return new QuestionType
            {
                QuestionTypeId = id ?? DefaultQuestionTypeId,
                Skill = skill,
                IsActive = isActive,
                Name = name
            };
        }

        public static Passage BuildPassage(
            string? id = null,
            PassageMediaType mediaType = PassageMediaType.Text,
            string title = "Passage Title")
        {
            return new Passage
            {
                PassageId = id ?? DefaultPassageId,
                Title = title,
                MediaType = mediaType,
                Status = PassageStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static QuestionOption BuildOption(
            string optionId,
            string qbId,
            string key,
            string content,
            bool isCorrect)
        {
            return new QuestionOption
            {
                OptionId = optionId,
                QuestionBankId = qbId,
                KeyOption = key,
                Content = content,
                ImageUrl = null,
                IsCorrect = isCorrect
            };
        }

        public static QuestionBank BuildQuestionBank(
            string? id = null,
            QuestionBankStatus status = QuestionBankStatus.Draft,
            string? questionTypeId = null,
            string? passageId = null,
            Passage? passage = null,
            QuestionType? questionType = null,
            string content = "content",
            List<QuestionOption>? options = null)
        {
            return new QuestionBank
            {
                QuestionBankId = id ?? DefaultQuestionBankId,
                Status = status,
                QuestionTypeId = questionTypeId ?? DefaultQuestionTypeId,
                PassageId = passageId,
                Passage = passage,
                QuestionType = questionType,
                Content = content,
                MediaUrl = null,
                Explanation = null,
                CreatedAt = DateTime.UtcNow,
                QuestionOptions = options ?? new List<QuestionOption>()
            };
        }

        public static List<CreateQuestionOptionDto> BuildCreateOptionsSingleCorrect()
        {
            return new List<CreateQuestionOptionDto>
            {
                new CreateQuestionOptionDto { KeyOption = "1", Content = "A", ImageUrl = null, IsCorrect = true },
                new CreateQuestionOptionDto { KeyOption = "2", Content = "B", ImageUrl = null, IsCorrect = false }
            };
        }
    }
}
