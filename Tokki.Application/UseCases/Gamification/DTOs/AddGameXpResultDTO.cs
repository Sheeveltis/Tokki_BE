namespace Tokki.Application.UseCases.Gamification.Commands.AddGameXp
{
    public class AddGameXpResultDto
    {
        public long TotalXP { get; set; }

        public long XpAdded { get; set; }

        public bool IsNewTitleUnlocked { get; set; }

        public string? NewTitleName { get; set; }

        public string? NewTitleColorHex { get; set; }
    }
}