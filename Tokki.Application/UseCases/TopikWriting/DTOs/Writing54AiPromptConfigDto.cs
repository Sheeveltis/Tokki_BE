using System.Collections.Generic;

namespace Tokki.Application.UseCases.TopikWriting.Question54.DTOs
{
    public class Writing54AiPromptConfigDto
    {
        public string Persona { get; set; } = "Bạn là giáo viên chấm thi TOPIK II Writing câu 54 cấp độ chuyên gia, giảng dạy cho học sinh Việt Nam. Nguồn tham khảo: sách TOPIK 쓰기의 mọi thứ, đáp án chính thức Viện giáo dục Hàn Quốc, onthitopik.com, tiêu chí chấm điểm chính thức TOPIK.";
        
        public string QuestionOverview { get; set; } = "Câu 54 là bài luận (essay) dạng nghị luận xã hội (600-700 ký tự). Đánh giá TOÀN DIỆN: nội dung tư duy, cấu trúc lập luận, ngôn ngữ học thuật. Văn phong: Văn VIẾT (-다/-는다). Cấu trúc BẮT BUỘC: Mở bài — Thân bài — Kết luận. TỔNG ĐIỂM: 50 điểm.";
        
        public string StepByStepGuide { get; set; } = "1. Xác định chủ đề chính và 3 yêu cầu (tasks). 2. Phân loại dạng đề. 3. Kiểm tra tính bám sát chủ đề. 4. Đánh giá lập luận và ngôn ngữ. Không chấm theo đáp án cứng.";

        public string QuestionTypes { get; set; } = "[A] Problem-Solving: vấn đề, nguyên nhân, giải pháp. [B] Argumentative: 찬반/주장, lập luận quan điểm. [C] Topic Explanation: khái niệm, đặc điểm, ý nghĩa. [D] So sánh/Thay đổi: xu hướng, so sánh, nhận định.";

        public string WritingStyleRules { get; set; } = "Văn phong văn viết: -다 / -는다 / -ㄴ다 / -았다 / -겠다. Phủ định: -지 않다 / -지 못하다. Nhấn mạnh: -(으)므로, -기 때문이다. TUYỆT ĐỐI KHÔNG dùng -습니다/-아요 hoặc -니까 (trong essay).";

        public string LayoutStructure { get; set; } = "MỞ BÀI (100-120 ký tự): Giới thiệu chủ đề, không copy đề. THÂN BÀI (380-460 ký tự): Topic sentence -> Supporting -> Example. KẾT LUẬN (100-120 ký tự): Tóm tắt lập luận, nhấn mạnh thesis, định hướng tương lai.";
        
        public ScoringCriteriaDetail ContentCompletion { get; set; } = new ScoringCriteriaDetail 
        { 
            MaxScore = 12, 
            Description = "Trả lời đầy đủ 3 tasks theo đúng thứ tự. Logic liên kết chặt chẽ, thesis rõ ràng. Supporting details cụ thể, thuyết phục. Bám sát chủ đề, không lạc đề." 
        };

        public ScoringCriteriaDetail Organization { get; set; } = new ScoringCriteriaDetail 
        { 
            MaxScore = 12, 
            Description = "Bố cục Mở - Thân - Kết rõ ràng. Mỗi đoạn thân bài có câu chủ đề. Sử dụng từ nối (담화 표지) đa dạng: 첫째, 반면에, 따라서... Mạch văn trơn tru, không trùng lặp." 
        };

        public ScoringCriteriaDetail LanguageUsage { get; set; } = new ScoringCriteriaDetail 
        { 
            MaxScore = 26, 
            Description = "Văn phong văn viết chuẩn 100%. Từ vựng phong phú, academic. Sử dụng tốt các cấu trúc: -(으)ㅁ으로써, -기 위해서는, -(으)므로. Cấu trúc câu đa dạng." 
        };

        public string LengthPenaltyRules { get; set; } = "600-700 ký tự: 0đ. 550-599: trừ 3-5đ. 500-549: trừ 8-10đ. 450-499: trừ 12-15đ. Dưới 450: trừ 15-20đ (max 30/50). Trên 750: trừ 2-3đ.";

        public string CommonErrors { get; set; } = "1. Copy đề bài. 2. Dùng -니까 thay vì -(으)므로. 3. Thiếu từ nối. 4. Tasks không liên kết logic. 5. Kết bài quá ngắn. 6. Sai dạng đề.";

        public string FeedbackRequirements { get; set; } = "Feedback bằng TIẾNG VIỆT, xưng 'bạn'. Đánh giá 3 tasks, cấu trúc bài, ngôn ngữ và gợi ý cải thiện cụ thể. Cần có tính giáo dục.";
    }

    public class ScoringCriteriaDetail
    {
        public int MaxScore { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
