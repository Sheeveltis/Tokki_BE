using Tokki.Application.Common.Models; // Đảm bảo namespace chứa class Error
using Tokki.Application.Common.Models;
namespace Tokki.Application.Common.Models
{
    public static class AppErrors
    {
        // --- NHÓM 1: COMMON (Lỗi chung chung hệ thống) ---
        public static readonly Error ServerError = new("App.ServerError", "Lỗi hệ thống, vui lòng thử lại sau.");
        public static readonly Error ValidationFailed = new("App.ValidationFailed", "Dữ liệu đầu vào không hợp lệ.");

        // --- NHÓM 2: AUTHENTICATION (Đăng nhập/Đăng ký) ---
        public static readonly Error WrongPassword = new("Auth.WrongPassword", "Sai mật khẩu.");
        public static readonly Error UserNotFound = new("Auth.UserNotFound", "Tài khoản không tồn tại.");
        public static readonly Error EmailDuplicated = new("Auth.EmailDuplicated", "Email này đã được sử dụng.");
        public static readonly Error PhoneNumberDuplicated = new("Auth.PhoneNumberDuplicated", "Số điện thoại này đã được sử dụng.");
        public static readonly Error UserNotFoundById = new("User.NotFoundById", "Người dùng không tồn tại.");
        public static readonly Error UserUnauthorized = new("User.Unauthorized", "Không xác định được người dùng.");
        public static readonly Error CannotDeleteSelf = new("User.CannotDeleteSelf", "Bạn không thể xóa chính mình.");
        public static readonly Error UserInactive = new("User.Inactive", "Tài khoản người dùng đã bị vô hiệu hóa.");
        public static readonly Error DefaultPasswordUsed = new("Auth.DefaultPasswordUsed", "Bạn đang sử dụng mật khẩu mặc định.");
        public static readonly Error InvalidCredentials = new("Auth.InvalidCredentials", "Mật khẩu cũ không chính xác.");

        
        //  Lỗi khi tài khoản bị khóa vĩnh viễn (Banned)
        public static readonly Error AccountBanned = new("Auth.AccountBanned", "Tài khoản của bạn đã bị khóa vĩnh viễn.");

        //  Lỗi khi tài khoản bị tạm khóa (Locked)
        // Note: Message ở đây là mặc định, FE sẽ dựa vào Code "Auth.AccountLocked" để hiển thị thông báo chi tiết (vd: kèm thời gian).
        public static readonly Error AccountLocked = new("Auth.AccountLocked", "Tài khoản đang bị tạm khóa do đăng nhập sai nhiều lần.");

        // --- NHÓM 3: BLOG/POST ---
        public static readonly Error BlogNotFound = new("Blog.NotFound", OperationMessages.NotFound("Blog"));
        public static readonly Error CannotDeleteOthersBlog = new("Blog.UnauthorizedDelete", "Bạn không được xóa bài của người khác.");

        // --- NHÓM 4: CATEGORY ---
        public static readonly Error CategoryNotFound = new("Category.NotFound", OperationMessages.NotFound("Category"));

        //Paymets & VIP
        public static readonly Error VipPackageNotFound = new("VipPackage.NotFound", "Gói dịch vụ VIP không tồn tại.");
        public static readonly Error VipPackageInactive = new("VipPackage.Inactive", "Gói dịch vụ này hiện đang tạm ngừng kinh doanh.");
        public static readonly Error PaymentNotFound = new("Payment.NotFound", OperationMessages.NotFound("Giao dịch thanh toán"));
        public static readonly Error PaymentFailed = new("Payment.Failed", "Khởi tạo giao dịch thất bại.");
        public static readonly Error PaymentInvalidContent = new("Payment.InvalidContent", "Nội dung chuyển khoản không chứa mã đơn hàng hợp lệ.");
        public static readonly Error PaymentInsufficientAmount = new("Payment.InsufficientAmount", "Số tiền chuyển khoản không đủ để kích hoạt gói.");
        public static readonly Error PaymentAlreadyProcessed = new("Payment.AlreadyProcessed", "Giao dịch này đã được xử lý trước đó.");

