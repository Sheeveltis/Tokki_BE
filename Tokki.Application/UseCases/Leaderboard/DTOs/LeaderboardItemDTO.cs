namespace Tokki.Application.UseCases.Leaderboard.DTOs
{
    public class LeaderboardItemDto
    {
        public int Rank { get; set; }           
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string? AvatarUrl { get; set; }
        public long TotalXP { get; set; }       
        public string? TitleName { get; set; }  
        public string? TitleColor { get; set; } 
    }
}