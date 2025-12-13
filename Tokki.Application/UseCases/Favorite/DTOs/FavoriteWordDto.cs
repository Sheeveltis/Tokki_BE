using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Favorite.DTOs
{
    public class FavoriteWordDto
    {
        public string FavoriteWordId { get; set; } = string.Empty;
        public string WordId { get; set; } = string.Empty;
        public string WordText { get; set; } = string.Empty;
        public string? Pronunciation { get; set; }
        public string? AudioURL { get; set; }
        public string? MeaningId { get; set; }
        public string? Definition { get; set; }
        public string? Note { get; set; }
        public DateTime CreateDate { get; set; }
        public UserFavoriteWordStatus Status { get; set; }
    }
}
