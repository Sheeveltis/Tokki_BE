using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.PronunciationRule.DTOs;

namespace Tokki.Infrastructure.Services
{
    public class AIPronunciationService : IAIPronunciationService
    {
        private readonly Client _client;

        public AIPronunciationService(IConfiguration configuration)
        {
            string apiKey = configuration["Gemini:ApiKey"];
            _client = new Client(apiKey: apiKey);
        }

        public async Task<string> GenerateFeedbackAsync(PronunciationAssessmentDTO assessment, string targetText, string ruleContext)
        {
            if (assessment.AccuracyScore >= 90)
            {
                return "Tuyệt vời! Bạn phát âm rất chuẩn xác và tự nhiên. Hãy giữ vững phong độ này nhé!";
            }

            var errorWords = assessment.Words
                .Where(w => w.AccuracyScore < 80)
                .Select(w => $"{w.Word} ({w.ErrorType})")
                .ToList();
            string errorDetails = errorWords.Any() ? string.Join(", ", errorWords) : "ngữ điệu chưa tự nhiên";

            string prompt = $@"
                Đóng vai giáo viên tiếng Hàn (Tokki Teacher). Nhận xét bài nói:
                - Câu mẫu: ""{targetText}""
                - Quy tắc: {ruleContext}
                - Điểm máy chấm: {assessment.AccuracyScore}/100.
                - Từ lỗi: {errorDetails}.
                
                Yêu cầu: Nhận xét ngắn gọn (tối đa 3 câu) bằng tiếng Việt. Chỉ ra cách đặt lưỡi/khẩu hình để sửa lỗi.
            ";

            try
            {
                var response = await _client.Models.GenerateContentAsync(
                    model: "gemini-3-flash-preview",
                    contents: new List<Content> // 1. Dùng List<Content> thay vì array []
                    {
                        new Content
                        {
                            Parts = new List<Part>
                            {
                                new Part { Text = prompt }
                            }
                        }
                    }
                );
                string? text = response?.Candidates?[0]?.Content?.Parts?[0]?.Text;

                return text ?? "Không có phản hồi từ AI.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gemini SDK Error: {ex.Message}");
                return "Hệ thống AI đang bận, vui lòng thử lại sau.";
            }
        }
    }
}
