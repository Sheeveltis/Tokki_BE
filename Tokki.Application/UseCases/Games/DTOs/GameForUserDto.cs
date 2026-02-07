using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Games.DTOs
{
    public class GameForUserDto
    {
        public string GameId { get; set; } = string.Empty;
        public string GameName { get; set; } = string.Empty;
        public GameType GameType { get; set; }
        public bool IsVip { get; set; }
        public string ImgUrl { get; set; }

    }
}
