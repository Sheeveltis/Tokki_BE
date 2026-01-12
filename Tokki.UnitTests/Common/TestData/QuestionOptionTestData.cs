using System;
using System.Collections.Generic;
using Tokki.Application.UseCases.QuestionBanks.Commands.QuestionOptions.Create;
using Tokki.Application.UseCases.QuestionBanks.Commands.QuestionOptions.Delete;
using Tokki.Application.UseCases.QuestionBanks.Commands.QuestionOptions.Update;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.UnitTests.Common.TestData
{
    public static class QuestionOptionTestData
    {
        public const string DefaultQuestionBankId = "qb-01";
        public const string DefaultQuestionTypeId = "qt-01";
        public const string DefaultOptionId = "opt-01";

        // ----------------------
        // Commands
        // ----------------------
        public static CreateQuestionOptionCommand BuildCreateCommand(
            string? questionBankId = null,
            string keyOption = "1",
            string? content = "A",
            string? imageUrl = null,
            bool isCorrect = false)
        {
            return new CreateQuestionOptionCommand
            {
                QuestionBankId = questionBankId ?? DefaultQuestionBankId,
                KeyOption = keyOption,
                Content = content,
                ImageUrl = imageUrl,
                IsCorrect = isCorrect
            };
        }

        public static DeleteQuestionOptionCommand BuildDeleteCommand(
            string? questionBankId = null,
            string? optionId = null)
        {
            return new DeleteQuestionOptionCommand
            {
                QuestionBankId = questionBankId ?? DefaultQuestionBankId,
                OptionId = optionId ?? DefaultOptionId
            };
        }

        public static UpdateQuestionOptionCommand BuildUpdateCommand(
            string? questionBankId = null,
            string? optionId = null,
            string? keyOption = null,
            string? content = null,
            string? imageUrl = null,
            bool? isCorrect = null)
        {
            return new UpdateQuestionOptionCommand
            {
                QuestionBankId = questionBankId ?? DefaultQuestionBankId,
                OptionId = optionId ?? DefaultOptionId,
                KeyOption = keyOption,
                Content = content,
                ImageUrl = imageUrl,
                IsCorrect = isCorrect
            };
        }

        // ----------------------
        // Entities
        // ----------------------
        public static QuestionType BuildQuestionType(
            string? id = null,
            QuestionSkill skill = QuestionSkill.Reading,
            bool isActive = true,
            string name = "Type")
        {
            return new QuestionType
            {
                QuestionTypeId = id ?? DefaultQuestionTypeId,
                Skill = skill,
                IsActive = isActive,
                Name = name
            };
        }

        public static QuestionOption BuildOption(
            string? optionId = null,
            string? questionBankId = null,
            string keyOption = "1",
            string? content = "A",
            string? imageUrl = null,
            bool isCorrect = false)
        {
            return new QuestionOption
            {
                OptionId = optionId ?? DefaultOptionId,
                QuestionBankId = questionBankId ?? DefaultQuestionBankId,
                KeyOption = keyOption,
                Content = content,
                ImageUrl = imageUrl,
                IsCorrect = isCorrect
            };
        }

        public static QuestionBank BuildQuestionBank(
            string? questionBankId = null,
            QuestionBankStatus status = QuestionBankStatus.Draft,
            string? questionTypeId = null,
            List<QuestionOption>? options = null)
        {
            return new QuestionBank
            {
                QuestionBankId = questionBankId ?? DefaultQuestionBankId,
                Status = status,
                QuestionTypeId = questionTypeId ?? DefaultQuestionTypeId,
                QuestionOptions = options ?? new List<QuestionOption>(),
                CreatedAt = DateTime.UtcNow
            };
        }

        public static List<QuestionOption> BuildOptions(int count, bool markFirstCorrect = false)
        {
            var list = new List<QuestionOption>();
            for (int i = 1; i <= count; i++)
            {
                list.Add(BuildOption(
                    optionId: $"opt-{i:00}",
                    questionBankId: DefaultQuestionBankId,
                    keyOption: i.ToString(),
                    content: $"Option {i}",
                    isCorrect: markFirstCorrect && i == 1
                ));
            }
            return list;
        }
    }
}
