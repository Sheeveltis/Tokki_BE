using System;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Titles.Queries.GetUnlockedTitles
{
    public class MyTitleResponse
    {
        public string TitleId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ColorHex { get; set; } = "#000000";
        public string? IconUrl { get; set; }
        public TitleRequirementType RequirementType { get; set; }
        public long RequirementQuantity { get; set; }
        public DateTime EarnedAt { get; set; }
    }
}
