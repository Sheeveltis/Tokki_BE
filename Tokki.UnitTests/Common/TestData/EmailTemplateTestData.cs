using System.Collections.Generic;
using Tokki.Application.UseCases.EmailTemplates.Commands.CreateEmailTemplate;
using Tokki.Application.UseCases.EmailTemplates.Commands.DeleteEmailTemplate;
using Tokki.Application.UseCases.EmailTemplates.Commands.UpdateEmailTemplate;
using Tokki.Application.UseCases.EmailTemplates.Queries;
using Tokki.Domain.Entities;

namespace Tokki.UnitTests.Common.TestData
{
    public static class EmailTemplateTestData
    {
        // Tạo một Command hợp lệ (thỏa mãn Validator)
        public static CreateEmailTemplateCommand GetValidCreateEmailTemplateCommand()
        {
            return new CreateEmailTemplateCommand
            {
                TemplateKey = "WELCOME_USER_2025", // Thỏa mãn regex: chữ, số, _, -
                Subject = "Chào mừng bạn đến với Tokki",
                Body = "<p>Xin chào, cảm ơn bạn đã đăng ký...</p>",
                Description = "Template gửi khi user đăng ký thành công"
            };
        }

        // Tạo một Entity giả lập đã tồn tại trong DB (để test lỗi trùng Key)
        public static EmailTemplate GetExistingEmailTemplate()
        {
            return new EmailTemplate
            {
                TemplateId = "template-old-1",
                TemplateKey = "WELCOME_USER_2025", // Trùng với key của Command ở trên
                Subject = "Subject Cũ",
                Body = "Body Cũ",
                Description = "Mô tả cũ"
            };
        }


        public static UpdateEmailTemplateCommand GetValidUpdateEmailTemplateCommand(string id)
        {
            return new UpdateEmailTemplateCommand
            {
                TemplateId = id,
                Subject = "Tiêu đề đã chỉnh sửa",
                Body = "<p>Nội dung đã được cập nhật mới nhất...</p>",
                Description = "Mô tả mới"
            };
        }

        public static UpdateEmailTemplateCommand GetUpdateCommandWithNonExistentId()
        {
            return new UpdateEmailTemplateCommand
            {
                TemplateId = "template-khong-ton-tai", // ID fake
                Subject = "Test Fail",
                Body = "Body Fail",
                Description = "Desc Fail"
            };
        }
        public static DeleteEmailTemplateCommand GetValidDeleteCommand(string id)
        {
            return new DeleteEmailTemplateCommand
            {
                TemplateId = id
            };
        }
        public static List<EmailTemplate> GetFakeEmailTemplateList()
        {
            return new List<EmailTemplate>
            {
                new EmailTemplate { TemplateId = "tpl-1", TemplateKey = "KEY_1", Subject = "Sub 1" },
                new EmailTemplate { TemplateId = "tpl-2", TemplateKey = "KEY_2", Subject = "Sub 2" },
                new EmailTemplate { TemplateId = "tpl-3", TemplateKey = "KEY_3", Subject = "Sub 3" }
            };
        }

        public static GetAllEmailTemplatesQuery GetGetAllQuery()
        {
            return new GetAllEmailTemplatesQuery();
        }

        public static GetEmailTemplateByIdQuery GetValidGetByIdQuery(string id)
        {
            return new GetEmailTemplateByIdQuery { TemplateId = id };
        }

    }
}