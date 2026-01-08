using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.Common.Models
{
    public static class AppErrors
    {
        // ============================================
        // NHÓM 1: COMMON (Lỗi chung hệ thống)
        // ============================================
        public static readonly Error ServerError = new("App.ServerError", "Lỗi hệ thống, vui lòng thử lại sau.");
        public static readonly Error ValidationFailed = new("App.ValidationFailed", "Dữ liệu đầu vào không hợp lệ.");
        public static readonly Error BadRequest = new("App.BadRequest", "Yêu cầu không hợp lệ.");

        // ============================================
        // NHÓM 2: AUTHENTICATION & USER
        // ============================================
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
        public static readonly Error AccountBanned = new("Auth.AccountBanned", "Tài khoản của bạn đã bị khóa vĩnh viễn.");
        public static readonly Error AccountLocked = new("Auth.AccountLocked", "Tài khoản đang bị tạm khóa do đăng nhập sai nhiều lần.");
        public static readonly Error AccountNotFound = new("Account.NotFound", "Không tìm thấy tài khoản.");
        public static readonly Error AccountInActive = new("Account.AccountInActive", "Tài khoảng không hoạt động.");
        public static readonly Error MergeAccountRequered = new("Account.MergeAccountRequered", "Email đã được đăng ký vui lòng đồng ý tích hợp.");
        public static readonly Error Unauthorized =   new("Auth.Unauthorized", "Bạn cần đăng nhập để thực hiện thao tác này.");
        public static readonly Error FavoriteVocabularyAddFailed = new("FavoriteVocabulary.AddFailed", "Không thể thêm từ vựng vào danh sách yêu thích.");
        public static readonly Error FavoriteVocabularyRemoveFailed =  new("FavoriteVocabulary.RemoveFailed", "Không thể gỡ từ vựng khỏi danh sách yêu thích.");
        public static readonly Error AccountAlreadyDeleted = new("Account.AlreadyDeleted", "Tài khoản đã bị xóa trước đó.");
        public static readonly Error InvalidGoogleToken = new("Auth.InvalidGoogleToken", "Google token không hợp lệ.");
        public static readonly Error InvalidFacebookToken = new("Auth.InvalidFacebookToken", "Facebook token không hợp lệ.");
        public static readonly Error FacebookAlreadyLinked = new("Account.FacebookAlreadyLinked", "Tài khoản Facebook này đã được liên kết.");
        public static readonly Error FacebookEmailRequired = new("Auth.FacebookEmailRequired", "Vui lòng cấp quyền truy cập email từ Facebook.");
        public static readonly Error FacebookIdMismatch =    new("Facebook.IdMismatch", "Facebook token không khớp với FacebookId.");
        public static readonly Error FacebookEmailMismatch =    new("Facebook.EmailMismatch", "Email cung cấp không khớp với email từ Facebook token.");
        public static readonly Error GoogleEmailRequired = new("Google.GoogleEmailRequired", "Email là bắt buộc. Vui lòng cấp quyền email cho ứng dụng");
        public static readonly Error TargetUserIdRequired = new("Account.TargetUserIdRequired", "Thiếu userId cần thao tác.");
        public static readonly Error CannotDisableSelf  = new("Account.CannotDisableSelf", "Bạn không thể vô hiệu hóa tài khoản của chính mình.");
        public static readonly Error AccountAlreadyInactive  = new("Account.AlreadyInactive", "Tài khoản này đã bị vô hiệu hóa.");
        public static readonly Error AdminForbidden  = new("Admin.Forbidden", "Bạn không có quyền thực hiện thao tác này.");
        public static readonly Error CannotDisableSuperAdmin   = new("Account.CannotDisableSuperAdmin", "Không thể vô hiệu hóa tài khoản Super Admin.");
        public static readonly Error AccountInvalidStatusTransition  = new("Account.InvalidStatusTransition", "Không thể chuyển trạng thái tài khoản theo yêu cầu.");
        public static readonly Error AccountUpdateFailed      = new("Account.UpdateFailed", "Cập nhật tài khoản thất bại. Vui lòng thử lại.");
        public static readonly Error AccountSoftDisableFailed  = new("Account.SoftDisableFailed", "Không thể vô hiệu hóa tài khoản do lỗi hệ thống.");


        // ============================================
        // NHÓM 3: BLOG/POST
        // ============================================
        public static readonly Error BlogNotFound = new("Blog.NotFound", OperationMessages.NotFound("Blog"));
        public static readonly Error CannotDeleteOthersBlog = new("Blog.UnauthorizedDelete", "Bạn không được xóa bài của người khác.");

        // ============================================
        // NHÓM 4: CATEGORY
        // ============================================
        public static readonly Error CategoryNotFound = new("Category.NotFound", OperationMessages.NotFound("Category"));

        // ============================================
        // NHÓM 5: PAYMENT & VIP PACKAGE
        // ============================================
        public static readonly Error VipPackageNotFound = new("VipPackage.NotFound", "Gói dịch vụ VIP không tồn tại.");
        public static readonly Error VipPackageInactive = new("VipPackage.Inactive", "Gói dịch vụ này hiện đang tạm ngừng kinh doanh.");
        public static readonly Error VipPackageInvalidPrice = new("VipPackage.InvalidPrice", "Giá gói VIP không được nhỏ hơn 0.");
        public static readonly Error VipPackageInvalidDuration = new("VipPackage.InvalidDuration", "Thời hạn gói phải lớn hơn 0 ngày.");
        public static readonly Error VipPackageFetchFailed = new("VipPackage.FetchFailed", "Lỗi khi lấy danh sách gói VIP.");
        public static readonly Error VipPackageCreationFailed = new("VipPackage.CreationFailed", "Không thể tạo gói VIP do lỗi hệ thống.");

        public static readonly Error PaymentNotFound = new("Payment.NotFound", OperationMessages.NotFound("Giao dịch thanh toán"));
        public static readonly Error PaymentFailed = new("Payment.Failed", "Khởi tạo giao dịch thất bại.");
        public static readonly Error PaymentInvalidContent = new("Payment.InvalidContent", "Nội dung chuyển khoản không chứa mã đơn hàng hợp lệ.");
        public static readonly Error PaymentInsufficientAmount = new("Payment.InsufficientAmount", "Số tiền chuyển khoản không đủ để kích hoạt gói.");
        public static readonly Error PaymentAlreadyProcessed = new("Payment.AlreadyProcessed", "Giao dịch này đã được xử lý trước đó.");

        // ============================================
        // NHÓM 6: REPORT
        // ============================================
        public static readonly Error ReportNotFound = new("Report.NotFound", "Không tìm thấy báo cáo hoặc báo cáo đã bị xóa.");
        public static readonly Error ReportUnauthorized = new("Report.Unauthorized", "Bạn không có quyền thực hiện thao tác trên báo cáo này.");
        public static readonly Error ReportCannotDelete = new("Report.CannotDelete", "Không thể xóa báo cáo đang xử lý hoặc đã hoàn tất.");
        public static readonly Error ReportFetchFailed = new("Report.FetchFailed", "Đã xảy ra lỗi khi lấy danh sách báo cáo.");
        public static readonly Error ReportCreationFailed = new("Report.CreationFailed", "Không thể tạo báo cáo. Vui lòng thử lại.");

        // ============================================
        // NHÓM 7: CONFIG
        // ============================================
        public static readonly Error ConfigNotFound = new("Config.NotFound", "Không tìm thấy cấu hình.");
        public static readonly Error ConfigKeyDuplicated = new("Config.KeyDuplicated", "Key cấu hình đã tồn tại.");
        public static readonly Error ConfigKeyInvalid = new("Config.KeyInvalid", "Key cấu hình không hợp lệ.");
        public static readonly Error ConfigValueInvalid = new("Config.ValueInvalid", "Giá trị cấu hình không hợp lệ.");

        // ============================================
        // NHÓM 8: OTP
        // ============================================
        public static readonly Error OtpInvalid = new("Otp.Invalid", "OTP không hợp lệ hoặc đã hết hạn.");
        public static readonly Error OtpCodeWrong = new("Otp.CodeWrong", "Sai mã OTP.");
        public static readonly Error OtpExpired = new("Otp.Expired", "OTP đã hết hạn.");
        public static readonly Error OtpUsed = new("Otp.Used", "OTP đã được sử dụng.");
        public static readonly Error OtpNotFound = new("Otp.NotFound", "Mã xác thực không tồn tại hoặc đã hết hạn.");
        public static readonly Error OtpMaxRetryExceeded = new("Otp.MaxRetryExceeded", "Bạn đã nhập sai quá số lần quy định. Mã xác thực đã bị hủy.");
        public static readonly Error OtpRevoked = new("Otp.Revoked", "Mã xác thực đã bị khóa do nhập sai quá nhiều lần.");
        public static readonly Error EmailServiceError = new("Otp.EmailServiceError", "Hệ thống gửi mail đang gặp sự cố. Vui lòng thử lại sau.");
        public static readonly Error EmailAlreadyExists = new("Account.EmailAlreadyExists","Email này đã được đăng ký tài khoản.");
        public static readonly Error AccountUnavailable = new("Account.AccountUnavailable", "Tài khoản liên kết với email này đã bị khóa hoặc xóa. Vui lòng liên hệ quản trị viên.");
        public static Error OtpRateLimitExceeded(int remainingSeconds)
  => new(
      "Otp.RateLimitExceeded",
      $"Vui lòng đợi {remainingSeconds} giây trước khi gửi lại OTP."
  );

        // ============================================
        // NHÓM 9: EMAIL TEMPLATE
        // ============================================
        public static readonly Error EmailTemplateNotFound = new("EmailTemplate.NotFound", "Không tìm thấy template email.");
        public static readonly Error EmailTemplateKeyDuplicated = new("EmailTemplate.KeyDuplicated", "TemplateKey đã tồn tại.");
        public static readonly Error EmailTemplateKeyInvalid = new("EmailTemplate.KeyInvalid", "TemplateKey không hợp lệ.");
        public static readonly Error EmailTemplateInUse = new("EmailTemplate.InUse", "Template đang được sử dụng, không thể xóa.");

        // ============================================
        // NHÓM 10: EMAIL CAMPAIGN
        // ============================================
        public static readonly Error EmailCampaignNotFound = new("EmailCampaign.NotFound", "Chiến dịch email không tồn tại.");
        public static readonly Error EmailCampaignInvalidSchedule = new("EmailCampaign.InvalidSchedule", "Thời gian lên lịch không hợp lệ.");
        public static readonly Error EmailCampaignAlreadySent = new("EmailCampaign.AlreadySent", "Chiến dịch đã được gửi.");

        // ============================================
        // NHÓM 11: TOPIC (Chủ đề từ vựng)
        // ============================================
        public static readonly Error TopicNotFound = new("Topic.NotFound", "Chủ đề không tồn tại.");
        public static readonly Error TopicNameDuplicated = new("Topic.NameDuplicated", "Tên chủ đề đã tồn tại.");
        public static readonly Error TopicInUse = new("Topic.InUse", "Chủ đề đang được sử dụng, không thể xóa.");
        public static readonly Error TopicUnauthorized = new("Topic.Unauthorized", "Bạn không có quyền thao tác với chủ đề này.");
        public static readonly Error TopicHasVocabularies = new("Topic.HasVocabularies", "Không thể xóa chủ đề đang có từ vựng.");
        public static readonly Error TopicAlreadyDeleted = new("Topic.AlreadyDeleted", "Chủ đề đã bị xóa trước đó.");
        public static readonly Error TopicInvalidStatusTransition =  new("Topic.InvalidStatusTransition", "Không thể chuyển trạng thái chủ đề theo yêu cầu.");

        // ============================================
        // NHÓM 12: VOCABULARY (Từ vựng)
        // ============================================
        public static readonly Error VocabularyNotFound = new("Vocabulary.NotFound", "Từ vựng không tồn tại.");
        public static readonly Error VocabularyDuplicated = new("Vocabulary.Duplicated", "Từ vựng đã tồn tại trong chủ đề này.");
        public static readonly Error VocabularyAlreadyDeleted = new("Vocabulary.AlreadyDeleted", "Từ vựng đã bị xóa trước đó.");
        public static readonly Error VocabularyListEmpty = new("Vocabulary.ListEmpty", "Danh sách từ vựng rỗng hoặc không hợp lệ.");
        public static readonly Error NoValidVocabulariesFound = new("Vocabulary.NoValidFound", "Không tìm thấy từ vựng nào hợp lệ trong danh sách gửi lên.");
        public static readonly Error VocabularyDeleted = new("Vocabulary.Deleted", "Từ vựng đã bị xóa, không thể thêm vào chủ đề.");
        public static readonly Error VocabularyInactive = new("Vocabulary.Inactive", "Từ vựng đang không hoạt động, không thể thêm vào chủ đề.");
        public static readonly Error VocabularyAddFailed = new("Vocabulary.AddFailed", "Không thể thêm từ vựng vào chủ đề.");
        public static readonly Error VocabularyTransactionFailed = new("Vocabulary.TransactionFailed", "Thực hiện thất bại. Không có từ vựng nào được thêm vào chủ đề.");
        public static readonly Error VocabularyInUse = new("Vocabulary.VocabularyInUse", "Từ vựng này đang được sử dụng trong 1 chủ đề.");

        
        public static Error VocabularyWithIdNotFound(List<string> vocabularyIds)
            => new("Vocabulary.IdsNotFound", $"Các từ vựng sau không tồn tại: {string.Join(", ", vocabularyIds)}");

        public static Error VocabularyStatusInvalid(string text, string vocabularyId, string status)
            => new("Vocabulary.StatusInvalid", $"{text} (ID: {vocabularyId}) - {status}");
        public static readonly Error ExampleIdEmpty =
        new("VocabularyExample.ExampleIdEmpty", "ExampleId không được rỗng.");

        public static readonly Error ExampleNotFound =
            new("VocabularyExample.ExampleNotFound", "Không tìm thấy câu ví dụ.");
        public static readonly Error VocabularyIdEmpty =
    new("Vocabulary.VocabularyIdEmpty", "VocabularyId không được rỗng.");

        public static readonly Error ExamplesEmpty =
            new("VocabularyExample.ExamplesEmpty", "Danh sách câu ví dụ không được rỗng.");

        public static readonly Error ExampleSentenceEmpty =
            new("VocabularyExample.SentenceEmpty", "Sentence không được rỗng.");
        public static readonly Error ExampleDuplicate =
          new("VocabularyExample.ExampleDuplicate", "Câu ví dụ trông được trùng.");

        public static readonly Error VocabularyDeletedCannotUpdate =
    new("Vocabulary.DeletedCannotUpdate", "Vocabulary đã bị xóa, không thể cập nhật hoặc khôi phục.");

        // VocabulariesTopic
        public static readonly Error VocabTopicIsEmpty = new("VocabTopic.IsEmpty", "Topic này không có từ vựng.");

        // ============================================
        // NHÓM 13: WORD
        // ============================================

        // ============================================
        // NHÓM 14: AUDIO
        // ============================================
        public static readonly Error AudioGenerationFailed = new("Audio.GenerationFailed", "Không thể tạo file âm thanh.");
        public static readonly Error AudioUploadFailed = new("Audio.UploadFailed", "Không thể tải file âm thanh lên cloud.");

        // ============================================
        // NHÓM 15: FAVORITE WORD
        // ============================================
        public static readonly Error FavoriteWordNotFound = new("FavoriteWord.NotFound", "Từ vựng yêu thích không tồn tại.");
        public static readonly Error FavoriteWordAlreadyExists = new("FavoriteWord.AlreadyExists", "Từ vựng đã được thêm vào danh sách yêu thích.");
        public static readonly Error FavoriteWordUnauthorized = new("FavoriteWord.Unauthorized", "Bạn không có quyền thao tác với từ vựng yêu thích này.");

        // ============================================
        // NHÓM 16: FAVORITE TOPIC
        // ============================================
        public static readonly Error FavoriteTopicNotFound = new("FavoriteTopic.NotFound", "Chủ đề yêu thích không tồn tại.");
        public static readonly Error FavoriteTopicAlreadyExists = new("FavoriteTopic.AlreadyExists", "Chủ đề đã được thêm vào danh sách yêu thích.");
        public static readonly Error FavoriteTopicUnauthorized = new("FavoriteTopic.Unauthorized", "Bạn không có quyền thao tác với chủ đề yêu thích này.");

        // ============================================
        // NHÓM 17: QUESTION TYPE
        // ============================================
        public static readonly Error QuestionTypeNotFound = new("QuestionType.NotFound", "Loại câu hỏi không tồn tại.");
        public static readonly Error QuestionTypeCodeDuplicated = new("QuestionType.CodeDuplicated", "Mã loại câu hỏi đã tồn tại.");
        public static readonly Error QuestionTypeNameDuplicated = new("QuestionType.NameDuplicated", "Tên loại câu hỏi đã tồn tại.");
        public static readonly Error QuestionTypeInUse = new("QuestionType.InUse", "Loại câu hỏi đang được sử dụng, không thể xóa.");

        // ============================================
        // NHÓM 18: PASSAGE
        // ============================================
        public static readonly Error PassageNotFound = new("Passage.NotFound", "Đoạn văn không tồn tại.");
        public static readonly Error PassageTitleDuplicated = new("Passage.TitleDuplicated", "Tiêu đề đoạn văn đã tồn tại.");
        public static readonly Error PassageInUse = new("Passage.InUse", "Đoạn văn đang được sử dụng trong câu hỏi, không thể xóa.");

        // ============================================
        // NHÓM 19: QUESTION BANK
        // ============================================
        public static readonly Error QuestionBankHasDeleted = new("QuestionBank.HasDeleted", "Câu hỏi đã được xóa trước đó.");
        public static readonly Error QuestionBankNotFound = new("QuestionBank.NotFound", "Câu hỏi không tồn tại.");
        public static readonly Error QuestionBankInvalidOptions = new("QuestionBank.InvalidOptions", "Câu hỏi phải có từ 2 đến 4 đáp án.");
        public static readonly Error QuestionBankNoCorrectAnswer = new("QuestionBank.NoCorrectAnswer", "Câu hỏi phải có ít nhất một đáp án đúng.");
        public static readonly Error QuestionBankMultipleCorrectAnswers = new("QuestionBank.MultipleCorrectAnswers", "Câu hỏi chỉ được có một đáp án đúng.");
        public static readonly Error QuestionBankInvalidKeyOption = new("QuestionBank.InvalidKeyOption", "Đáp án phải có KeyOption từ '1' đến '4'.");
        public static readonly Error QuestionBankDuplicateKeyOption = new("QuestionBank.DuplicateKeyOption", "Không được trùng KeyOption trong các đáp án.");
        public static readonly Error QuestionBankNeedToHaveDraftStatus = new("QuestionType.QuestionBankNeedToHaveDraftStatus", "Câu hỏi này phải ở trạng thái soạn thảo.");

        public static Error PassageMediaTypeMismatch(PassageMediaType mediaType, QuestionSkill skill) => new(
        "Passage.MediaTypeMismatch",
        $"Loại media '{mediaType}' của bài đọc không phù hợp với kỹ năng '{skill}'."
        );
        public static readonly Error WritingNoOptions = new("QuestionBank.WritingNoOptions","Câu hỏi tự luận (Writing) không được có đáp án trắc nghiệm");
        // ============================================
        // NHÓM 20: QUESTION OPTION
        // ============================================
        public static readonly Error QuestionOptionNotFound = new("QuestionOption.NotFound", "Đáp án không tồn tại.");
        public static readonly Error QuestionOptionInvalidKeyOption = new("QuestionOption.InvalidKeyOption", "KeyOption phải là '1', '2', '3' hoặc '4'.");

        // ============================================
        // NHÓM 21: EXAM TEMPLATE
        // ============================================
        public static readonly Error ExamTemplateNotFound = new("ExamTemplate.NotFound", "Mẫu đề thi không tồn tại.");
        public static readonly Error ExamTemplateNameDuplicated = new("ExamTemplate.NameDuplicated", "Tên mẫu đề thi đã tồn tại.");
        public static readonly Error ExamTemplateInUse = new("ExamTemplate.InUse", "Mẫu đề thi đang được sử dụng, không thể xóa.");
        public static readonly Error ExamTemplateNoParts = new("ExamTemplate.NoParts", "Mẫu đề thi phải có ít nhất một phần.");
        public static readonly Error ExamTemplateCantDelete = new("ExamTemplate.CannotDelete", "Không thể xóa đề thi đang ở trạng thái Xuất bản (Published). Hãy hủy xuất bản về Nháp trước khi xóa.");
        // ============================================
        // NHÓM 22: TEMPLATE PART
        // ============================================
        public static readonly Error TemplatePartNotFound = new("TemplatePart.NotFound", "Phần thi không tồn tại.");
        public static readonly Error TemplatePartInvalidRange = new("TemplatePart.InvalidRange", "QuestionFrom phải nhỏ hơn hoặc bằng QuestionTo.");
        public static readonly Error TemplatePartRangeOverlap = new("TemplatePart.RangeOverlap", "Khoảng câu hỏi bị trùng với phần khác trong mẫu đề.");
        public static readonly Error TemplatePartQuestionRangeInvalid = new("TemplatePart.QuestionRangeInvalid", "Khoảng câu hỏi phải lớn hơn 0.");

        // ============================================
        // NHÓM 23: EXAM
        // ============================================
        public static readonly Error ExamNotFound = new("Exam.NotFound", "Đề thi không tồn tại.");
        public static readonly Error ExamTitleDuplicated = new("Exam.TitleDuplicated", "Tiêu đề đề thi đã tồn tại.");
        public static readonly Error ExamQuestionNotInPart = new("Exam.QuestionNotInPart", "Câu hỏi không nằm trong khoảng của bất kỳ phần nào trong mẫu đề.");
        public static readonly Error ExamQuestionSkillMismatch = new("Exam.QuestionSkillMismatch", "Kỹ năng của câu hỏi không khớp với kỹ năng của phần.");
        public static readonly Error ExamQuestionNoDuplicated = new("Exam.QuestionNoDuplicated", "Số thứ tự câu hỏi đã tồn tại trong đề thi.");
        public static readonly Error ExamQuestionBankNotFound = new("Exam.QuestionBankNotFound", "Câu hỏi trong ngân hàng không tồn tại.");
        public static readonly Error ExamTemplateCannotUpdateInUse = new("ExamTemplate.CannotUpdateInUse", "Mẫu đề thi đã được sử dụng trong kỳ thi, không thể cập nhật. Vui lòng sử dụng chức năng Sao chép (Duplicate).");
        public static readonly Error ExamTemplateCannotUpdatePublished = new("ExamTemplate.CannotUpdatePublished", "Mẫu đề đã xuất bản chỉ có thể cập nhật khi chưa được sử dụng.");

        // ============================================
        // NHÓM 24: COMMENT
        // ============================================
        public static readonly Error CommentNotFound = new("Comment.NotFound", OperationMessages.NotFound("bình luận"));
        public static readonly Error Forbidden = new("Comment.Forbidden", "Bạn không có quyền thực hiện thao tác này trên bình luận.");
        //LiveChat
        public static readonly Error ChatRoomNotFound = new("ChatRoom.NotFound", "Phòng chat không tồn tại.");
        public static readonly Error ChatRoomAlreadySupported = new("ChatRoom.AlreadySupported", "Phòng chat đã có nhân viên hỗ trợ.");
        //Mini-game
        public static readonly Error MiniGameNotFound = new("MiniGame.NotFound", "Trò chơi không tồn tại.");
        public static readonly Error MiniGameInvalidParameters = new("MiniGame.InvalidParameters", "Tham số trò chơi không hợp lệ.");
        public static readonly Error MiniGameMatchingVocabNotFound = new ("MiniGame.MatchingVocabNotFound", "Không tìm thấy từ vựng để tạo thẻ ghép.");
        //Excel
        public static readonly Error ExcelFileInvalidFormat = new("Excel.FileInvalidFormat", "Định dạng file Excel không hợp lệ.");
        public static readonly Error ExcelFileReadError = new("Excel.FileReadError", "Lỗi khi đọc file Excel.");
        public static readonly Error ExcelNoValidDataFound = new("Excel.NoValidDataFound", "Không tìm thấy dữ liệu hợp lệ trong file Excel.");
        public static readonly Error ExcelDataValidationFailed = new("Excel.DataValidationFailed", "Dữ liệu trong file Excel không hợp lệ.");
        public static readonly Error ExcelDataNull = new("Excel.DataNull", "Dữ liệu trong file Excel bị trống.");

        //Game
        public static readonly Error GameNotFound = new("Game.NotFound","Game không tồn tại.");
        public static readonly Error GameNameDuplicated = new( "Game.NameDuplicated","Tên game đã tồn tại.");
        public static readonly Error GameResultNotFound = new( "GameResult.NotFound","Không tìm thấy kết quả trò chơi cho user.");
  }
}