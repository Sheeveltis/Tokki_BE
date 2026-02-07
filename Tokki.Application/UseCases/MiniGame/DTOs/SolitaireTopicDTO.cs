using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.MiniGame.DTOs
{
    public class SolitaireTopicDTO
    {
        public string TopicId { get; set; }
        public string TopicName { get; set; }
        public string ImgUrl { get; set; }

        public List<SolitaireVocabDTO> Vocabularies { get; set; } = new();
    }

    public class SolitaireVocabDTO
    {
        public string VocabId { get; set; }
        public string Text { get; set; }    // Tiếng Hàn
        public string ImgUrl { get; set; }  // Ảnh
        public string Definition { get; set; } // Nghĩa Tiếng Việt (để ghép cặp)
    }
}
