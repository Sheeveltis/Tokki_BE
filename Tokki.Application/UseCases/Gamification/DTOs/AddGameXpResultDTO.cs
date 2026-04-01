namespace Tokki.Application.UseCases.Gamification.Commands.AddGameXp
{
    public class AddGameXpResultDto
    {
        public long TotalXP { get; set; }
        public long XpAdded { get; set; }
        public bool IsLevelUp { get; set; }
        public int NewLevel { get; set; }
    }
}