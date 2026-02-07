using System;
using System.Collections.Generic;
using Tokki.Application.UseCases.Passages.Commands.CreatePassage;
using Tokki.Application.UseCases.Passages.Commands.DeletePassage;
using Tokki.Application.UseCases.Passages.Commands.UpdatePassage;
using Tokki.Application.UseCases.Passages.Queries.GetPassageById;
using Tokki.Application.UseCases.Passages.Queries.GetPassages;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.UnitTests.Common.TestData
{
    public static class PassageTestData
    {
        public const string DefaultPassageId = "pass-01";
        public const string DefaultTitle = "My Passage";
        public const string DefaultContent = "This is content";
        public const string DefaultImageUrl = "https://img/p.png";
        public const string DefaultAudioUrl = "https://audio/a.mp3";

        // -----------------------
        // Commands / Queries
        // -----------------------
        public static CreatePassageCommand GetCreateCommand(
            string? title = null,
            string? content = null,
            string? imageUrl = null,
            string? audioUrl = null,
            PassageMediaType mediaType = PassageMediaType.Text)
        {
            return new CreatePassageCommand
            {
                Title = title ?? DefaultTitle,
                Content = content,
                ImageUrl = imageUrl,
                AudioUrl = audioUrl,
                MediaType = mediaType
            };
        }

        public static DeletePassageCommand GetDeleteCommand(string? passageId = null)
        {
            return new DeletePassageCommand
            {
                PassageId = passageId ?? DefaultPassageId
            };
        }

        public static UpdatePassageCommand GetUpdateCommand(
            string? passageId = null,
            string? title = null,
            string? content = null,
            string? imageUrl = null,
            string? audioUrl = null,
            PassageMediaType? mediaType = null)
        {
            return new UpdatePassageCommand
            {
                PassageId = passageId ?? DefaultPassageId,
                Title = title,
                Content = content,
                ImageUrl = imageUrl,
                AudioUrl = audioUrl,
                MediaType = mediaType
            };
        }

        public static GetPassageByIdQuery GetByIdQuery(string? passageId = null)
        {
            return new GetPassageByIdQuery
            {
                PassageId = passageId ?? DefaultPassageId
            };
        }

        public static GetPassagesQuery GetPassagesQuery(
            int pageNumber = 1,
            int pageSize = 10,
            string? searchTerm = null,
            PassageMediaType? mediaType = null,
            PassageStatus? status = null)
        {
            return new GetPassagesQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                MediaType = mediaType,
                Status = status
            };
        }

        // -----------------------
        // Entities
        // -----------------------
        public static Passage BuildPassage(
            string? passageId = null,
            string? title = null,
            PassageMediaType mediaType = PassageMediaType.Text,
            PassageStatus status = PassageStatus.Active,
            string? content = null,
            string? imageUrl = null,
            string? audioUrl = null,
            DateTime? createdAt = null)
        {
            return new Passage
            {
                PassageId = passageId ?? DefaultPassageId,
                Title = title ?? DefaultTitle,
                MediaType = mediaType,
                Status = status,
                Content = content,
                ImageUrl = imageUrl,
                AudioUrl = audioUrl,
                CreatedAt = createdAt ?? DateTime.UtcNow
            };
        }

        public static List<Passage> BuildPassageList(params Passage[] passages)
            => new List<Passage>(passages);
    }
}