        //Report
        public static readonly Error ReportNotFound = new("Report.NotFound", "Không tìm thấy báo cáo hoặc báo cáo đã bị xóa.");
        public static readonly Error ReportUnauthorized = new("Report.Unauthorized", "Bạn không có quyền thực hiện thao tác trên báo cáo này.");
        public static readonly Error ReportCannotDelete = new("Report.CannotDelete", "Không thể xóa báo cáo đang xử lý hoặc đã hoàn tất.");
        public static readonly Error ReportFetchFailed = new("Report.FetchFailed", "Đã xảy ra lỗi khi lấy danh sách báo cáo.");
        public static readonly Error ReportCreationFailed = new("Report.CreationFailed", "Không thể tạo báo cáo. Vui lòng thử lại.");

        //VIP Package
        public static readonly Error VipPackageInvalidPrice = new("VipPackage.InvalidPrice", "Giá gói VIP không được nhỏ hơn 0.");
        public static readonly Error VipPackageInvalidDuration = new("VipPackage.InvalidDuration", "Thời hạn gói phải lớn hơn 0 ngày.");
        public static readonly Error VipPackageFetchFailed = new("VipPackage.FetchFailed", "Lỗi khi lấy danh sách gói VIP.");
        public static readonly Error VipPackageCreationFailed = new("VipPackage.CreationFailed", "Không thể tạo gói VIP do lỗi hệ thống.");


        public static readonly Error ConfigNotFound = new("Config.NotFound", "Không tìm thấy cấu hình.");
        public static readonly Error ConfigKeyDuplicated = new("Config.KeyDuplicated", "Key cấu hình đã tồn tại.");
        public static readonly Error ConfigKeyInvalid = new("Config.KeyInvalid", "Key cấu hình không hợp lệ.");
        public static readonly Error ConfigValueInvalid = new("Config.ValueInvalid", "Giá trị cấu hình không hợp lệ.");

        public static readonly Error OtpInvalid = new("Otp.Invalid", "OTP không hợp lệ hoặc đã hết hạn.");
        public static readonly Error OtpCodeWrong = new("Otp.CodeWrong", "Sai mã OTP.");
        public static readonly Error OtpExpired = new("Otp.Expired", "OTP đã hết hạn.");
        public static readonly Error OtpUsed = new("Otp.Used", "OTP đã được sử dụng.");
        public static readonly Error OtpNotFound = new("Otp.NotFound", "Mã xác thực không tồn tại hoặc đã hết hạn.");
        public static readonly Error OtpRateLimitExceeded = new("Otp.RateLimitExceeded", "Vui lòng đợi trước khi gửi lại OTP.");
        public static readonly Error OtpMaxRetryExceeded = new("Otp.MaxRetryExceeded", "Bạn đã nhập sai quá số lần quy định. Mã xác thực đã bị hủy.");
        public static readonly Error OtpRevoked = new("Otp.Revoked", "Mã xác thực đã bị khóa do nhập sai quá nhiều lần.");
        public static readonly Error EmailServiceError = new("Otp.EmailServiceError", "Hệ thống gửi mail đang gặp sự cố. Vui lòng thử lại sau.");

        public static readonly Error EmailTemplateNotFound = new("EmailTemplate.NotFound", "Không tìm thấy template email.");
        public static readonly Error EmailTemplateKeyDuplicated = new("EmailTemplate.KeyDuplicated", "TemplateKey đã tồn tại.");
        public static readonly Error EmailTemplateKeyInvalid = new("EmailTemplate.KeyInvalid", "TemplateKey không hợp lệ.");
        public static readonly Error EmailTemplateInUse = new("EmailTemplate.InUse", "Template đang được sử dụng, không thể xóa.");



        public static readonly Error EmailCampaignNotFound = new("EmailCampaign.NotFound", "Chiến dịch email không tồn tại.");
        public static readonly Error EmailCampaignInvalidSchedule = new("EmailCampaign.InvalidSchedule", "Thời gian lên lịch không hợp lệ.");
        public static readonly Error EmailCampaignAlreadySent = new("EmailCampaign.AlreadySent", "Chiến dịch đã được gửi.");


