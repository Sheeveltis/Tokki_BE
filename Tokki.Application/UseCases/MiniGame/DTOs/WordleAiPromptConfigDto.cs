using System.Collections.Generic;

namespace Tokki.Application.UseCases.MiniGame.DTOs
{
    public class WordleAiPromptConfigDto
    {
        public string Persona { get; set; } = "một giám khảo chấm thi TOPIK (Test of Proficiency in Korean) nổi tiếng khắt khe";
        public ScoringCriteria Meaning { get; set; } = new ScoringCriteria { MaxScore = 40, Description = "- 40đ: Sử dụng đúng nghĩa, ngữ cảnh tự nhiên.\n- 10-20đ: Dùng từ bị gượng ép hoặc sai sắc thái nghĩa.\n- 0đ: Sai nghĩa hoàn toàn hoặc không có từ khóa." };
        public ScoringCriteria Grammar { get; set; } = new ScoringCriteria { MaxScore = 30, Description = "- [Sơ cấp 1]: (Chỉ dùng -이/가, -은/는, -에, -아요/어요): TỐI ĐA 10đ.\n- [Sơ cấp 2 - Trung cấp 1]: (Dùng -(으)니까, -아/어서, -지만, -고...): TỐI ĐA 20đ.\n- [Trung cấp 2 - Cao cấp]: (Dùng định ngữ phức tạp, cấu trúc -(으)ㄹ 뿐만 아니라, -기 nhờ..., kính ngữ -시...): TỐI ĐA 30đ." };
        public ScoringCriteria Naturalness { get; set; } = new ScoringCriteria { MaxScore = 30, Description = "- Câu dưới 8 từ: TỐI ĐA 10đ.\n- Câu có trạng từ (매우, nhất là...), định ngữ, bối cảnh rõ ràng: 20-30đ." };
        public List<WordleExampleDto> Examples { get; set; } = new List<WordleExampleDto>();
        public int MaxScoreForSimpleSentence { get; set; } = 60;
    }

    public class ScoringCriteria
    {
        public int MaxScore { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class WordleExampleDto
    {
        public string Word { get; set; } = string.Empty;
        public string Sentence { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public string ScoreRange { get; set; } = string.Empty;
        public string Feedback { get; set; } = string.Empty;
    }
}
