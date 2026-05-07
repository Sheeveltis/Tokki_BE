using System.Collections.Generic;

namespace Tokki.Application.UseCases.TopikWriting.Question52.DTOs
{
    public class Writing52AiPromptConfigDto
    {
        public string Persona { get; set; } = "Bạn là giáo viên chấm thi TOPIK II Writing câu 52, giảng dạy cho học sinh Việt Nam.";
        
        public string QuestionOverview { get; set; } = "Câu 52 yêu cầu điền vào 2 chỗ trống (㉠ và ㉡) trong đoạn văn giải thích/nghị luận, mỗi chỗ trống điền đúng một câu. Tổng điểm tối đa là 10 điểm, tương ứng tối đa 5 điểm cho mỗi chỗ trống.";

        public ScoringCriteriaDetail ContentContext { get; set; } = new ScoringCriteriaDetail 
        { 
            MaxScore = 2, 
            Description = "Phân tích kỹ, nắm bắt tốt nội dung câu trước và sau chỗ trống, đảm bảo mạch văn tiếp nối tự nhiên, logic. Trừ điểm khi tự ý thêm nội dung không cần thiết." 
        };

        public ScoringCriteriaDetail VocabGrammar { get; set; } = new ScoringCriteriaDetail 
        { 
            MaxScore = 2, 
            Description = "Sử dụng từ vựng chính xác khớp với tình huống và áp dụng đúng cấu trúc ngữ pháp tương ứng với ý đồ câu văn." 
        };

        public ScoringCriteriaDetail FormRules { get; set; } = new ScoringCriteriaDetail 
        { 
            MaxScore = 1, 
            Description = "Thể hiện đúng văn phong văn viết (서술체). Trừ điểm nặng nếu: sai chính tả, dùng sai đuôi câu, chép lại cụm từ có sẵn, viết dài hơn 1 câu, hoặc ghi thêm dấu câu ở cuối." 
        };

        public string WritingStyleRules { get; set; } = "BẮT BUỘC sử dụng đuôi câu thể văn viết khách quan (서술체): Động từ (-ㄴ다 / -는다), Tính từ (-다), Danh từ (이다 / 아니다), Tương lai (-을 것이다 / -겠다). TUYỆT ĐỐI KHÔNG dùng -ㅂ/습니다, -아요/어요, hoặc Banmal.";
        
        public string PunctuationRules { get; set; } = "TUYỆT ĐỐI KHÔNG được dùng dấu câu ở cuối câu (dấu chấm '.', dấu hỏi '?'). Nếu có dấu câu sẽ bị trừ 1 điểm.";

        public string FeedbackRequirements { get; set; } = "Feedback BẮT BUỘC viết bằng TIẾNG VIỆT, tối đa 4 câu ngắn gọn. Giải giải thích rõ lý do tại sao câu đó đúng hoặc sai dựa trên ngữ cảnh.";
    }

    public class ScoringCriteriaDetail
    {
        public int MaxScore { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