        // --- NHÓM: TOPIC (Chủ đề từ vựng) ---
        public static readonly Error TopicNotFound = new("Topic.NotFound", "Chủ đề không tồn tại.");
        public static readonly Error TopicNameDuplicated = new("Topic.NameDuplicated", "Tên chủ đề đã tồn tại.");
        public static readonly Error TopicInUse = new("Topic.InUse", "Chủ đề đang được sử dụng, không thể xóa.");
        public static readonly Error TopicUnauthorized = new("Topic.Unauthorized", "Bạn không có quyền thao tác với chủ đề này.");

        // --- NHÓM: VOCABULARY (Từ vựng) ---
        public static readonly Error VocabularyNotFound = new("Vocabulary.NotFound", "Từ vựng không tồn tại.");
        public static readonly Error VocabularyDuplicated = new("Vocabulary.Duplicated", "Từ vựng đã tồn tại trong chủ đề này.");

       
        // --- NHÓM: WORD (Từ vựng) ---
        public static readonly Error WordNotFound = new("Word.NotFound", "Từ vựng không tồn tại.");
        public static readonly Error WordDuplicated = new("Word.Duplicated", "Từ vựng đã tồn tại.");
        public static readonly Error WordInUse = new("Word.InUse", "Từ vựng đang được sử dụng, không thể xóa.");

        // --- NHÓM: MEANING (Nghĩa của từ) ---
        public static readonly Error MeaningNotFound = new("Meaning.NotFound", "Nghĩa của từ không tồn tại.");

        public static readonly Error AudioGenerationFailed = new("Audio.GenerationFailed", "Không thể tạo file âm thanh.");
        public static readonly Error AudioUploadFailed = new("Audio.UploadFailed", "Không thể tải file âm thanh lên cloud.");

        public static readonly Error MeaningInvalidWord = new("Meaning.InvalidWord", "Nghĩa không thuộc về từ vựng này.");
        // --- NHÓM: FAVORITE WORD ---

        public static readonly Error FavoriteWordNotFound = new("FavoriteWord.NotFound", "Từ vựng yêu thích không tồn tại.");
        public static readonly Error FavoriteWordAlreadyExists = new("FavoriteWord.AlreadyExists", "Từ vựng đã được thêm vào danh sách yêu thích.");
        public static readonly Error FavoriteWordUnauthorized = new("FavoriteWord.Unauthorized", "Bạn không có quyền thao tác với từ vựng yêu thích này.");

        // --- NHÓM: FAVORITE TOPIC ---
        public static readonly Error FavoriteTopicNotFound = new("FavoriteTopic.NotFound", "Chủ đề yêu thích không tồn tại.");
        public static readonly Error FavoriteTopicAlreadyExists = new("FavoriteTopic.AlreadyExists", "Chủ đề đã được thêm vào danh sách yêu thích.");
        public static readonly Error FavoriteTopicUnauthorized = new("FavoriteTopic.Unauthorized", "Bạn không có quyền thao tác với chủ đề yêu thích này.");

        // ============================================
        // QUESTION TYPE ERRORS
        // ============================================
        public static readonly Error QuestionTypeNotFound = new("QuestionType.NotFound", "Loại câu hỏi không tồn tại.");
        public static readonly Error QuestionTypeCodeDuplicated = new("QuestionType.CodeDuplicated", "Mã loại câu hỏi đã tồn tại.");
        public static readonly Error QuestionTypeNameDuplicated = new("QuestionType.NameDuplicated", "Tên loại câu hỏi đã tồn tại.");
        public static readonly Error QuestionTypeInUse = new("QuestionType.InUse", "Loại câu hỏi đang được sử dụng, không thể xóa.");

        // ============================================
        // PASSAGE ERRORS
        // ============================================
        public static readonly Error PassageNotFound = new("Passage.NotFound", "Đoạn văn không tồn tại.");
        public static readonly Error PassageTitleDuplicated = new("Passage.TitleDuplicated", "Tiêu đề đoạn văn đã tồn tại.");
        public static readonly Error PassageInUse = new("Passage.InUse", "Đoạn văn đang được sử dụng trong câu hỏi, không thể xóa.");

