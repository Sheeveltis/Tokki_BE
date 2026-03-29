using MediatR;
using System.Text;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Exam.DTOs;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Exam.Commands.ExportExamToPdf
{
    public class ExportExamToPdfCommandHandler : IRequestHandler<ExportExamToPdfCommand, OperationResult<ExportExamPdfResponse>>
    {
        private readonly IExamRepository _examRepository;
        private readonly IPdfService _pdfService;

        public ExportExamToPdfCommandHandler(IExamRepository examRepository, IPdfService pdfService)
        {
            _examRepository = examRepository;
            _pdfService = pdfService;
        }

        public async Task<OperationResult<ExportExamPdfResponse>> Handle(ExportExamToPdfCommand request, CancellationToken cancellationToken)
        {
            var exam = await _examRepository.GetExamWithFullDetailsAsync(request.ExamId, cancellationToken);
            if (exam == null)
            {
                return OperationResult<ExportExamPdfResponse>.Failure("Không tìm thấy đề thi.", 404);
            }

            // Increment PDF Download Count
            exam.PdfDownloadCount++;
            await _examRepository.UpdateAsync(exam);
            await _examRepository.SaveChangesAsync(cancellationToken);

            string[] possiblePaths = {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Templates", "ExamExportTemplate.html"),
                Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Templates", "ExamExportTemplate.html"),
                Path.Combine(Directory.GetCurrentDirectory(), "Tokki.WebAPI", "Resources", "Templates", "ExamExportTemplate.html")
            };

            string templatePath = possiblePaths.FirstOrDefault(File.Exists) ?? string.Empty;
            string headerPath = possiblePaths.Select(p => p.Replace("ExamExportTemplate.html", "ExamHeader.html")).FirstOrDefault(File.Exists) ?? string.Empty;

            if (string.IsNullOrEmpty(templatePath))
            {
                return OperationResult<ExportExamPdfResponse>.Failure($"Không tìm thấy tệp mẫu PDF. Vui lòng kiểm tra Resources/Templates/ExamExportTemplate.html.", 500);
            }

            string htmlTemplate = await File.ReadAllTextAsync(templatePath, cancellationToken);
            
            // Skill translations and ordering based on TemplateParts
            var orderedSkills = exam.ExamTemplate.TemplateParts
                .OrderBy(p => p.QuestionFrom)
                .Select(p => p.Skill)
                .Distinct()
                .OrderBy(s => (int)s) 
                .ToList();


            string skillKo = string.Join(" / ", orderedSkills.Select(s => s switch {
                QuestionSkill.Listening => "듣기",
                QuestionSkill.Reading => "읽기",
                QuestionSkill.Writing => "쓰기",
                _ => s.ToString()
            }));

            string skillEn = string.Join(" / ", orderedSkills.Select(s => s.ToString()));

            string topikLevelName = exam.Type switch {
                ExamType.TopikI or ExamType.EntranceTestTopikI => "TOPIK Ⅰ",
                ExamType.TopikII or ExamType.EntranceTestTopikII => "TOPIK Ⅱ",
                _ => exam.Type.ToString()
            };

            // Xây dựng HTML nội dung câu hỏi theo Skill
            string questionsHtml = BuildQuestionsHtmlBySkill(exam, request.ShowExplanation);

            // Thay thế các placeholder
            string finalHtml = htmlTemplate
                .Replace("{{ Title }}", exam.Title)
                .Replace("{{ TopikLevelName }}", topikLevelName)
                .Replace("{{ SkillKo }}", skillKo)
                .Replace("{{ SkillEn }}", skillEn)
                .Replace("{{ QuestionsHtml }}", questionsHtml);

            byte[] pdfBytes = _pdfService.GeneratePdfFromHtml(finalHtml, exam.Title, headerPath);

            // Sanitizing Title for Filename
            string safeTitle = string.Join("_", exam.Title.Split(Path.GetInvalidFileNameChars()));
            string fileName = $"[Tokki]_{safeTitle}.pdf";

            return OperationResult<ExportExamPdfResponse>.Success(new ExportExamPdfResponse
            {
                PdfData = pdfBytes,
                FileName = fileName
            });
        }

        private string BuildQuestionsHtmlBySkill(Domain.Entities.Exam exam, bool showExplanation)
        {
            var sb = new StringBuilder();
            var parts = exam.ExamTemplate.TemplateParts.OrderBy(p => p.QuestionFrom).ToList();
            

            var allQuestions = exam.ExamQuestions.OrderBy(eq => eq.QuestionNo).ToList();
            
            // Distinct skills in order of appearance
            var skills = parts.Select(p => p.Skill).Distinct().ToList();

            foreach (var skill in skills)
            {
                var skillParts = parts.Where(p => p.Skill == skill).OrderBy(p => p.QuestionFrom).ToList();
                string skillName = skill switch {
                    QuestionSkill.Listening => "듣기",
                    QuestionSkill.Reading => "읽기",
                    QuestionSkill.Writing => "쓰기",
                    _ => skill.ToString()
                };

                // Skill Separator with Page Break (via CSS)
                bool isFirstSkill = skills.IndexOf(skill) == 0;
                string headerClass = isFirstSkill ? "skill-header first-skill" : "skill-header";

                sb.AppendLine($"<div class='{headerClass}'>");
                sb.AppendLine($"<h1>{skillName}</h1>");
                sb.AppendLine("</div>");

                foreach (var part in skillParts)
                {
                    // Render Part Header
                    string range = part.QuestionFrom == part.QuestionTo 
                        ? $"[{part.QuestionFrom}]" 
                        : $"[{part.QuestionFrom}~{part.QuestionTo}]";

                    sb.AppendLine("<div class='part-section'>");
                    sb.AppendLine($"<div class='part-header-topik'>※{range} {part.PartTitle} (각 {part.Mark} 점)</div>");

                    var partQuestions = allQuestions
                        .Where(q => q.QuestionNo >= part.QuestionFrom && q.QuestionNo <= part.QuestionTo)
                        .OrderBy(q => q.QuestionNo)
                        .ToList();

                    var groupedInPart = partQuestions
                        .OrderBy(q => q.QuestionNo)
                        .GroupBy(q => q.QuestionBank.PassageId)
                        .ToList();

                    foreach (var group in groupedInPart)
                    {
                        var firstQuestion = group.First();
                        var passage = firstQuestion.QuestionBank.Passage;

                        if (passage != null)
                        {
                            sb.AppendLine("<div class='passage-box'>");
                            sb.AppendLine($"<div class='passage-content'>{passage.Content}</div>");
                            sb.AppendLine("</div>");

                            foreach (var eq in group)
                            {
                                sb.AppendLine(RenderQuestion(eq, showExplanation && skill != QuestionSkill.Writing));
                            }
                        }
                        else
                        {
                            foreach (var eq in group)
                            {
                                sb.AppendLine(RenderQuestion(eq, showExplanation && skill != QuestionSkill.Writing));
                            }
                        }
                    }
                    sb.AppendLine("</div>"); // Close part-section
                }
            }

            return sb.ToString();
        }

        private string RenderQuestion(Domain.Entities.ExamQuestion eq, bool showExplanation)
        {
            var qb = eq.QuestionBank;
            var sb = new StringBuilder();
            sb.AppendLine("<div class='question-item'>");
            sb.AppendLine("<div class='question-header'>");
            sb.AppendLine($"<span class='question-no'>{eq.QuestionNo}.</span>");
            sb.AppendLine($"<span class='question-content'>{qb.Content}</span>");
            sb.AppendLine("</div>");

            if (!string.IsNullOrEmpty(qb.MediaUrl) && (qb.MediaUrl.EndsWith(".jpg") || qb.MediaUrl.EndsWith(".png") || qb.MediaUrl.EndsWith(".jpeg")))
            {
                sb.AppendLine($"<img class='media-image' src='{qb.MediaUrl}' />");
            }

            sb.AppendLine("<ul class='option-list'>");
            var options = qb.QuestionOptions.OrderBy(o => o.KeyOption).ToList();
            foreach (var opt in options)
            {
                string marker = opt.KeyOption switch 
                { 
                    "1" => "①", 
                    "2" => "②", 
                    "3" => "③", 
                    "4" => "④", 
                    _   => $"{opt.KeyOption}." 
                };

                sb.AppendLine("<li class='option-item'>");
                sb.AppendLine($"<span class='option-marker'>{marker}</span>");
                
                if (!string.IsNullOrEmpty(opt.ImageUrl))
                {
                    sb.AppendLine($"<img class='option-image' src='{opt.ImageUrl}' />");
                }
                
                if (!string.IsNullOrEmpty(opt.Content))
                {
                    sb.AppendLine($"<span class='option-text'>{opt.Content}</span>");
                }
                
                sb.AppendLine("</li>");
            }
            sb.AppendLine("</ul>");

            if (showExplanation && !string.IsNullOrEmpty(qb.Explanation))
            {
                sb.AppendLine("<div class='explanation-box'>");
                sb.AppendLine("<span class='explanation-title'>Giải thích chi tiết: </span>");
                sb.AppendLine(qb.Explanation);
                sb.AppendLine("</div>");
            }

            sb.AppendLine("</div>"); // Close question-item

            return sb.ToString();
        }
    }
}
