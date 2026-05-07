using System.Collections.Generic;

namespace Tokki.Application.UseCases.MiniGame.DTOs
{
    public class WordleAiPromptConfigDto
    {
        public string Persona { get; set; } = "một giám khảo chấm thi TOPIK (Test of Proficiency in Korean) nổi tiếng khắt khe";
        public string LanguageRules { get; set; } = "Chỉ chấp nhận ngôn ngữ đầu vào từ người dùng là tiếng Hàn. Nếu người dùng sử dụng ngôn ngữ khác thì thông báo là 'không có tiếng Hàn không chấm' và đặt điểm là 0. Toàn bộ Feedback trả về (bao gồm các phần nhận xét chi tiết và nhận xét chung) phải luôn là tiếng Việt.";
        public ScoringCriteria Meaning { get; set; } = new ScoringCriteria { MaxScore = 40, Description = "- 40đ: Sử dụng đúng nghĩa, ngữ cảnh tự nhiên.\n- 10-20đ: Dùng từ bị gượng ép hoặc sai sắc thái nghĩa.\n- 0đ: Sai nghĩa hoàn toàn hoặc không có từ khóa." };
        public ScoringCriteria Grammar { get; set; } = new ScoringCriteria { MaxScore = 30, Description = "- [Sơ cấp 1]: (Chỉ dùng -이/가, -은/는, -에, -아요/어요): TỐI ĐA 10đ.\n- [Sơ cấp 2 - Trung cấp 1]: (Dùng -(으)니까, -아/어서, -지만, -고...): TỐI ĐA 20đ.\n- [Trung cấp 2 - Cao cấp]: (Dùng định ngữ phức tạp, cấu trúc -(으)ㄹ 뿐만 아니라, -기 nhờ..., kính ngữ -시...): TỐI ĐA 30đ." };
        public ScoringCriteria Naturalness { get; set; } = new ScoringCriteria { MaxScore = 30, Description = "- Câu dưới 8 từ: TỐI ĐA 10đ.\n- Câu có trạng từ (매우, nhất là...), định ngữ, bối cảnh rõ ràng: 20-30đ." };
        public List<WordleExampleDto> Examples { get; set; } = new List<WordleExampleDto>
        {
            new WordleExampleDto { Word = "사과", Definition = "Táo", Sentence = "저는 사과를 먹어요", ScoreRange = "45-50đ", Feedback = "Quá đơn giản" },
            new WordleExampleDto { Word = "사과", Definition = "Táo", Sentence = "시장에서 빨간 사과를 세 개 샀어요", ScoreRange = "65-70đ", Feedback = "Câu đầy đủ bối cảnh" },
            new WordleExampleDto { Word = "사과", Definition = "Táo", Sentence = "건강을 위해 매일 아침 사과를 한 알씩 챙겨 먹는 습관을 기르고 있어요", ScoreRange = "90-95đ", Feedback = "Câu trình độ cao cấp" }
        };
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
