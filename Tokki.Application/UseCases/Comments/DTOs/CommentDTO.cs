namespace Tokki.Application.UseCases.Comments.DTOs
{
    public class CommentDTO
    {
        public string Id { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsEdited { get; set; }

        public string UserId { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorAvatar { get; set; } = string.Empty;

        public string? Name { get; set; }
        public string? ColorHex { get; set; }
        public string? IconUrl { get; set; }

        public string? ParentId { get; set; }
        public List<CommentDTO> Replies { get; set; } = new List<CommentDTO>();
    }
}