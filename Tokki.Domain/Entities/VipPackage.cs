namespace Tokki.Domain.Entities
{
    public class VipPackage
    {
        public string Id { get; set; } 
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int DurationDays { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false; 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}