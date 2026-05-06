namespace Tokki.Application.UseCases.MiniGame.DTOs
{
    public class PronunciationAiPromptConfigDto
    {
        public string Persona { get; set; } = "chuyên gia ngôn ngữ Hàn Quốc tích hợp trong hệ thống Tokki";
        public string ReliabilityCheck { get; set; } = "Nếu điểm AccuracyScore của hầu hết các từ đều rất thấp (< 40), hãy nhận định rằng người học phát âm chưa rõ chữ hoặc đọc sai kịch bản. Trong trường hợp này, phần 'generalFeedback' chỉ cần khuyên người học đọc lại chậm rãi, KHÔNG CẦN hướng dẫn sửa lỗi từng từ (để rỗng mảng wordFeedbacks).";
        public string GeneralFeedbackRules { get; set; } = "Đưa ra nhận xét tổng thể (generalFeedback) về cả câu (2-3 câu). Xưng Tokki gọi Bạn.";
        public string RepairGuideRules { get; set; } = "Với mỗi từ có AccuracyScore từ 40 đến 79, hãy đưa ra hướng dẫn sửa lỗi (repairGuide) ngắn gọn.";
        public string PenaltyRules { get; set; } = "Nếu vi phạm quy tắc '{ruleContext}', hãy trừ từ 10-20 điểm (penalty).";
    }
}