        // ============================================
        // QUESTION BANK ERRORS
        // ============================================
        public static readonly Error QuestionBankNotFound = new("QuestionBank.NotFound", "Câu hỏi không tồn tại.");
        public static readonly Error QuestionBankInvalidOptions = new("QuestionBank.InvalidOptions", "Câu hỏi phải có từ 2 đến 4 đáp án.");
        public static readonly Error QuestionBankNoCorrectAnswer = new("QuestionBank.NoCorrectAnswer", "Câu hỏi phải có ít nhất một đáp án đúng.");
        public static readonly Error QuestionBankMultipleCorrectAnswers = new("QuestionBank.MultipleCorrectAnswers", "Câu hỏi chỉ được có một đáp án đúng.");
        public static readonly Error QuestionBankInvalidKeyOption = new("QuestionBank.InvalidKeyOption", "Đáp án phải có KeyOption từ '1' đến '4'.");
        public static readonly Error QuestionBankDuplicateKeyOption = new("QuestionBank.DuplicateKeyOption", "Không được trùng KeyOption trong các đáp án.");

        // ============================================
        // QUESTION OPTION ERRORS
        // ============================================
        public static readonly Error QuestionOptionNotFound = new("QuestionOption.NotFound", "Đáp án không tồn tại.");
        public static readonly Error QuestionOptionInvalidKeyOption = new("QuestionOption.InvalidKeyOption", "KeyOption phải là '1', '2', '3' hoặc '4'.");
        // ============================================
        // EXAM TEMPLATE ERRORS
        // ============================================
        public static readonly Error ExamTemplateNotFound = new("ExamTemplate.NotFound", "Mẫu đề thi không tồn tại.");
        public static readonly Error ExamTemplateNameDuplicated = new("ExamTemplate.NameDuplicated", "Tên mẫu đề thi đã tồn tại.");
        public static readonly Error ExamTemplateInUse = new("ExamTemplate.InUse", "Mẫu đề thi đang được sử dụng, không thể xóa.");
        public static readonly Error ExamTemplateNoParts = new("ExamTemplate.NoParts", "Mẫu đề thi phải có ít nhất một phần.");

        // ============================================
        // TEMPLATE PART ERRORS
        // ============================================
        public static readonly Error TemplatePartNotFound = new("TemplatePart.NotFound", "Phần thi không tồn tại.");
        public static readonly Error TemplatePartInvalidRange = new("TemplatePart.InvalidRange", "QuestionFrom phải nhỏ hơn hoặc bằng QuestionTo.");
        public static readonly Error TemplatePartRangeOverlap = new("TemplatePart.RangeOverlap", "Khoảng câu hỏi bị trùng với phần khác trong mẫu đề.");
        public static readonly Error TemplatePartQuestionRangeInvalid = new("TemplatePart.QuestionRangeInvalid", "Khoảng câu hỏi phải lớn hơn 0.");

        // ============================================
        // EXAM ERRORS
        // ============================================
        public static readonly Error ExamNotFound = new("Exam.NotFound", "Đề thi không tồn tại.");
        public static readonly Error ExamTitleDuplicated = new("Exam.TitleDuplicated", "Tiêu đề đề thi đã tồn tại.");
        public static readonly Error ExamQuestionNotInPart = new("Exam.QuestionNotInPart", "Câu hỏi không nằm trong khoảng của bất kỳ phần nào trong mẫu đề.");
        public static readonly Error ExamQuestionSkillMismatch = new("Exam.QuestionSkillMismatch", "Kỹ năng của câu hỏi không khớp với kỹ năng của phần.");
        public static readonly Error ExamQuestionNoDuplicated = new("Exam.QuestionNoDuplicated", "Số thứ tự câu hỏi đã tồn tại trong đề thi.");
        public static readonly Error ExamQuestionBankNotFound = new("Exam.QuestionBankNotFound", "Câu hỏi trong ngân hàng không tồn tại.");

        //Comment
        public static readonly Error CommentNotFound = new("Comment.NotFound", OperationMessages.NotFound("bình luận"));
        public static readonly Error Forbidden = new("Comment.Forbidden", "Bạn không có quyền thực hiện thao tác này trên bình luận.");
    }
}

        
