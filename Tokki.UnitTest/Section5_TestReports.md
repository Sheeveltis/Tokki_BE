# 5. Test Reports

> **Project**: TOKKI LEARNING MANAGEMENT SYSTEM
> **Project Code**: TK_CAPSTONE_2026
> **Report Generated**: 26/04/2026 21:00
> **Test Framework**: xUnit 2.9.2 + Moq 4.20.72 + FluentAssertions 8.8.0

---

## 5.1 Test Execution Summary

| Metric | Value |
|---|---|
| **Total Test Cases** | **2094** |
| **Passed** | **1923** |
| **Failed** | **171** |
| **Skipped** | **0** |
| **Pass Rate** | **91.83%** |
| **Execution Time** | **00:44.422** |
| **Test Start** | 2026-04-26T21:00:12.9540848+07:00 |
| **Test End** | 2026-04-26T21:00:57.3761870+07:00 |

---

## 5.2 Results Per Module

| # | Module | Total TCs | Passed | Failed | Skipped | Pass Rate |
|---|---|---|---|---|---|---|| 1 | **Accounts** | 178 | 157 | 21 | 0 | 88.2% |
| 2 | **Blogs** | 54 | 43 | 11 | 0 | 79.6% |
| 3 | **Categories** | 30 | 28 | 2 | 0 | 93.3% |
| 4 | **Cloudinary** | 31 | 28 | 3 | 0 | 90.3% |
| 5 | **Comments** | 30 | 27 | 3 | 0 | 90% |
| 6 | **EmailTemplates** | 116 | 113 | 3 | 0 | 97.4% |
| 7 | **Exam** | 139 | 129 | 10 | 0 | 92.8% |
| 8 | **ExamTemplates** | 90 | 87 | 3 | 0 | 96.7% |
| 9 | **Excel** | 73 | 57 | 16 | 0 | 78.1% |
| 10 | **FavoriteVocabulary** | 18 | 18 | 0 | 0 | 100% |
| 11 | **Gamification** | 8 | 1 | 7 | 0 | 12.5% |
| 12 | **Leaderboard** | 6 | 6 | 0 | 0 | 100% |
| 13 | **LiveChat** | 36 | 35 | 1 | 0 | 97.2% |
| 14 | **MiniGame** | 48 | 43 | 5 | 0 | 89.6% |
| 15 | **Other** | 72 | 68 | 4 | 0 | 94.4% |
| 16 | **Otps** | 50 | 49 | 1 | 0 | 98% |
| 17 | **Passages** | 36 | 33 | 3 | 0 | 91.7% |
| 18 | **Payments** | 42 | 40 | 2 | 0 | 95.2% |
| 19 | **PronunciationExample** | 36 | 32 | 4 | 0 | 88.9% |
| 20 | **PronunciationRule** | 43 | 43 | 0 | 0 | 100% |
| 21 | **QuestionBanks** | 207 | 185 | 22 | 0 | 89.4% |
| 22 | **QuestionTypes** | 36 | 31 | 5 | 0 | 86.1% |
| 23 | **Reports** | 36 | 36 | 0 | 0 | 100% |
| 24 | **Roadmap** | 83 | 80 | 3 | 0 | 96.4% |
| 25 | **StatisticBlog** | 18 | 18 | 0 | 0 | 100% |
| 26 | **Statistics** | 24 | 24 | 0 | 0 | 100% |
| 27 | **SystemConfigs** | 24 | 17 | 7 | 0 | 70.8% |
| 28 | **TemplateParts** | 32 | 30 | 2 | 0 | 93.8% |
| 29 | **TextToSpeech** | 6 | 6 | 0 | 0 | 100% |
| 30 | **Titles** | 40 | 32 | 8 | 0 | 80% |
| 31 | **Tools** | 2 | 2 | 0 | 0 | 100% |
| 32 | **Topics** | 112 | 112 | 0 | 0 | 100% |
| 33 | **TopikWriting** | 12 | 8 | 4 | 0 | 66.7% |
| 34 | **UserExam** | 118 | 116 | 2 | 0 | 98.3% |
| 35 | **UserTopicProgress** | 6 | 6 | 0 | 0 | 100% |
| 36 | **Utilities** | 1 | 1 | 0 | 0 | 100% |
| 37 | **VipPackages** | 24 | 24 | 0 | 0 | 100% |
| 38 | **VocabSpacedRepetition** | 12 | 12 | 0 | 0 | 100% |
| 39 | **Vocabulary** | 131 | 116 | 15 | 0 | 88.5% |
| 40 | **VocabularyExample** | 34 | 30 | 4 | 0 | 88.2% |
| | **GRAND TOTAL** | **2094** | **1923** | **171** | **0** | **91.83%** |

---

## 5.3 Failed Test Cases Detail
> [!WARNING]
> **171 test case(s) failed.** Details below:

| # | Module | Test Name | Error Message |
|---|---|---|---|| 1 | Accounts | `Accounts.ChangePasswordCommandHandlerTests.Handle_CorrectOldPassword_ShouldUpdateHashAndReturn200` | Expected result.Data to be a match with the expectation, but it differs at index 0:    ↓ (actual)   "Đổi mật khẩu thành công!"   "Ð?i m?t kh?u thành công!"    ↑ (expected) |
| 2 | Accounts | `Accounts.Commands.FacebookLoginBranchCoverageTests.Handle_SocialLoginExists_AccountLocked_Returns403` | Expected result.IsSuccess to be False, but found True. |
| 3 | Accounts | `Accounts.UpdateProfileCommandHandlerTests.Handle_UserNotFound_ShouldReturn404` | Expected result.StatusCode to be 404, but found 400 (difference of -4). |
| 4 | Accounts | `Accounts.Commands.GoogleLoginCommandHandlerTests.CheckAccountStatus_Inactive_Returns403` | Expected result.Errors.First().Code to be a match with the expectation, but it differs at index 8:            ↓ (actual)   "Account.AccountInActive"   "Account.InActive"            ↑ (expected) |
| 5 | Accounts | `Accounts.Commands.UpdateMyLevelCommandValidatorTests.Validate_InvalidEnumLevel_ShouldHaveError` | FluentValidation.TestHelper.ValidationTestException : Expected an error message of '*không hợp lệ*'. Actual message was ''Cấp độ' không hợp lệ. Giá trị hợp lệ: 1 (Level1), 2 (Level2), 3 (Level3), 4 (L... |
| 6 | Accounts | `Accounts.AdminSoftDeleteAccountCommandHandlerTests.Handle_MissingAdminUserId_ShouldReturn401` | Expected result.StatusCode to be 401, but found 400 (difference of -1). |
| 7 | Accounts | `Accounts.Commands.GoogleLoginCommandHandlerTests.CheckAccountStatus_Banned_Returns403` | Expected result.Errors.First().Code to be a match with the expectation, but it differs at index 1:     ↓ (actual)   "Auth.AccountBanned"   "Account.Banned"     ↑ (expected) |
| 8 | Accounts | `Accounts.Commands.FacebookCompleteRegistrationBranchCoverageTests.Handle_AccountBanned_ShouldReturn403` | Expected result.StatusCode to be 403, but found 409 (difference of 6). |
| 9 | Accounts | `Accounts.AdminUpdateUserCommandHandlerTests.Handle_TargetUserNotFound_ShouldReturn404` | Expected result.StatusCode to be 404, but found 400 (difference of -4). |
| 10 | Accounts | `Accounts.UpdateProfileCommandHandlerTests.Handle_NoUserId_ShouldReturn401` | Expected result.StatusCode to be 401, but found 400 (difference of -1). |
| 11 | Accounts | `Accounts.Commands.UpdateMyLevelCommandHandlerTests.Handle_LockedUser_ShouldReturn403` | Expected result.IsSuccess to be False, but found True. |
| 12 | Accounts | `Accounts.Commands.DeleteAccountCommandHandlerTests.Handle_UserNotFound_ShouldReturnFailureWithUserNotFound` | Expected result.Errors.First().Code to be a match with the expectation, but it differs at index 13:               ↓ (actual)   "…r.NotFoundById"   "…r.NotFound.Id"               ↑ (expected) |
| 13 | Accounts | `Accounts.Commands.GoogleLoginCommandHandlerTests.CheckAccountStatus_Locked_Returns403` | Expected result.Errors.First().Code to be a match with the expectation, but it differs at index 1:     ↓ (actual)   "Auth.AccountLocked"   "Account.Locked"     ↑ (expected) |
| 14 | Accounts | `Accounts.Commands.FacebookLoginCommandHandlerTests.Handle_ExistingBannedAccount_ShouldReturn403` | Expected resultMerged.StatusCode to be 403, but found 401 (difference of -2). |
| 15 | Accounts | `Accounts.ChangePasswordCommandHandlerTests.Handle_UserNotFound_ShouldReturn404` | Expected result.StatusCode to be 404, but found 400 (difference of -4). |
| 16 | Accounts | `Accounts.Commands.UpdateMyLevelCommandHandlerTests.Handle_InactiveUser_ShouldReturn403` | Expected result.Errors.First().Code to be a match with the expectation, but it differs at index 8:            ↓ (actual)   "Account.AccountInActive"   "Account.InActive"            ↑ (expected) |
| 17 | Accounts | `Accounts.ResetPasswordCommandHandlerTests.Handle_UserNotFound_ShouldReturn404` | Expected result.StatusCode to be 404, but found 400 (difference of -4). |
| 18 | Accounts | `Accounts.AdminSoftDeleteAccountCommandHandlerTests.Handle_TargetUserNotFound_ShouldReturn404` | Expected result.StatusCode to be 404, but found 400 (difference of -4). |
| 19 | Accounts | `Accounts.Commands.FacebookLoginBranchCoverageTests.Handle_EmailExists_AccountLocked_Returns403` | Expected result.StatusCode to be 403, but found 409 (difference of 6). |
| 20 | Accounts | `Accounts.Commands.UpdateMyLevelCommandHandlerTests.Handle_BannedUser_ShouldReturn403` | Expected result.Errors.First().Code to be a match with the expectation, but it differs at index 1:     ↓ (actual)   "Auth.AccountBanned"   "Account.Banned"     ↑ (expected) |
| 21 | Accounts | `Accounts.Commands.FacebookCompleteRegistrationBranchCoverageTests.Handle_FacebookApiThrowsException_ShouldReturn500` | Expected result.StatusCode to be 500, but found 401 (difference of -99). |
| 22 | Blogs | `Blogs.Queries.GetPagedBlogsQueryHandlerTests.Handle_ProperTagsData_ShouldMapListStrictly` | System.ArgumentNullException : Value cannot be null. (Parameter 'key') |
| 23 | Blogs | `Blogs.ApproveBlogCommandHandlerTests.Handle_Unauthorized_ShouldReturn401` | Expected result.StatusCode to be 401, but found 400 (difference of -1). |
| 24 | Blogs | `Blogs.SubmitBlogForApprovalCommandHandlerTests.Handle_Unauthorized_ShouldReturn401` | Expected result.StatusCode to be 401, but found 400 (difference of -1). |
| 25 | Blogs | `Blogs.GetBlogByIdQueryHandlerTests.Handle_WithTags_ShouldMapTagsCorrectly` | Expected result.IsSuccess to be True, but found False. |
| 26 | Blogs | `Blogs.UpdateBlogCommandHandlerTests.Handle_DatabaseException_ShouldReturn500` | Expected result.Message "Lỗi hệ thống: Mock Update Failure" to contain "L?i h? th?ng: Mock Update Failure". |
| 27 | Blogs | `Blogs.GetBlogByIdQueryHandlerTests.Handle_ValidRequest_WithAuthorInfo_ShouldReturn200` | Expected result.IsSuccess to be True, but found False. |
| 28 | Blogs | `Blogs.GetBlogByIdQueryHandlerTests.Handle_ValidRequest_MissingAuthor_ShouldReturnAnonymous` | Expected result.IsSuccess to be True, but found False. |
| 29 | Blogs | `Blogs.Queries.GetPagedBlogsQueryHandlerTests.Handle_MissingCategoryName_ShouldMapToDefaultString` | System.ArgumentNullException : Value cannot be null. (Parameter 'key') |
| 30 | Blogs | `Blogs.CreateBlogCommandHandlerTests.Handle_DatabaseThrowsException_ShouldReturn500` | Expected result.Message "Không thể thêm Bài viết. Vui lòng thử lại." to contain "Lỗi SQL chi tiết: Mock DB Failure". |
| 31 | Blogs | `Blogs.GetBlogByIdQueryHandlerTests.Handle_WithoutCategoryInclude_ShouldReturnNA` | Expected result.IsSuccess to be True, but found False. |
| 32 | Blogs | `Blogs.RejectBlogCommandHandlerTests.Handle_Unauthorized_ShouldReturn401` | Expected result.StatusCode to be 401, but found 400 (difference of -1). |
| 33 | Categories | `Categories.Queries.GetCategoryByIdQueryHandlerTests.Handle_CategoryNotFoundMessage_ShouldReadCorrectly` | Expected result.Message "Không thể lấy thông tin Danh mục. Vui lòng thử lại sau." to contain "th?t b?i". |
| 34 | Categories | `Categories.Commands.UpdateCategoryCommandHandlerTests.Handle_ValidUpdate_ShouldRecalculateSlug` | Expected category.Slug to be a match with the expectation, but it differs at index 3:       ↓ (actual)   "ngh-thut-mi-CAT-001"   "nghe-thuat-moi-CAT-001"       ↑ (expected) |
| 35 | Cloudinary | `Cloudinary.Commands.UploadImageCommandValidatorTests.Validate_NullContentType_ShouldHaveError` | System.NullReferenceException : Object reference not set to an instance of an object. |
| 36 | Cloudinary | `Cloudinary.UploadVocabularyImageByUrlCommandHandlerTests.Handle_BothUploadPathsFail_ErrorMessageContainsFallback` | Expected result.Message "HTTP Error khi tải ảnh từ nguồn: NotFound - Response status code does not indicate success: 404 (Not Found)." to contain "fallback". |
| 37 | Cloudinary | `Cloudinary.Commands.UploadImageCommandValidatorTests.Validate_NullFile_ShouldHaveError` | System.NullReferenceException : Object reference not set to an instance of an object. |
| 38 | Comments | `Comments.CreateCommentCommandHandlerTests.Handle_RepositoryThrows_ShouldReturn500` | Expected result.IsSuccess to be False, but found True. |
| 39 | Comments | `Comments.GetCommentsQueryHandlerTests.Handle_DeletedComment_ShouldShowMaskedContent` | Expected result.Data.First().Content "Bình luận này đã bị xóa." to contain "dã b? xóa". |
| 40 | Comments | `Comments.CreateCommentCommandHandlerTests.Handle_BlogNotFound_ShouldReturn404` | Expected result.IsSuccess to be False, but found True. |
| 41 | EmailTemplates | `EmailTemplates.Commands.UpdateEmailCampaignCommandValidatorTests.Validate_InvalidStatusUpdate_ShouldHaveError` | FluentValidation.TestHelper.ValidationTestException : Expected a validation error for property Status ---- Properties with Validation Errors: [0]: Status.Value  |
| 42 | EmailTemplates | `EmailTemplates.Commands.UpdateEmailCampaignCommandHandlerTests.Handle_JobNotFound_ShouldReturn404` | Expected result.Message to be a match with the expectation, but it differs at index 12:           ↓ (actual)   "…tìm thấy campaign!"   "…tìm th?y campaign!"           ↑ (expected) |
| 43 | EmailTemplates | `EmailTemplates.Commands.UpdateEmailCampaignCommandValidatorTests.Validate_NoneTargetWithoutEmails_ShouldHaveError` | FluentValidation.TestHelper.ValidationTestException : Expected a validation error for property  ---- Properties with Validation Errors: [0]: Người nhận  |
| 44 | Exam | `Exam.Queries.GetQuestionsByPartQueryHandlerTests.Handle_SkillListening_ShouldMapToAudioMediaType` | System.InvalidOperationException : Sequence contains no elements |
| 45 | Exam | `Exam.CreateExamCommandHandlerTests.Handle_ValidData_ShouldCreateExamSuccessfully` | Expected result.IsSuccess to be True, but found False. |
| 46 | Exam | `Exam.Queries.GetQuestionsByPartQueryHandlerTests.Handle_SkillWriting_ShouldFallbackToImageMediaType` | System.InvalidOperationException : Sequence contains no elements |
| 47 | Exam | `Exam.Queries.GetQuestionsByPartQueryHandlerTests.Handle_SkillIsNull_ShouldMapToImageDefault` | System.InvalidOperationException : Sequence contains no elements |
| 48 | Exam | `Exam.CreateExamCommandHandlerTests.Handle_IncompleteSkillDurations_ShouldReturn400` | Expected result.Message "Vui lòng nhập thời gian cho phần 'Reading'." to contain "Please enter a valid test time". |
| 49 | Exam | `Exam.Queries.GetQuestionsByPartQueryHandlerTests.Handle_PassageNotNull_MapsCompletePassageSafely` | System.InvalidOperationException : Sequence contains no elements |
| 50 | Exam | `Exam.Queries.GetQuestionsByPartQueryHandlerTests.Handle_PassagesNull_MapsToEmptyWithoutError` | System.InvalidOperationException : Sequence contains no elements |
| 51 | Exam | `Exam.CreateExamCommandHandlerTests.Handle_NotEnoughQuestionsInBank_ShouldReturn400` | Expected result.Message "Vui lòng nhập thời gian cho phần '0'." to contain "insufficient". |
| 52 | Exam | `Exam.Queries.GetQuestionsByPartQueryHandlerTests.Handle_OptionsMapping_ShouldStrictlyOrderAscending` | System.InvalidOperationException : Sequence contains no elements |
| 53 | Exam | `Exam.AddQuestionToExamCommandHandlerTests.Handle_ExamSlotNotFound_ShouldReturn404` | Expected result.Message "Không tìm thấy câu hỏi số 99 trong đề thi này để thay thế." to contain "Question number 99 not found". |
| 54 | ExamTemplates | `ExamTemplates.Commands.DuplicateExamTemplateCommandHandlerTests.Handle_TemplateNotFound_ReturnsFailure404` | Expected result.Message "Mẫu đề thi không tồn tại." to contain "M?u d? thi không t?n t?i". |
| 55 | ExamTemplates | `ExamTemplates.Commands.DuplicateExamTemplateCommandHandlerTests.Handle_Exception_Returns500ServerError` | System.Exception : Database error |
| 56 | ExamTemplates | `ExamTemplates.DuplicateExamTemplateCommandHandlerTests.Handle_TemplateHasParts_ShouldDuplicateParts` | System.ArgumentException : Invalid callback. Setup on method with parameters (IEnumerable<TemplatePart>) cannot invoke callback with parameters (List<TemplatePart>). |
| 57 | Excel | `Excel.ExportAccountsQueryHandlerTests.Handle_AccountsEmpty_ShouldExportEmptyFile` | Expected result.Data.FileName "Tokki_Account_26042026.xlsx" to contain "Accounts_Export_". |
| 58 | Excel | `Excel.ExportVocabByTopicQueryHandlerTests.Handle_TopicNotFound_ShouldReturn404` | Expected result.StatusCode to be 404, but found 400 (difference of -4). |
| 59 | Excel | `Excel.Commands.ImportPronunciationRulesCommandHandlerTests.Handle_DatabaseException_ReturnsFailure` | Expected result.Message to be a match with the expectation, but it differs at index 0:    ↓ (actual)   "Lỗi lưu dữ liệu vào database."   "DATABASE_ERROR"    ↑ (expected) |
| 60 | Excel | `Excel.ExportVocabByTopicQueryHandlerTests.Handle_VocabListNull_ShouldReturnVocabTopicIsEmpty` | Expected result.StatusCode to be 404, but found 400 (difference of -4). |
| 61 | Excel | `Excel.ImportQuestionTypesCommandHandlerTests.Handle_DuplicateCodeInFile_ShouldFailSecondRow` | Expected result.Data!.FailureList to contain 1 item(s), but found 2: {     Tokki.Application.UseCases.Excel.DTOs.QuestionTypePreviewDTO     {         Code = "DUP_CODE",         Name = "Reading 1",... |
| 62 | Excel | `Excel.ExportVocabByTopicQueryHandlerTests.Handle_ValidTopicWithVocabs_ShouldExportFile` | Expected result.Data.FileName to be a match with the expectation, but it differs at index 0:    ↓ (actual)   "Tokki_Vocab_Korean Basics_26042026.xlsx"   "Korean Basics.xlsx"    ↑ (expected) |
| 63 | Excel | `Excel.ExportVocabByTopicQueryHandlerTests.Handle_VocabListEmpty_ShouldReturnVocabTopicIsEmpty` | Expected result.StatusCode to be 404, but found 400 (difference of -4). |
| 64 | Excel | `Excel.Commands.ImportPronunciationRulesCommandHandlerTests.Handle_ExcelNull_ShouldReturnError` | Expected result.Message "Không tìm thấy dữ liệu hợp lệ trong file Excel." to contain "không tìm thấy dữ liệu hợp lệ". |
| 65 | Excel | `Excel.ExportQuestionTypesQueryHandlerTests.Handle_ValidData_FileNameHasCorrectPrefix` | Expected result.Data.FileName to start with "QuestionTypes_", but "Tokki_QuestionType_26042026.xlsx" differs near "Tok" (index 0). |
| 66 | Excel | `Excel.ExportVocabByTopicQueryHandlerTests.Handle_LongTopicName_ShouldTruncateSheetName` | Expected result.Data.FileName to be a match with the expectation, but it differs at index 1:     ↓ (actual)   "Tokki_Vocab_This Is A Very Long Topic Name That Exceeds Thirty Characters_260420…"   "... |
| 67 | Excel | `Excel.Commands.ImportPronunciationRulesCommandHandlerTests.Handle_ExcelEmpty_ShouldReturnError` | Expected result.Message to be a match with the expectation, but it differs at index 0:    ↓ (actual)   "Không tìm thấy dữ liệu hợp lệ trong file Excel."   "EXCEL_EMPTY"    ↑ (expected) |
| 68 | Excel | `Excel.ExportQuestionTypesQueryHandlerTests.Handle_ValidData_ShouldMapAndExportCorrectly` | Expected result.Data.FileName "Tokki_QuestionType_26042026.xlsx" to contain "QuestionTypes_". |
| 69 | Excel | `Excel.ImportPronunciationExampleCommandHandlerTests.Handle_AddRangeThrows_ShouldReturnDatabaseError` | Expected result.StatusCode to be 500, but found 400 (difference of -100). |
| 70 | Excel | `Excel.ExportAccountsQueryHandlerTests.Handle_ValidData_ShouldReturnWrappedFileBytes` | Expected result.Data.FileName "Tokki_Account_26042026.xlsx" to contain "Accounts_Export_". |
| 71 | Excel | `Excel.ImportQuestionTypesCommandHandlerTests.Handle_DatabaseThrowsException_ShouldReturnDbErrorFailure` | Expected result.IsSuccess to be False, but found True. |
| 72 | Excel | `Excel.ImportQuestionTypesCommandHandlerTests.Handle_ValidRow_ShouldCreateEntityAndInsert` | Expected result.Data!.SuccessList to contain 1 item(s), but found 0: {empty}. |
| 73 | Gamification | `Gamification.Commands.AddGameXpCommandHandlerTests.Handle_EmptyUserId_ShouldFail` | Expected result.Message "UserId không hợp lệ." to contain "không h?p l? =". |
| 74 | Gamification | `Gamification.Commands.AddGameXpCommandHandlerTests.Handle_GainsLevel_SetsIsLevelUp` | System.ArgumentNullException : Value cannot be null. (Parameter 'source') |
| 75 | Gamification | `Gamification.Commands.AddGameXpCommandHandlerTests.Handle_EmptyConfig_UsesDefaultLimit` | System.ArgumentNullException : Value cannot be null. (Parameter 'source') |
| 76 | Gamification | `Gamification.Commands.AddGameXpCommandHandlerTests.Handle_DailyLimitReached_AmountBecomesZero` | System.ArgumentNullException : Value cannot be null. (Parameter 'source') |
| 77 | Gamification | `Gamification.Commands.AddGameXpCommandHandlerTests.Handle_DailyLimitPartial_AmountTrimmed` | System.ArgumentNullException : Value cannot be null. (Parameter 'source') |
| 78 | Gamification | `Gamification.Commands.AddGameXpCommandHandlerTests.Handle_UserNotFound_ShouldFail` | Expected result.Message "Không tìm thấy user với id: uid" to contain "Không tìm th?y user". |
| 79 | Gamification | `Gamification.Commands.AddGameXpCommandHandlerTests.Handle_NonMiniGameSource_AddsFullAmount` | Expected result.Message "Cộng thành công 50 XP." to contain "C?ng thành công 50 XP". |
| 80 | LiveChat | `LiveChat.JoinSupportChatCommandHandlerTests.Handle_ValidRequestMissingUserInfo_ShouldUseDefaultFallback` | Moq.MockException :  Expected invocation on the mock once, but was 0 times: x => x.SendMessageToRoomAsync("R1", It.Is<ChatMessage>(m => m.Content.Contains("Nhân viên h? tr?")))  Performed invocatio... |
| 81 | MiniGame | `MiniGame.Queries.Wordle.GetWordleResultHandlerTests.Handle_GameNull_ShouldReturnFailure` | Expected result.Message "Không tìm thấy thông tin trò chơi." to contain "Không tìm th?y thông tin trò choi". |
| 82 | MiniGame | `MiniGame.SubmitWordleGuessHandlerTests.Handle_GameAlreadyOver_ShouldReturnFailure` | Expected result.Message "Lượt chơi hôm nay của bạn cho từ này đã kết thúc." to contain "end". |
| 83 | MiniGame | `MiniGame.Queries.Wordle.GetWordleResultHandlerTests.Handle_ProgressNull_ShouldReturnFailure` | Expected result.Message "Bạn cần hoàn thành và chiến thắng trò chơi để xem kết quả này." to contain "B?n c?n hoàn thành". |
| 84 | MiniGame | `MiniGame.Queries.Wordle.GetWordleResultHandlerTests.Handle_ProgressIsWonFalse_ShouldReturnFailure` | Expected result.Message "Bạn cần hoàn thành và chiến thắng trò chơi để xem kết quả này." to contain "chi?n th?ng trò choi". |
| 85 | MiniGame | `MiniGame.SubmitWordleSentenceCommandHandlerTests.Handle_AiScoreExactlyHalfPoint_ShouldRoundUp_MightFail` | Expected capturedSubmission!.AiScore to be 85 because ⚠️ WILL FAIL: Math.Round(84.5) = 84 due to Banker's Rounding, not 85, but found 84 (difference of -1). |
| 86 | Other | `Tokki.UnitTest.Application.Common.Helpers.LevelEngineTests.GetLevel_HighXp_ShouldCalculateCurveProperly` | Expected level to be 5, but found 4. |
| 87 | Other | `Tokki.UnitTest.Application.Common.Helpers.HangulAndWordleHelperBranchTests.CalculateFeedback_CompletelyWrong_ShouldBeAllGray` | Expected result to contain only items matching (fb.BlockColor == "Gray"), but {     Tokki.Application.UseCases.MiniGame.DTOs.BlockFeedback     {         BlockColor = "Yellow",         Character = ... |
| 88 | Other | `Tokki.UnitTest.Application.Common.Helpers.EnumExtensionsTests.GetDescription_UndefinedEnumValue_ReturnsIntString` | System.ArgumentNullException : Value cannot be null. (Parameter 'element') |
| 89 | Other | `Tokki.UnitTest.Application.Common.Helpers.WordleHelperTests.CalculateFeedback_NoMatch_ReturnsGray` | Expected result[0].BlockColor to be "Gray" with a length of 4, but "Yellow" has a length of 6, differs near "Yel" (index 0). |
| 90 | Otps | `Otps.VerifyForgotPasswordOtpCommandHandlerTests.Handle_CorrectOtp_ShouldReturnResetToken` | Expected result.Data to be a match with the expectation, but it differs at index 12:               ↓ (actual)   "…SET-TOKEN-XYZ"   "…SET-TOKEN-ABC"               ↑ (expected) |
| 91 | Passages | `Passages.Commands.UpdatePassageCommandHandlerTests.Handle_TextMissingContent_ShouldReturn400` | Expected result.Message "Loại Văn bản bắt buộc phải có nội dung." to contain "b?t bu?c ph?i có n?i dung". |
| 92 | Passages | `Passages.Commands.UpdatePassageCommandHandlerTests.Handle_TitleEmpty_ShouldReturn400` | Expected result2.Message "Tiêu đề không được để trống." to contain "Tiêu d? không du?c d? tr?ng". |
| 93 | Passages | `Passages.CreatePassageCommandHandlerTests.Handle_ValidTextPassage_ShouldReturn201WithId` | Expected result.Data to be "PASS-NEW", but "PASS-001" differs near "001" (index 5). |
| 94 | Payments | `Payments.Queries.PaymentHistoryDtoTests.StatusDisplay_Failed_ShouldFormatStringFallback` | Expected dto.StatusDisplay to be a match with the expectation, but it differs at index 0:    ↓ (actual)   "Đang chờ thanh toán"   "Thất bại"    ↑ (expected) |
| 95 | Payments | `Payments.Commands.ProcessWebhookCommandHandlerTests.Handle_InsufficientAmount_ShouldReturnMessage` | Expected result.Data "Số tiền chuyển (50,000) thấp hơn giá trị gói (100,000)." to contain "không d? so v?i hóa don". |
| 96 | PronunciationExample | `PronunciationExample.Commands.CreatePronunciationExampleCommandHandlerTests.Handle_RuleNotFound_ShouldReturn404` | Expected result.Message to be a match with the expectation, but it differs at index 0:    ↓ (actual)   "Quy tắc phát âm không tồn tại."   "Rule.NotFound"    ↑ (expected) |
| 97 | PronunciationExample | `PronunciationExample.Queries.GetPagedPronunciationExamplesQueryHandlerTests.Handle_EdgeMapping1` | System.ArgumentNullException : Value cannot be null. (Parameter 'source') |
| 98 | PronunciationExample | `PronunciationExample.Queries.GetPagedPronunciationExamplesQueryHandlerTests.Handle_EdgeMapping2` | System.ArgumentNullException : Value cannot be null. (Parameter 'source') |
| 99 | PronunciationExample | `PronunciationExample.Queries.GetPagedPronunciationExamplesQueryHandlerTests.Handle_EdgeMapping3` | System.ArgumentNullException : Value cannot be null. (Parameter 'source') |
| 100 | QuestionBanks | `QuestionBanks.Commands.QuestionOptions.UpdateQuestionOptionCommandHandlerTests.Handle_DuplicateKeyOption_ShouldReturn400` | Expected result.Errors.First().Description "Dữ liệu đầu vào không hợp lệ." to contain "dã t?n t?i". |
| 101 | QuestionBanks | `QuestionBanks.Commands.RejectQuestionBanksCommandValidatorTests.Validate_DuplicateIds_ShouldHaveError` | FluentValidation.TestHelper.ValidationTestException : Expected an error message of 'Danh sách mã câu h?i b? trùng.'. Actual message was 'Danh sách mã câu hỏi bị trùng.' |
| 102 | QuestionBanks | `QuestionBanks.Commands.CreateQuestionBankByStaffCommandValidatorTests.ValidateAsync_NoContentNoImageInOption_ShouldHaveError` | FluentValidation.TestHelper.ValidationTestException : Expected an error message of 'Ðáp án ph?i có n?i dung text ho?c ?nh.'. Actual message was 'Đáp án phải có nội dung text hoặc ảnh.' |
| 103 | QuestionBanks | `QuestionBanks.Commands.ApproveQuestionBanksCommandValidatorTests.Validate_NullIds_HasValidationError` | System.ArgumentNullException : Value cannot be null. (Parameter 'source') |
| 104 | QuestionBanks | `QuestionBanks.Commands.ApproveQuestionBanksCommandValidatorTests.Validate_WhitespaceIds_HasValidationError` | FluentValidation.TestHelper.ValidationTestException : Expected an error message of 'Danh sách mã câu h?i không du?c r?ng.'. Actual message was 'Danh sách mã câu hỏi không được rỗng.' |
| 105 | QuestionBanks | `QuestionBanks.Commands.RejectQuestionBank.RejectQuestionBankCommandValidatorTests.Validate_NullQuestionBankIds_ShouldHaveError` | System.ArgumentNullException : Value cannot be null. (Parameter 'source') |
| 106 | QuestionBanks | `QuestionBanks.Commands.ApproveQuestionBanksCommandHandlerTests.Handle_AlreadyDeleted_ShouldReturn400` | Expected result.Message "QuestionBankId q1 đã bị xóa, không thể duyệt." to contain "dã b? xóa". |
| 107 | QuestionBanks | `QuestionBanks.Commands.RejectQuestionBank.RejectQuestionBankCommandHandlerTests.Handle_DeletedStatus_ShouldReturn400` | Expected result.Errors[0].Description "Dữ liệu đầu vào không hợp lệ." to contain "đã bị xóa". |
| 108 | QuestionBanks | `QuestionBanks.Commands.RejectQuestionBanksCommandValidatorTests.Validate_QuestionBankIdsNull_ShouldHaveError` | System.ArgumentNullException : Value cannot be null. (Parameter 'source') |
| 109 | QuestionBanks | `QuestionBanks.Commands.CreateQuestionBankCommandHandlerTests.Handle_TypeInactive_ShouldReturn400` | Expected result.Message "Loại câu hỏi đang bị vô hiệu hóa." to contain "vô hi?u hóa". |
| 110 | QuestionBanks | `QuestionBanks.Commands.QuestionOptions.CreateQuestionOptionCommandHandlerTests.Handle_DuplicateKeyOption_ShouldReturn400` | Expected result.Errors.First().Description "Dữ liệu đầu vào không hợp lệ." to contain "đã tồn tại". |
| 111 | QuestionBanks | `QuestionBanks.Commands.RejectQuestionBanksCommandValidatorTests.Validate_QuestionBankIdsEmptyStringsOnly_ShouldHaveError` | FluentValidation.TestHelper.ValidationTestException : Expected an error message of 'Danh sách mã câu h?i không du?c r?ng.'. Actual message was 'Danh sách mã câu hỏi không được rỗng.' |
| 112 | QuestionBanks | `QuestionBanks.Commands.CreateQuestionBankByStaffCommandHandlerTests.Handle_ReadingMissingContent_ShouldReturn400` | Expected result.Message "Câu hỏi Reading bắt buộc phải có Content." to contain "b?t bu?c ph?i có Content". |
| 113 | QuestionBanks | `QuestionBanks.Commands.ApproveQuestionBanksCommandValidatorTests.Validate_DuplicateWhitespaceIds_HasValidationError` | FluentValidation.TestHelper.ValidationTestException : Expected an error message of 'Danh sách mã câu h?i b? trùng.'. Actual message was 'Danh sách mã câu hỏi bị trùng.' |
| 114 | QuestionBanks | `QuestionBanks.Commands.QuestionOptions.UpdateQuestionOptionCommandHandlerTests.Handle_RemoveSingleCorrect_ShouldReturn400` | Expected result.Errors.First().Description "Dữ liệu đầu vào không hợp lệ." to contain "ít nh?t m?t dáp án dúng". |
| 115 | QuestionBanks | `QuestionBanks.Commands.RejectQuestionBanksCommandValidatorTests.Validate_EmptyRejectReason_ShouldHaveError` | FluentValidation.TestHelper.ValidationTestException : Expected an error message of ''Lý do t? ch?i' không du?c b? tr?ng.'. Actual message was ''Lý do từ chối' must not be empty.' |
| 116 | QuestionBanks | `QuestionBanks.Commands.CreateQuestionBankByStaffCommandHandlerTests.Handle_TypeInactive_ShouldReturn400` | Expected result.Message "Loại câu hỏi đang bị vô hiệu hóa." to contain "vô hi?u hóa". |
| 117 | QuestionBanks | `QuestionBanks.Commands.ApproveQuestionBanksCommandHandlerTests.Handle_NotPendingApproval_ShouldReturnValidationFailed` | Expected result.Message "QuestionBankId q1 không ở trạng thái PendingApproval." to contain "không ? tr?ng thái PendingApproval". |
| 118 | QuestionBanks | `QuestionBanks.Commands.ApproveQuestionBanksCommandValidatorTests.Validate_DuplicateTargetIds_HasValidationError` | FluentValidation.TestHelper.ValidationTestException : Expected an error message of 'Danh sách mã câu h?i b? trùng.'. Actual message was 'Danh sách mã câu hỏi bị trùng.' |
| 119 | QuestionBanks | `QuestionBanks.Commands.UpdateQuestionBank.UpdateQuestionBankCommandHandlerTests.Handle_StatusAssigned_ShouldReturn403` | Expected result.Errors[0].Description "Bạn không có quyền thực hiện thao tác này trên bình luận." to contain "tr?ng thái Assigned". |
| 120 | QuestionBanks | `QuestionBanks.Commands.RejectQuestionBanksCommandValidatorTests.Validate_DuplicateIdsTrimmed_ShouldHaveError` | FluentValidation.TestHelper.ValidationTestException : Expected an error message of 'Danh sách mã câu h?i b? trùng.'. Actual message was 'Danh sách mã câu hỏi bị trùng.' |
| 121 | QuestionBanks | `QuestionBanks.Commands.QuestionOptions.CreateQuestionOptionCommandHandlerTests.Handle_OptionLimitExceeded_ShouldReturn400` | Expected result.Errors.First().Description "Dữ liệu đầu vào không hợp lệ." to contain "quá 4 đáp án". |
| 122 | QuestionTypes | `QuestionTypes.Commands.UpdateQuestionType.UpdateQuestionTypeCommandHandlerTests.Handle_CodeExists_ShouldReturnFailure` | Expected result.Message "Mã code đã tồn tại." to contain "Mã code dã t?n t?i". |
| 123 | QuestionTypes | `QuestionTypes.Commands.UpdateQuestionType.UpdateQuestionTypeCommandHandlerTests.Handle_NameExists_ShouldReturnFailure` | Expected result.Message "Tên loại câu hỏi đã tồn tại." to contain "Tên lo?i câu h?i dã t?n t?i". |
| 124 | QuestionTypes | `QuestionTypes.Commands.UpdateQuestionType.UpdateQuestionTypeCommandHandlerTests.Handle_NotFound_ShouldReturnFailure` | Expected result.Message "Không tìm thấy loại câu hỏi." to contain "Không tìm th?y". |
| 125 | QuestionTypes | `QuestionTypes.UpdateQuestionTypeCommandHandlerTests.Handle_DuplicateName_ShouldReturnFailure` | Expected result.Message "Tên loại câu hỏi đã tồn tại." to contain "tên". |
| 126 | QuestionTypes | `QuestionTypes.CreateQuestionTypeCommandHandlerTests.Handle_DuplicateName_ShouldReturnFailure` | Expected result.Message "Tên loại câu hỏi đã tồn tại." to contain "tên". |
| 127 | Roadmap | `Roadmap.Commands.GenerateRoadmapCommandValidatorTests.Validate_InvalidTargetAim_ShouldHaveError` | FluentValidation.TestHelper.ValidationTestException : Expected a validation error for property TargetAim |
| 128 | Roadmap | `Roadmap.Commands.SubmitExamCommandHandlerTests.Handle_ExamNotFound_ReturnsFailure404` | Expected result.Message "Đề thi không tồn tại hoặc không có câu hỏi." to contain "Ð? thi không t?n t?i". |
| 129 | Roadmap | `Roadmap.GenerateNextWeekCommandHandlerTests.Handle_ExamScoreEvaluatedWithWarning` | Expected result.Data!.HasWarning to be True, but found False. |
| 130 | SystemConfigs | `SystemConfigs.GetSystemConfigByKeyQueryHandlerTests.Handle_ActiveConfig_IsActiveTrueInDto` | System.NullReferenceException : Object reference not set to an instance of an object. |
| 131 | SystemConfigs | `SystemConfigs.GetAllSystemConfigsQueryHandlerTests.Handle_RepoReturnsConfigs_DtoFieldsMappedCorrectly` | System.InvalidOperationException : Nullable object must have a value. |
| 132 | SystemConfigs | `SystemConfigs.GetSystemConfigByKeyQueryHandlerTests.Handle_ConfigFound_AllDtoFieldsMapped` | System.NullReferenceException : Object reference not set to an instance of an object. |
| 133 | SystemConfigs | `SystemConfigs.GetAllSystemConfigsQueryHandlerTests.Handle_WithPage3Size20_PagingMetadataCorrect` | System.InvalidOperationException : Nullable object must have a value. |
| 134 | SystemConfigs | `SystemConfigs.GetAllSystemConfigsQueryHandlerTests.Handle_RepoReturnsConfigs_ShouldReturn200WithPagedResult` | System.InvalidOperationException : Nullable object must have a value. |
| 135 | SystemConfigs | `SystemConfigs.GetAllSystemConfigsQueryHandlerTests.Handle_ConfigsIncludeInactive_AllReturnedInList` | System.InvalidOperationException : Nullable object must have a value. |
| 136 | SystemConfigs | `SystemConfigs.GetSystemConfigByKeyQueryHandlerTests.Handle_ConfigFound_ShouldReturn200WithDto` | Expected result.IsSuccess to be True, but found False. |
| 137 | TemplateParts | `TemplateParts.CreateTemplatePartCommandHandlerTests.Handle_ExamTemplateNotFound_ShouldReturnFailure` | Expected result.IsSuccess to be False, but found True. |
| 138 | TemplateParts | `TemplateParts.CreateTemplatePartCommandHandlerTests.Handle_RepositoryThrowsException_ShouldReturn500` | Expected result.StatusCode to be 500, but found 400 (difference of -100). |
| 139 | Titles | `Titles.CreateTitleCommandHandlerTests.Handle_ValidRequest_ShouldReturn201WithTitle` | System.NullReferenceException : Object reference not set to an instance of an object. |
| 140 | Titles | `Titles.UpdateTitleCommandHandlerTests.Handle_ValidRequest_UpdateCalledOnce` | System.NullReferenceException : Object reference not set to an instance of an object. |
| 141 | Titles | `Titles.UpdateTitleCommandHandlerTests.Handle_SameName_ShouldNotCallGetTitleByName` | System.NullReferenceException : Object reference not set to an instance of an object. |
| 142 | Titles | `Titles.CreateTitleCommandHandlerTests.Handle_ValidRequest_AddCalledOnce` | System.NullReferenceException : Object reference not set to an instance of an object. |
| 143 | Titles | `Titles.CreateTitleCommandHandlerTests.Handle_NegativeXP_ShouldReturn400Failure` | System.NullReferenceException : Object reference not set to an instance of an object. |
| 144 | Titles | `Titles.CreateTitleCommandHandlerTests.Handle_ValidRequest_CreatedTitleFieldsMatchCommand` | System.NullReferenceException : Object reference not set to an instance of an object. |
| 145 | Titles | `Titles.UpdateTitleCommandHandlerTests.Handle_ValidRequest_ShouldReturn200WithTitle` | System.NullReferenceException : Object reference not set to an instance of an object. |
| 146 | Titles | `Titles.UpdateTitleCommandHandlerTests.Handle_ValidRequest_FieldsUpdatedOnEntity` | System.NullReferenceException : Object reference not set to an instance of an object. |
| 147 | TopikWriting | `TopikWriting.SolveQuestion51HandlerTests.Handle_HangfireThrowsException_ShouldReturn500` | Expected result.Message "Lỗi xử lý câu 51: Hangfire connection failed" to contain "Error processing sentence 51". |
| 148 | TopikWriting | `TopikWriting.SolveQuestion54HandlerTests.Handle_HangfireThrowsException_ShouldReturn500` | Expected result.Message "Lỗi xử lý câu 54: Hangfire connection failed" to contain "Error processing sentence 54". |
| 149 | TopikWriting | `TopikWriting.SolveQuestion53HandlerTests.Handle_HangfireThrowsException_ShouldReturn500` | Expected result.Message "Lỗi xử lý câu 53: Hangfire connection failed" to contain "Error processing sentence 53". |
| 150 | TopikWriting | `TopikWriting.SolveQuestion52HandlerTests.Handle_HangfireThrowsException_ShouldReturn500` | Expected result.Message "Lỗi xử lý câu 52: Hangfire connection failed" to contain "Error processing sentence 52". |
| 151 | UserExam | `UserExam.SubmitUserExamCommandHandlerTests.Handle_ValidSubmit_ShouldReturn200WithScore` | Expected result.Data!.FinalMcqScore to be 1, but found 0 (difference of -1). |
| 152 | UserExam | `UserExam.GetPracticeQuestionsQueryHandlerTests.Handle_TwoQuestionsNoPassage_ShouldReturnTwoGroups` | Expected result.Data! to contain 2 item(s), but found 1: {     Tokki.Application.UseCases.UserExam.DTOs.QuestionResultGroupDto     {         Questions = Tokki.Application.UseCases.UserExam.DTOs.Que... |
| 153 | Vocabulary | `Vocabulary.BulkCreateVocabulariesCommandHandlerTests.Handle_ExampleDuplicateInRequest_ShouldSkipDuplicateAndReturn201` | Expected result.Data.Results[0].Message "Tạo vocabulary thành công. Đã bỏ qua 1 câu ví dụ trùng lặp." to contain "skip". |
| 154 | Vocabulary | `Vocabulary.Commands.CreateVocabularyByStaff.CreateVocabularyByStaffCommandHandlerTests.Handle_ExampleFiltering_ShouldFilterCorrectly` | Expected result.Message "Tạo vocabulary thành công. Đang chờ phê duyệt. Đã bỏ qua 2 câu ví dụ trùng lặp." to contain "b? qua". |
| 155 | Vocabulary | `Vocabulary.Commands.BulkCreateVocabulariesCommandValidatorTests.Validate_DefinitionExceedLength_ShouldHaveError` | FluentValidation.TestHelper.ValidationTestException : Expected an error message of 'Chiều dài của 'Definition' phải lớn hơn hoặc bằng 0 ký tự và ít hơn hoặc bằng 500 ký tự. Bạn đã nhập 501 ký tự.'. Ac... |
| 156 | Vocabulary | `Vocabulary.CreateVocabularyByStaffCommandHandlerTests.Handle_ValidData_ShouldReturnDraftStatus201` | Expected result.Message "Tạo vocabulary thành công. Đang chờ phê duyệt." to contain "awaiting approval". |
| 157 | Vocabulary | `Vocabulary.BulkCreateVocabulariesByStaffCommandHandlerTests.Handle_ExampleDuplicateInRequest_ShouldSkipAndStillReturn201` | Expected result.Data.Results[0].Message "Tạo thành công. Bỏ qua 1 câu ví dụ trùng." to contain "Skip". |
| 158 | Vocabulary | `Vocabulary.CreateVocabularyCommandHandlerTests.Handle_DuplicateTextAndDefinition_ShouldReturn400` | Expected result.IsSuccess to be False, but found True. |
| 159 | Vocabulary | `Vocabulary.Commands.BulkCreateVocabulariesCommandValidatorTests.Validate_VocabulariesNull_ShouldHaveError` | System.NullReferenceException : Object reference not set to an instance of an object. |
| 160 | Vocabulary | `Vocabulary.Commands.CreateVocabularyCommandValidatorTests.Validate_LongDefinition_ShouldHaveError` | FluentValidation.TestHelper.ValidationTestException : Expected an error message of 'Definition không du?c vu?t quá 500 ký t?.'. Actual message was 'Definition không được vượt quá 500 ký tự.' |
| 161 | Vocabulary | `Vocabulary.Commands.BulkCreateVocabulariesByStaffCommandValidatorTests.Validate_TextEmpty_ShouldHaveError` | FluentValidation.TestHelper.ValidationTestException : Expected an error message of ''Text' không được bỏ trống.'. Actual message was ''Text' must not be empty.' |
| 162 | Vocabulary | `Vocabulary.Commands.BulkCreateVocabulariesCommandValidatorTests.Validate_VocabulariesEmpty_ShouldHaveError` | FluentValidation.TestHelper.ValidationTestException : Expected an error message of ''Danh sách vocabulary' không được bỏ trống.'. Actual message was ''Danh sách vocabulary' must not be empty.' |
| 163 | Vocabulary | `Vocabulary.Commands.CreateVocabularyCommandValidatorTests.Validate_EmptyText_ShouldHaveError` | FluentValidation.TestHelper.ValidationTestException : Expected an error message of 'Text không du?c d? tr?ng.'. Actual message was 'Text không được để trống.' |
| 164 | Vocabulary | `Vocabulary.Commands.BulkCreateVocabulariesCommandValidatorTests.Validate_TextEmpty_ShouldHaveError` | FluentValidation.TestHelper.ValidationTestException : Expected an error message of ''Text' không được bỏ trống.'. Actual message was ''Text' must not be empty.' |
| 165 | Vocabulary | `Vocabulary.BulkCreateVocabulariesCommandHandlerTests.Handle_HasDuplicateVocab_ShouldReturn400` | Expected result.Message to match (x.Contains("VOCABULARY_DUPLICATE") OrElse x.Contains("coincide")), but found "Không thể tạo vocabulary. Phát hiện 1 từ vựng bị trùng lặp (Text + Definition): 1. Từ '안... |
| 166 | Vocabulary | `Vocabulary.Commands.BulkCreateVocabulariesByStaffCommandValidatorTests.Validate_VocabulariesNull_ShouldHaveError` | System.NullReferenceException : Object reference not set to an instance of an object. |
| 167 | Vocabulary | `Vocabulary.Commands.CreateVocabularyCommandValidatorTests.Validate_DuplicateExamples_ShouldHaveError` | FluentValidation.TestHelper.ValidationTestException : Expected an error message of 'Danh sách câu ví d? b? trùng: hello'. Actual message was 'Danh sách câu ví dụ bị trùng: Hello' |
| 168 | VocabularyExample | `VocabularyExample.Commands.UpdateVocabularyExampleCommandValidatorTests.Validate_EmptyExampleId_ShouldHaveError` | FluentValidation.TestHelper.ValidationTestException : Expected an error message of ''ExampleId' không được bỏ trống.'. Actual message was ''ExampleId' must not be empty.' |
| 169 | VocabularyExample | `VocabularyExample.DeleteVocabularyExampleCommandHandlerTests.Handle_RepositoryThrows_ShouldReturn500` | System.Exception : DB update failed |
| 170 | VocabularyExample | `VocabularyExample.Commands.UpdateVocabularyExampleCommandValidatorTests.Validate_NullUpdateData_ShouldHaveError` | FluentValidation.TestHelper.ValidationTestException : Expected an error message of ''UpdateData' không được bỏ trống.'. Actual message was ''UpdateData' must not be empty.' |
| 171 | VocabularyExample | `VocabularyExample.Commands.UpdateVocabularyExampleCommandValidatorTests.Validate_InvalidStatus_ShouldHaveError` | FluentValidation.TestHelper.ValidationTestException : Expected a validation error for property UpdateData.Status ---- Properties with Validation Errors: [0]: UpdateData.Status.Value  |

### Failed Tests â€” Full Error Output
#### 1. `Accounts.ChangePasswordCommandHandlerTests.Handle_CorrectOldPassword_ShouldUpdateHashAndReturn200`

- **Module**: Accounts
- **Duration**: 00:00:00.6131430

```
Expected result.Data to be a match with the expectation, but it differs at index 0:
   ↓ (actual)
  "Đổi mật khẩu thành công!"
  "Ð?i m?t kh?u thành công!"
   ↑ (expected)
```
#### 2. `Accounts.Commands.FacebookLoginBranchCoverageTests.Handle_SocialLoginExists_AccountLocked_Returns403`

- **Module**: Accounts
- **Duration**: 00:00:00.0013168

```
Expected result.IsSuccess to be False, but found True.
```
#### 3. `Accounts.UpdateProfileCommandHandlerTests.Handle_UserNotFound_ShouldReturn404`

- **Module**: Accounts
- **Duration**: 00:00:00.0019668

```
Expected result.StatusCode to be 404, but found 400 (difference of -4).
```
#### 4. `Accounts.Commands.GoogleLoginCommandHandlerTests.CheckAccountStatus_Inactive_Returns403`

- **Module**: Accounts
- **Duration**: 00:00:00.0025623

```
Expected result.Errors.First().Code to be a match with the expectation, but it differs at index 8:
           ↓ (actual)
  "Account.AccountInActive"
  "Account.InActive"
           ↑ (expected)
```
#### 5. `Accounts.Commands.UpdateMyLevelCommandValidatorTests.Validate_InvalidEnumLevel_ShouldHaveError`

- **Module**: Accounts
- **Duration**: 00:00:00.0051862

```
FluentValidation.TestHelper.ValidationTestException : Expected an error message of '*không hợp lệ*'. Actual message was ''Cấp độ' không hợp lệ. Giá trị hợp lệ: 1 (Level1), 2 (Level2), 3 (Level3), 4 (L...
```
#### 6. `Accounts.AdminSoftDeleteAccountCommandHandlerTests.Handle_MissingAdminUserId_ShouldReturn401`

- **Module**: Accounts
- **Duration**: 00:00:00.0041104

```
Expected result.StatusCode to be 401, but found 400 (difference of -1).
```
#### 7. `Accounts.Commands.GoogleLoginCommandHandlerTests.CheckAccountStatus_Banned_Returns403`

- **Module**: Accounts
- **Duration**: 00:00:00.0024753

```
Expected result.Errors.First().Code to be a match with the expectation, but it differs at index 1:
    ↓ (actual)
  "Auth.AccountBanned"
  "Account.Banned"
    ↑ (expected)
```
#### 8. `Accounts.Commands.FacebookCompleteRegistrationBranchCoverageTests.Handle_AccountBanned_ShouldReturn403`

- **Module**: Accounts
- **Duration**: 00:00:00.0017805

```
Expected result.StatusCode to be 403, but found 409 (difference of 6).
```
#### 9. `Accounts.AdminUpdateUserCommandHandlerTests.Handle_TargetUserNotFound_ShouldReturn404`

- **Module**: Accounts
- **Duration**: 00:00:00.0027335

```
Expected result.StatusCode to be 404, but found 400 (difference of -4).
```
#### 10. `Accounts.UpdateProfileCommandHandlerTests.Handle_NoUserId_ShouldReturn401`

- **Module**: Accounts
- **Duration**: 00:00:00.0011865

```
Expected result.StatusCode to be 401, but found 400 (difference of -1).
```
#### 11. `Accounts.Commands.UpdateMyLevelCommandHandlerTests.Handle_LockedUser_ShouldReturn403`

- **Module**: Accounts
- **Duration**: 00:00:00.0008951

```
Expected result.IsSuccess to be False, but found True.
```
#### 12. `Accounts.Commands.DeleteAccountCommandHandlerTests.Handle_UserNotFound_ShouldReturnFailureWithUserNotFound`

- **Module**: Accounts
- **Duration**: 00:00:00.0009357

```
Expected result.Errors.First().Code to be a match with the expectation, but it differs at index 13:
              ↓ (actual)
  "…r.NotFoundById"
  "…r.NotFound.Id"
              ↑ (expected)
```
#### 13. `Accounts.Commands.GoogleLoginCommandHandlerTests.CheckAccountStatus_Locked_Returns403`

- **Module**: Accounts
- **Duration**: 00:00:00.0051475

```
Expected result.Errors.First().Code to be a match with the expectation, but it differs at index 1:
    ↓ (actual)
  "Auth.AccountLocked"
  "Account.Locked"
    ↑ (expected)
```
#### 14. `Accounts.Commands.FacebookLoginCommandHandlerTests.Handle_ExistingBannedAccount_ShouldReturn403`

- **Module**: Accounts
- **Duration**: 00:00:00.0020133

```
Expected resultMerged.StatusCode to be 403, but found 401 (difference of -2).
```
#### 15. `Accounts.ChangePasswordCommandHandlerTests.Handle_UserNotFound_ShouldReturn404`

- **Module**: Accounts
- **Duration**: 00:00:00.0030197

```
Expected result.StatusCode to be 404, but found 400 (difference of -4).
```
#### 16. `Accounts.Commands.UpdateMyLevelCommandHandlerTests.Handle_InactiveUser_ShouldReturn403`

- **Module**: Accounts
- **Duration**: 00:00:00.0009019

```
Expected result.Errors.First().Code to be a match with the expectation, but it differs at index 8:
           ↓ (actual)
  "Account.AccountInActive"
  "Account.InActive"
           ↑ (expected)
```
#### 17. `Accounts.ResetPasswordCommandHandlerTests.Handle_UserNotFound_ShouldReturn404`

- **Module**: Accounts
- **Duration**: 00:00:00.0013832

```
Expected result.StatusCode to be 404, but found 400 (difference of -4).
```
#### 18. `Accounts.AdminSoftDeleteAccountCommandHandlerTests.Handle_TargetUserNotFound_ShouldReturn404`

- **Module**: Accounts
- **Duration**: 00:00:00.0026227

```
Expected result.StatusCode to be 404, but found 400 (difference of -4).
```
#### 19. `Accounts.Commands.FacebookLoginBranchCoverageTests.Handle_EmailExists_AccountLocked_Returns403`

- **Module**: Accounts
- **Duration**: 00:00:00.0086047

```
Expected result.StatusCode to be 403, but found 409 (difference of 6).
```
#### 20. `Accounts.Commands.UpdateMyLevelCommandHandlerTests.Handle_BannedUser_ShouldReturn403`

- **Module**: Accounts
- **Duration**: 00:00:00.0009515

```
Expected result.Errors.First().Code to be a match with the expectation, but it differs at index 1:
    ↓ (actual)
  "Auth.AccountBanned"
  "Account.Banned"
    ↑ (expected)
```
#### 21. `Accounts.Commands.FacebookCompleteRegistrationBranchCoverageTests.Handle_FacebookApiThrowsException_ShouldReturn500`

- **Module**: Accounts
- **Duration**: 00:00:00.0020328

```
Expected result.StatusCode to be 500, but found 401 (difference of -99).
```
#### 22. `Blogs.Queries.GetPagedBlogsQueryHandlerTests.Handle_ProperTagsData_ShouldMapListStrictly`

- **Module**: Blogs
- **Duration**: 00:00:00.0016405

```
System.ArgumentNullException : Value cannot be null. (Parameter 'key')
```
#### 23. `Blogs.ApproveBlogCommandHandlerTests.Handle_Unauthorized_ShouldReturn401`

- **Module**: Blogs
- **Duration**: 00:00:00.0085463

```
Expected result.StatusCode to be 401, but found 400 (difference of -1).
```
#### 24. `Blogs.SubmitBlogForApprovalCommandHandlerTests.Handle_Unauthorized_ShouldReturn401`

- **Module**: Blogs
- **Duration**: 00:00:00.0030320

```
Expected result.StatusCode to be 401, but found 400 (difference of -1).
```
#### 25. `Blogs.GetBlogByIdQueryHandlerTests.Handle_WithTags_ShouldMapTagsCorrectly`

- **Module**: Blogs
- **Duration**: 00:00:00.0038056

```
Expected result.IsSuccess to be True, but found False.
```
#### 26. `Blogs.UpdateBlogCommandHandlerTests.Handle_DatabaseException_ShouldReturn500`

- **Module**: Blogs
- **Duration**: 00:00:00.0038208

```
Expected result.Message "Lỗi hệ thống: Mock Update Failure" to contain "L?i h? th?ng: Mock Update Failure".
```
#### 27. `Blogs.GetBlogByIdQueryHandlerTests.Handle_ValidRequest_WithAuthorInfo_ShouldReturn200`

- **Module**: Blogs
- **Duration**: 00:00:00.2711088

```
Expected result.IsSuccess to be True, but found False.
```
#### 28. `Blogs.GetBlogByIdQueryHandlerTests.Handle_ValidRequest_MissingAuthor_ShouldReturnAnonymous`

- **Module**: Blogs
- **Duration**: 00:00:00.0022512

```
Expected result.IsSuccess to be True, but found False.
```
#### 29. `Blogs.Queries.GetPagedBlogsQueryHandlerTests.Handle_MissingCategoryName_ShouldMapToDefaultString`

- **Module**: Blogs
- **Duration**: 00:00:00.0019105

```
System.ArgumentNullException : Value cannot be null. (Parameter 'key')
```
#### 30. `Blogs.CreateBlogCommandHandlerTests.Handle_DatabaseThrowsException_ShouldReturn500`

- **Module**: Blogs
- **Duration**: 00:00:00.0040345

```
Expected result.Message "Không thể thêm Bài viết. Vui lòng thử lại." to contain "Lỗi SQL chi tiết: Mock DB Failure".
```
#### 31. `Blogs.GetBlogByIdQueryHandlerTests.Handle_WithoutCategoryInclude_ShouldReturnNA`

- **Module**: Blogs
- **Duration**: 00:00:00.0162065

```
Expected result.IsSuccess to be True, but found False.
```
#### 32. `Blogs.RejectBlogCommandHandlerTests.Handle_Unauthorized_ShouldReturn401`

- **Module**: Blogs
- **Duration**: 00:00:00.0040128

```
Expected result.StatusCode to be 401, but found 400 (difference of -1).
```
#### 33. `Categories.Queries.GetCategoryByIdQueryHandlerTests.Handle_CategoryNotFoundMessage_ShouldReadCorrectly`

- **Module**: Categories
- **Duration**: 00:00:00.0019681

```
Expected result.Message "Không thể lấy thông tin Danh mục. Vui lòng thử lại sau." to contain "th?t b?i".
```
#### 34. `Categories.Commands.UpdateCategoryCommandHandlerTests.Handle_ValidUpdate_ShouldRecalculateSlug`

- **Module**: Categories
- **Duration**: 00:00:00.0027240

```
Expected category.Slug to be a match with the expectation, but it differs at index 3:
      ↓ (actual)
  "ngh-thut-mi-CAT-001"
  "nghe-thuat-moi-CAT-001"
      ↑ (expected)
```
#### 35. `Cloudinary.Commands.UploadImageCommandValidatorTests.Validate_NullContentType_ShouldHaveError`

- **Module**: Cloudinary
- **Duration**: 00:00:00.0005155

```
System.NullReferenceException : Object reference not set to an instance of an object.
```
#### 36. `Cloudinary.UploadVocabularyImageByUrlCommandHandlerTests.Handle_BothUploadPathsFail_ErrorMessageContainsFallback`

- **Module**: Cloudinary
- **Duration**: 00:00:00.1716798

```
Expected result.Message "HTTP Error khi tải ảnh từ nguồn: NotFound - Response status code does not indicate success: 404 (Not Found)." to contain "fallback".
```
#### 37. `Cloudinary.Commands.UploadImageCommandValidatorTests.Validate_NullFile_ShouldHaveError`

- **Module**: Cloudinary
- **Duration**: 00:00:00.0003624

```
System.NullReferenceException : Object reference not set to an instance of an object.
```
#### 38. `Comments.CreateCommentCommandHandlerTests.Handle_RepositoryThrows_ShouldReturn500`

- **Module**: Comments
- **Duration**: 00:00:00.0028932

```
Expected result.IsSuccess to be False, but found True.
```
#### 39. `Comments.GetCommentsQueryHandlerTests.Handle_DeletedComment_ShouldShowMaskedContent`

- **Module**: Comments
- **Duration**: 00:00:00.0062138

```
Expected result.Data.First().Content "Bình luận này đã bị xóa." to contain "dã b? xóa".
```
#### 40. `Comments.CreateCommentCommandHandlerTests.Handle_BlogNotFound_ShouldReturn404`

- **Module**: Comments
- **Duration**: 00:00:00.0029305

```
Expected result.IsSuccess to be False, but found True.
```
#### 41. `EmailTemplates.Commands.UpdateEmailCampaignCommandValidatorTests.Validate_InvalidStatusUpdate_ShouldHaveError`

- **Module**: EmailTemplates
- **Duration**: 00:00:00.0015031

```
FluentValidation.TestHelper.ValidationTestException : Expected a validation error for property Status
----
Properties with Validation Errors:
[0]: Status.Value

```
#### 42. `EmailTemplates.Commands.UpdateEmailCampaignCommandHandlerTests.Handle_JobNotFound_ShouldReturn404`

- **Module**: EmailTemplates
- **Duration**: 00:00:00.0011608

```
Expected result.Message to be a match with the expectation, but it differs at index 12:
          ↓ (actual)
  "…tìm thấy campaign!"
  "…tìm th?y campaign!"
          ↑ (expected)
```
#### 43. `EmailTemplates.Commands.UpdateEmailCampaignCommandValidatorTests.Validate_NoneTargetWithoutEmails_ShouldHaveError`

- **Module**: EmailTemplates
- **Duration**: 00:00:00.0141796

```
FluentValidation.TestHelper.ValidationTestException : Expected a validation error for property 
----
Properties with Validation Errors:
[0]: Người nhận

```
#### 44. `Exam.Queries.GetQuestionsByPartQueryHandlerTests.Handle_SkillListening_ShouldMapToAudioMediaType`

- **Module**: Exam
- **Duration**: 00:00:00.0019044

```
System.InvalidOperationException : Sequence contains no elements
```
#### 45. `Exam.CreateExamCommandHandlerTests.Handle_ValidData_ShouldCreateExamSuccessfully`

- **Module**: Exam
- **Duration**: 00:00:00.0260470

```
Expected result.IsSuccess to be True, but found False.
```
#### 46. `Exam.Queries.GetQuestionsByPartQueryHandlerTests.Handle_SkillWriting_ShouldFallbackToImageMediaType`

- **Module**: Exam
- **Duration**: 00:00:00.0007135

```
System.InvalidOperationException : Sequence contains no elements
```
#### 47. `Exam.Queries.GetQuestionsByPartQueryHandlerTests.Handle_SkillIsNull_ShouldMapToImageDefault`

- **Module**: Exam
- **Duration**: 00:00:00.0012124

```
System.InvalidOperationException : Sequence contains no elements
```
#### 48. `Exam.CreateExamCommandHandlerTests.Handle_IncompleteSkillDurations_ShouldReturn400`

- **Module**: Exam
- **Duration**: 00:00:00.0045018

```
Expected result.Message "Vui lòng nhập thời gian cho phần 'Reading'." to contain "Please enter a valid test time".
```
#### 49. `Exam.Queries.GetQuestionsByPartQueryHandlerTests.Handle_PassageNotNull_MapsCompletePassageSafely`

- **Module**: Exam
- **Duration**: 00:00:00.0022532

```
System.InvalidOperationException : Sequence contains no elements
```
#### 50. `Exam.Queries.GetQuestionsByPartQueryHandlerTests.Handle_PassagesNull_MapsToEmptyWithoutError`

- **Module**: Exam
- **Duration**: 00:00:00.0016835

```
System.InvalidOperationException : Sequence contains no elements
```
#### 51. `Exam.CreateExamCommandHandlerTests.Handle_NotEnoughQuestionsInBank_ShouldReturn400`

- **Module**: Exam
- **Duration**: 00:00:00.0025515

```
Expected result.Message "Vui lòng nhập thời gian cho phần '0'." to contain "insufficient".
```
#### 52. `Exam.Queries.GetQuestionsByPartQueryHandlerTests.Handle_OptionsMapping_ShouldStrictlyOrderAscending`

- **Module**: Exam
- **Duration**: 00:00:00.0008663

```
System.InvalidOperationException : Sequence contains no elements
```
#### 53. `Exam.AddQuestionToExamCommandHandlerTests.Handle_ExamSlotNotFound_ShouldReturn404`

- **Module**: Exam
- **Duration**: 00:00:00.0038820

```
Expected result.Message "Không tìm thấy câu hỏi số 99 trong đề thi này để thay thế." to contain "Question number 99 not found".
```
#### 54. `ExamTemplates.Commands.DuplicateExamTemplateCommandHandlerTests.Handle_TemplateNotFound_ReturnsFailure404`

- **Module**: ExamTemplates
- **Duration**: 00:00:00.0022854

```
Expected result.Message "Mẫu đề thi không tồn tại." to contain "M?u d? thi không t?n t?i".
```
#### 55. `ExamTemplates.Commands.DuplicateExamTemplateCommandHandlerTests.Handle_Exception_Returns500ServerError`

- **Module**: ExamTemplates
- **Duration**: 00:00:00.0010534

```
System.Exception : Database error
```
#### 56. `ExamTemplates.DuplicateExamTemplateCommandHandlerTests.Handle_TemplateHasParts_ShouldDuplicateParts`

- **Module**: ExamTemplates
- **Duration**: 00:00:00.0017628

```
System.ArgumentException : Invalid callback. Setup on method with parameters (IEnumerable<TemplatePart>) cannot invoke callback with parameters (List<TemplatePart>).
```
#### 57. `Excel.ExportAccountsQueryHandlerTests.Handle_AccountsEmpty_ShouldExportEmptyFile`

- **Module**: Excel
- **Duration**: 00:00:00.0215618

```
Expected result.Data.FileName "Tokki_Account_26042026.xlsx" to contain "Accounts_Export_".
```
#### 58. `Excel.ExportVocabByTopicQueryHandlerTests.Handle_TopicNotFound_ShouldReturn404`

- **Module**: Excel
- **Duration**: 00:00:00.0019944

```
Expected result.StatusCode to be 404, but found 400 (difference of -4).
```
#### 59. `Excel.Commands.ImportPronunciationRulesCommandHandlerTests.Handle_DatabaseException_ReturnsFailure`

- **Module**: Excel
- **Duration**: 00:00:00.0016525

```
Expected result.Message to be a match with the expectation, but it differs at index 0:
   ↓ (actual)
  "Lỗi lưu dữ liệu vào database."
  "DATABASE_ERROR"
   ↑ (expected)
```
#### 60. `Excel.ExportVocabByTopicQueryHandlerTests.Handle_VocabListNull_ShouldReturnVocabTopicIsEmpty`

- **Module**: Excel
- **Duration**: 00:00:00.0064178

```
Expected result.StatusCode to be 404, but found 400 (difference of -4).
```
#### 61. `Excel.ImportQuestionTypesCommandHandlerTests.Handle_DuplicateCodeInFile_ShouldFailSecondRow`

- **Module**: Excel
- **Duration**: 00:00:00.0119739

```
Expected result.Data!.FailureList to contain 1 item(s), but found 2: {
    Tokki.Application.UseCases.Excel.DTOs.QuestionTypePreviewDTO
    {
        Code = "DUP_CODE",
        Name = "Reading 1",...
```
#### 62. `Excel.ExportVocabByTopicQueryHandlerTests.Handle_ValidTopicWithVocabs_ShouldExportFile`

- **Module**: Excel
- **Duration**: 00:00:00.0028145

```
Expected result.Data.FileName to be a match with the expectation, but it differs at index 0:
   ↓ (actual)
  "Tokki_Vocab_Korean Basics_26042026.xlsx"
  "Korean Basics.xlsx"
   ↑ (expected)
```
#### 63. `Excel.ExportVocabByTopicQueryHandlerTests.Handle_VocabListEmpty_ShouldReturnVocabTopicIsEmpty`

- **Module**: Excel
- **Duration**: 00:00:00.0018495

```
Expected result.StatusCode to be 404, but found 400 (difference of -4).
```
#### 64. `Excel.Commands.ImportPronunciationRulesCommandHandlerTests.Handle_ExcelNull_ShouldReturnError`

- **Module**: Excel
- **Duration**: 00:00:00.0024642

```
Expected result.Message "Không tìm thấy dữ liệu hợp lệ trong file Excel." to contain "không tìm thấy dữ liệu hợp lệ".
```
#### 65. `Excel.ExportQuestionTypesQueryHandlerTests.Handle_ValidData_FileNameHasCorrectPrefix`

- **Module**: Excel
- **Duration**: 00:00:00.0029930

```
Expected result.Data.FileName to start with "QuestionTypes_", but "Tokki_QuestionType_26042026.xlsx" differs near "Tok" (index 0).
```
#### 66. `Excel.ExportVocabByTopicQueryHandlerTests.Handle_LongTopicName_ShouldTruncateSheetName`

- **Module**: Excel
- **Duration**: 00:00:00.0066826

```
Expected result.Data.FileName to be a match with the expectation, but it differs at index 1:
    ↓ (actual)
  "Tokki_Vocab_This Is A Very Long Topic Name That Exceeds Thirty Characters_260420…"
  "...
```
#### 67. `Excel.Commands.ImportPronunciationRulesCommandHandlerTests.Handle_ExcelEmpty_ShouldReturnError`

- **Module**: Excel
- **Duration**: 00:00:00.0013476

```
Expected result.Message to be a match with the expectation, but it differs at index 0:
   ↓ (actual)
  "Không tìm thấy dữ liệu hợp lệ trong file Excel."
  "EXCEL_EMPTY"
   ↑ (expected)
```
#### 68. `Excel.ExportQuestionTypesQueryHandlerTests.Handle_ValidData_ShouldMapAndExportCorrectly`

- **Module**: Excel
- **Duration**: 00:00:00.0026964

```
Expected result.Data.FileName "Tokki_QuestionType_26042026.xlsx" to contain "QuestionTypes_".
```
#### 69. `Excel.ImportPronunciationExampleCommandHandlerTests.Handle_AddRangeThrows_ShouldReturnDatabaseError`

- **Module**: Excel
- **Duration**: 00:00:00.0024615

```
Expected result.StatusCode to be 500, but found 400 (difference of -100).
```
#### 70. `Excel.ExportAccountsQueryHandlerTests.Handle_ValidData_ShouldReturnWrappedFileBytes`

- **Module**: Excel
- **Duration**: 00:00:00.0032726

```
Expected result.Data.FileName "Tokki_Account_26042026.xlsx" to contain "Accounts_Export_".
```
#### 71. `Excel.ImportQuestionTypesCommandHandlerTests.Handle_DatabaseThrowsException_ShouldReturnDbErrorFailure`

- **Module**: Excel
- **Duration**: 00:00:00.0031328

```
Expected result.IsSuccess to be False, but found True.
```
#### 72. `Excel.ImportQuestionTypesCommandHandlerTests.Handle_ValidRow_ShouldCreateEntityAndInsert`

- **Module**: Excel
- **Duration**: 00:00:00.0015357

```
Expected result.Data!.SuccessList to contain 1 item(s), but found 0: {empty}.
```
#### 73. `Gamification.Commands.AddGameXpCommandHandlerTests.Handle_EmptyUserId_ShouldFail`

- **Module**: Gamification
- **Duration**: 00:00:00.0016334

```
Expected result.Message "UserId không hợp lệ." to contain "không h?p l? =".
```
#### 74. `Gamification.Commands.AddGameXpCommandHandlerTests.Handle_GainsLevel_SetsIsLevelUp`

- **Module**: Gamification
- **Duration**: 00:00:00.0014656

```
System.ArgumentNullException : Value cannot be null. (Parameter 'source')
```
#### 75. `Gamification.Commands.AddGameXpCommandHandlerTests.Handle_EmptyConfig_UsesDefaultLimit`

- **Module**: Gamification
- **Duration**: 00:00:00.0018312

```
System.ArgumentNullException : Value cannot be null. (Parameter 'source')
```
#### 76. `Gamification.Commands.AddGameXpCommandHandlerTests.Handle_DailyLimitReached_AmountBecomesZero`

- **Module**: Gamification
- **Duration**: 00:00:00.0019223

```
System.ArgumentNullException : Value cannot be null. (Parameter 'source')
```
#### 77. `Gamification.Commands.AddGameXpCommandHandlerTests.Handle_DailyLimitPartial_AmountTrimmed`

- **Module**: Gamification
- **Duration**: 00:00:00.0082257

```
System.ArgumentNullException : Value cannot be null. (Parameter 'source')
```
#### 78. `Gamification.Commands.AddGameXpCommandHandlerTests.Handle_UserNotFound_ShouldFail`

- **Module**: Gamification
- **Duration**: 00:00:00.0021514

```
Expected result.Message "Không tìm thấy user với id: uid" to contain "Không tìm th?y user".
```
#### 79. `Gamification.Commands.AddGameXpCommandHandlerTests.Handle_NonMiniGameSource_AddsFullAmount`

- **Module**: Gamification
- **Duration**: 00:00:00.0024529

```
Expected result.Message "Cộng thành công 50 XP." to contain "C?ng thành công 50 XP".
```
#### 80. `LiveChat.JoinSupportChatCommandHandlerTests.Handle_ValidRequestMissingUserInfo_ShouldUseDefaultFallback`

- **Module**: LiveChat
- **Duration**: 00:00:00.0073294

```
Moq.MockException : 
Expected invocation on the mock once, but was 0 times: x => x.SendMessageToRoomAsync("R1", It.Is<ChatMessage>(m => m.Content.Contains("Nhân viên h? tr?")))

Performed invocatio...
```
#### 81. `MiniGame.Queries.Wordle.GetWordleResultHandlerTests.Handle_GameNull_ShouldReturnFailure`

- **Module**: MiniGame
- **Duration**: 00:00:00.0030807

```
Expected result.Message "Không tìm thấy thông tin trò chơi." to contain "Không tìm th?y thông tin trò choi".
```
#### 82. `MiniGame.SubmitWordleGuessHandlerTests.Handle_GameAlreadyOver_ShouldReturnFailure`

- **Module**: MiniGame
- **Duration**: 00:00:00.0046382

```
Expected result.Message "Lượt chơi hôm nay của bạn cho từ này đã kết thúc." to contain "end".
```
#### 83. `MiniGame.Queries.Wordle.GetWordleResultHandlerTests.Handle_ProgressNull_ShouldReturnFailure`

- **Module**: MiniGame
- **Duration**: 00:00:00.0023490

```
Expected result.Message "Bạn cần hoàn thành và chiến thắng trò chơi để xem kết quả này." to contain "B?n c?n hoàn thành".
```
#### 84. `MiniGame.Queries.Wordle.GetWordleResultHandlerTests.Handle_ProgressIsWonFalse_ShouldReturnFailure`

- **Module**: MiniGame
- **Duration**: 00:00:00.0020155

```
Expected result.Message "Bạn cần hoàn thành và chiến thắng trò chơi để xem kết quả này." to contain "chi?n th?ng trò choi".
```
#### 85. `MiniGame.SubmitWordleSentenceCommandHandlerTests.Handle_AiScoreExactlyHalfPoint_ShouldRoundUp_MightFail`

- **Module**: MiniGame
- **Duration**: 00:00:00.0039014

```
Expected capturedSubmission!.AiScore to be 85 because ⚠️ WILL FAIL: Math.Round(84.5) = 84 due to Banker's Rounding, not 85, but found 84 (difference of -1).
```
#### 86. `Tokki.UnitTest.Application.Common.Helpers.LevelEngineTests.GetLevel_HighXp_ShouldCalculateCurveProperly`

- **Module**: Other
- **Duration**: 00:00:00.0006646

```
Expected level to be 5, but found 4.
```
#### 87. `Tokki.UnitTest.Application.Common.Helpers.HangulAndWordleHelperBranchTests.CalculateFeedback_CompletelyWrong_ShouldBeAllGray`

- **Module**: Other
- **Duration**: 00:00:00.0018954

```
Expected result to contain only items matching (fb.BlockColor == "Gray"), but {
    Tokki.Application.UseCases.MiniGame.DTOs.BlockFeedback
    {
        BlockColor = "Yellow",
        Character = ...
```
#### 88. `Tokki.UnitTest.Application.Common.Helpers.EnumExtensionsTests.GetDescription_UndefinedEnumValue_ReturnsIntString`

- **Module**: Other
- **Duration**: 00:00:00.0002524

```
System.ArgumentNullException : Value cannot be null. (Parameter 'element')
```
#### 89. `Tokki.UnitTest.Application.Common.Helpers.WordleHelperTests.CalculateFeedback_NoMatch_ReturnsGray`

- **Module**: Other
- **Duration**: 00:00:00.0007450

```
Expected result[0].BlockColor to be "Gray" with a length of 4, but "Yellow" has a length of 6, differs near "Yel" (index 0).
```
#### 90. `Otps.VerifyForgotPasswordOtpCommandHandlerTests.Handle_CorrectOtp_ShouldReturnResetToken`

- **Module**: Otps
- **Duration**: 00:00:00.0025481

```
Expected result.Data to be a match with the expectation, but it differs at index 12:
              ↓ (actual)
  "…SET-TOKEN-XYZ"
  "…SET-TOKEN-ABC"
              ↑ (expected)
```
#### 91. `Passages.Commands.UpdatePassageCommandHandlerTests.Handle_TextMissingContent_ShouldReturn400`

- **Module**: Passages
- **Duration**: 00:00:00.0116527

```
Expected result.Message "Loại Văn bản bắt buộc phải có nội dung." to contain "b?t bu?c ph?i có n?i dung".
```
#### 92. `Passages.Commands.UpdatePassageCommandHandlerTests.Handle_TitleEmpty_ShouldReturn400`

- **Module**: Passages
- **Duration**: 00:00:00.0022623

```
Expected result2.Message "Tiêu đề không được để trống." to contain "Tiêu d? không du?c d? tr?ng".
```
#### 93. `Passages.CreatePassageCommandHandlerTests.Handle_ValidTextPassage_ShouldReturn201WithId`

- **Module**: Passages
- **Duration**: 00:00:00.0031958

```
Expected result.Data to be "PASS-NEW", but "PASS-001" differs near "001" (index 5).
```
#### 94. `Payments.Queries.PaymentHistoryDtoTests.StatusDisplay_Failed_ShouldFormatStringFallback`

- **Module**: Payments
- **Duration**: 00:00:00.0011899

```
Expected dto.StatusDisplay to be a match with the expectation, but it differs at index 0:
   ↓ (actual)
  "Đang chờ thanh toán"
  "Thất bại"
   ↑ (expected)
```
#### 95. `Payments.Commands.ProcessWebhookCommandHandlerTests.Handle_InsufficientAmount_ShouldReturnMessage`

- **Module**: Payments
- **Duration**: 00:00:00.0020678

```
Expected result.Data "Số tiền chuyển (50,000) thấp hơn giá trị gói (100,000)." to contain "không d? so v?i hóa don".
```
#### 96. `PronunciationExample.Commands.CreatePronunciationExampleCommandHandlerTests.Handle_RuleNotFound_ShouldReturn404`

- **Module**: PronunciationExample
- **Duration**: 00:00:00.0030171

```
Expected result.Message to be a match with the expectation, but it differs at index 0:
   ↓ (actual)
  "Quy tắc phát âm không tồn tại."
  "Rule.NotFound"
   ↑ (expected)
```
#### 97. `PronunciationExample.Queries.GetPagedPronunciationExamplesQueryHandlerTests.Handle_EdgeMapping1`

- **Module**: PronunciationExample
- **Duration**: 00:00:00.0012044

```
System.ArgumentNullException : Value cannot be null. (Parameter 'source')
```
#### 98. `PronunciationExample.Queries.GetPagedPronunciationExamplesQueryHandlerTests.Handle_EdgeMapping2`

- **Module**: PronunciationExample
- **Duration**: 00:00:00.0008871

```
System.ArgumentNullException : Value cannot be null. (Parameter 'source')
```
#### 99. `PronunciationExample.Queries.GetPagedPronunciationExamplesQueryHandlerTests.Handle_EdgeMapping3`

- **Module**: PronunciationExample
- **Duration**: 00:00:00.0057498

```
System.ArgumentNullException : Value cannot be null. (Parameter 'source')
```
#### 100. `QuestionBanks.Commands.QuestionOptions.UpdateQuestionOptionCommandHandlerTests.Handle_DuplicateKeyOption_ShouldReturn400`

- **Module**: QuestionBanks
- **Duration**: 00:00:00.0018065

```
Expected result.Errors.First().Description "Dữ liệu đầu vào không hợp lệ." to contain "dã t?n t?i".
```
#### 101. `QuestionBanks.Commands.RejectQuestionBanksCommandValidatorTests.Validate_DuplicateIds_ShouldHaveError`

- **Module**: QuestionBanks
- **Duration**: 00:00:00.0006277

```
FluentValidation.TestHelper.ValidationTestException : Expected an error message of 'Danh sách mã câu h?i b? trùng.'. Actual message was 'Danh sách mã câu hỏi bị trùng.'
```
#### 102. `QuestionBanks.Commands.CreateQuestionBankByStaffCommandValidatorTests.ValidateAsync_NoContentNoImageInOption_ShouldHaveError`

- **Module**: QuestionBanks
- **Duration**: 00:00:00.0011435

```
FluentValidation.TestHelper.ValidationTestException : Expected an error message of 'Ðáp án ph?i có n?i dung text ho?c ?nh.'. Actual message was 'Đáp án phải có nội dung text hoặc ảnh.'
```
#### 103. `QuestionBanks.Commands.ApproveQuestionBanksCommandValidatorTests.Validate_NullIds_HasValidationError`

- **Module**: QuestionBanks
- **Duration**: 00:00:00.0006459

```
System.ArgumentNullException : Value cannot be null. (Parameter 'source')
```
#### 104. `QuestionBanks.Commands.ApproveQuestionBanksCommandValidatorTests.Validate_WhitespaceIds_HasValidationError`

- **Module**: QuestionBanks
- **Duration**: 00:00:00.0005086

```
FluentValidation.TestHelper.ValidationTestException : Expected an error message of 'Danh sách mã câu h?i không du?c r?ng.'. Actual message was 'Danh sách mã câu hỏi không được rỗng.'
```
#### 105. `QuestionBanks.Commands.RejectQuestionBank.RejectQuestionBankCommandValidatorTests.Validate_NullQuestionBankIds_ShouldHaveError`

- **Module**: QuestionBanks
- **Duration**: 00:00:00.0004549

```
System.ArgumentNullException : Value cannot be null. (Parameter 'source')
```
#### 106. `QuestionBanks.Commands.ApproveQuestionBanksCommandHandlerTests.Handle_AlreadyDeleted_ShouldReturn400`

- **Module**: QuestionBanks
- **Duration**: 00:00:00.0027191

```
Expected result.Message "QuestionBankId q1 đã bị xóa, không thể duyệt." to contain "dã b? xóa".
```
#### 107. `QuestionBanks.Commands.RejectQuestionBank.RejectQuestionBankCommandHandlerTests.Handle_DeletedStatus_ShouldReturn400`

- **Module**: QuestionBanks
- **Duration**: 00:00:00.0023420

```
Expected result.Errors[0].Description "Dữ liệu đầu vào không hợp lệ." to contain "đã bị xóa".
```
#### 108. `QuestionBanks.Commands.RejectQuestionBanksCommandValidatorTests.Validate_QuestionBankIdsNull_ShouldHaveError`

- **Module**: QuestionBanks
- **Duration**: 00:00:00.0003991

```
System.ArgumentNullException : Value cannot be null. (Parameter 'source')
```
#### 109. `QuestionBanks.Commands.CreateQuestionBankCommandHandlerTests.Handle_TypeInactive_ShouldReturn400`

- **Module**: QuestionBanks
- **Duration**: 00:00:00.0025209

```
Expected result.Message "Loại câu hỏi đang bị vô hiệu hóa." to contain "vô hi?u hóa".
```
#### 110. `QuestionBanks.Commands.QuestionOptions.CreateQuestionOptionCommandHandlerTests.Handle_DuplicateKeyOption_ShouldReturn400`

- **Module**: QuestionBanks
- **Duration**: 00:00:00.0035673

```
Expected result.Errors.First().Description "Dữ liệu đầu vào không hợp lệ." to contain "đã tồn tại".
```
#### 111. `QuestionBanks.Commands.RejectQuestionBanksCommandValidatorTests.Validate_QuestionBankIdsEmptyStringsOnly_ShouldHaveError`

- **Module**: QuestionBanks
- **Duration**: 00:00:00.0004980

```
FluentValidation.TestHelper.ValidationTestException : Expected an error message of 'Danh sách mã câu h?i không du?c r?ng.'. Actual message was 'Danh sách mã câu hỏi không được rỗng.'
```
#### 112. `QuestionBanks.Commands.CreateQuestionBankByStaffCommandHandlerTests.Handle_ReadingMissingContent_ShouldReturn400`

- **Module**: QuestionBanks
- **Duration**: 00:00:00.0019729

```
Expected result.Message "Câu hỏi Reading bắt buộc phải có Content." to contain "b?t bu?c ph?i có Content".
```
#### 113. `QuestionBanks.Commands.ApproveQuestionBanksCommandValidatorTests.Validate_DuplicateWhitespaceIds_HasValidationError`

- **Module**: QuestionBanks
- **Duration**: 00:00:00.0004012

```
FluentValidation.TestHelper.ValidationTestException : Expected an error message of 'Danh sách mã câu h?i b? trùng.'. Actual message was 'Danh sách mã câu hỏi bị trùng.'
```
#### 114. `QuestionBanks.Commands.QuestionOptions.UpdateQuestionOptionCommandHandlerTests.Handle_RemoveSingleCorrect_ShouldReturn400`

- **Module**: QuestionBanks
- **Duration**: 00:00:00.0025440

```
Expected result.Errors.First().Description "Dữ liệu đầu vào không hợp lệ." to contain "ít nh?t m?t dáp án dúng".
```
#### 115. `QuestionBanks.Commands.RejectQuestionBanksCommandValidatorTests.Validate_EmptyRejectReason_ShouldHaveError`

- **Module**: QuestionBanks
- **Duration**: 00:00:00.0005089

```
FluentValidation.TestHelper.ValidationTestException : Expected an error message of ''Lý do t? ch?i' không du?c b? tr?ng.'. Actual message was ''Lý do từ chối' must not be empty.'
```
#### 116. `QuestionBanks.Commands.CreateQuestionBankByStaffCommandHandlerTests.Handle_TypeInactive_ShouldReturn400`

- **Module**: QuestionBanks
- **Duration**: 00:00:00.0018339

```
Expected result.Message "Loại câu hỏi đang bị vô hiệu hóa." to contain "vô hi?u hóa".
```
#### 117. `QuestionBanks.Commands.ApproveQuestionBanksCommandHandlerTests.Handle_NotPendingApproval_ShouldReturnValidationFailed`

- **Module**: QuestionBanks
- **Duration**: 00:00:00.0012871

```
Expected result.Message "QuestionBankId q1 không ở trạng thái PendingApproval." to contain "không ? tr?ng thái PendingApproval".
```
#### 118. `QuestionBanks.Commands.ApproveQuestionBanksCommandValidatorTests.Validate_DuplicateTargetIds_HasValidationError`

- **Module**: QuestionBanks
- **Duration**: 00:00:00.0005967

```
FluentValidation.TestHelper.ValidationTestException : Expected an error message of 'Danh sách mã câu h?i b? trùng.'. Actual message was 'Danh sách mã câu hỏi bị trùng.'
```
#### 119. `QuestionBanks.Commands.UpdateQuestionBank.UpdateQuestionBankCommandHandlerTests.Handle_StatusAssigned_ShouldReturn403`

- **Module**: QuestionBanks
- **Duration**: 00:00:00.0023244

```
Expected result.Errors[0].Description "Bạn không có quyền thực hiện thao tác này trên bình luận." to contain "tr?ng thái Assigned".
```
#### 120. `QuestionBanks.Commands.RejectQuestionBanksCommandValidatorTests.Validate_DuplicateIdsTrimmed_ShouldHaveError`

- **Module**: QuestionBanks
- **Duration**: 00:00:00.0019827

```
FluentValidation.TestHelper.ValidationTestException : Expected an error message of 'Danh sách mã câu h?i b? trùng.'. Actual message was 'Danh sách mã câu hỏi bị trùng.'
```
#### 121. `QuestionBanks.Commands.QuestionOptions.CreateQuestionOptionCommandHandlerTests.Handle_OptionLimitExceeded_ShouldReturn400`

- **Module**: QuestionBanks
- **Duration**: 00:00:00.0027714

```
Expected result.Errors.First().Description "Dữ liệu đầu vào không hợp lệ." to contain "quá 4 đáp án".
```
#### 122. `QuestionTypes.Commands.UpdateQuestionType.UpdateQuestionTypeCommandHandlerTests.Handle_CodeExists_ShouldReturnFailure`

- **Module**: QuestionTypes
- **Duration**: 00:00:00.0023875

```
Expected result.Message "Mã code đã tồn tại." to contain "Mã code dã t?n t?i".
```
#### 123. `QuestionTypes.Commands.UpdateQuestionType.UpdateQuestionTypeCommandHandlerTests.Handle_NameExists_ShouldReturnFailure`

- **Module**: QuestionTypes
- **Duration**: 00:00:00.0019011

```
Expected result.Message "Tên loại câu hỏi đã tồn tại." to contain "Tên lo?i câu h?i dã t?n t?i".
```
#### 124. `QuestionTypes.Commands.UpdateQuestionType.UpdateQuestionTypeCommandHandlerTests.Handle_NotFound_ShouldReturnFailure`

- **Module**: QuestionTypes
- **Duration**: 00:00:00.0018186

```
Expected result.Message "Không tìm thấy loại câu hỏi." to contain "Không tìm th?y".
```
#### 125. `QuestionTypes.UpdateQuestionTypeCommandHandlerTests.Handle_DuplicateName_ShouldReturnFailure`

- **Module**: QuestionTypes
- **Duration**: 00:00:00.0032714

```
Expected result.Message "Tên loại câu hỏi đã tồn tại." to contain "tên".
```
#### 126. `QuestionTypes.CreateQuestionTypeCommandHandlerTests.Handle_DuplicateName_ShouldReturnFailure`

- **Module**: QuestionTypes
- **Duration**: 00:00:00.0017840

```
Expected result.Message "Tên loại câu hỏi đã tồn tại." to contain "tên".
```
#### 127. `Roadmap.Commands.GenerateRoadmapCommandValidatorTests.Validate_InvalidTargetAim_ShouldHaveError`

- **Module**: Roadmap
- **Duration**: 00:00:00.0010873

```
FluentValidation.TestHelper.ValidationTestException : Expected a validation error for property TargetAim
```
#### 128. `Roadmap.Commands.SubmitExamCommandHandlerTests.Handle_ExamNotFound_ReturnsFailure404`

- **Module**: Roadmap
- **Duration**: 00:00:00.0019932

```
Expected result.Message "Đề thi không tồn tại hoặc không có câu hỏi." to contain "Ð? thi không t?n t?i".
```
#### 129. `Roadmap.GenerateNextWeekCommandHandlerTests.Handle_ExamScoreEvaluatedWithWarning`

- **Module**: Roadmap
- **Duration**: 00:00:00.0318321

```
Expected result.Data!.HasWarning to be True, but found False.
```
#### 130. `SystemConfigs.GetSystemConfigByKeyQueryHandlerTests.Handle_ActiveConfig_IsActiveTrueInDto`

- **Module**: SystemConfigs
- **Duration**: 00:00:00.0013211

```
System.NullReferenceException : Object reference not set to an instance of an object.
```
#### 131. `SystemConfigs.GetAllSystemConfigsQueryHandlerTests.Handle_RepoReturnsConfigs_DtoFieldsMappedCorrectly`

- **Module**: SystemConfigs
- **Duration**: 00:00:00.0004984

```
System.InvalidOperationException : Nullable object must have a value.
```
#### 132. `SystemConfigs.GetSystemConfigByKeyQueryHandlerTests.Handle_ConfigFound_AllDtoFieldsMapped`

- **Module**: SystemConfigs
- **Duration**: 00:00:00.0004973

```
System.NullReferenceException : Object reference not set to an instance of an object.
```
#### 133. `SystemConfigs.GetAllSystemConfigsQueryHandlerTests.Handle_WithPage3Size20_PagingMetadataCorrect`

- **Module**: SystemConfigs
- **Duration**: 00:00:00.0004527

```
System.InvalidOperationException : Nullable object must have a value.
```
#### 134. `SystemConfigs.GetAllSystemConfigsQueryHandlerTests.Handle_RepoReturnsConfigs_ShouldReturn200WithPagedResult`

- **Module**: SystemConfigs
- **Duration**: 00:00:00.0006980

```
System.InvalidOperationException : Nullable object must have a value.
```
#### 135. `SystemConfigs.GetAllSystemConfigsQueryHandlerTests.Handle_ConfigsIncludeInactive_AllReturnedInList`

- **Module**: SystemConfigs
- **Duration**: 00:00:00.0039899

```
System.InvalidOperationException : Nullable object must have a value.
```
#### 136. `SystemConfigs.GetSystemConfigByKeyQueryHandlerTests.Handle_ConfigFound_ShouldReturn200WithDto`

- **Module**: SystemConfigs
- **Duration**: 00:00:00.0017981

```
Expected result.IsSuccess to be True, but found False.
```
#### 137. `TemplateParts.CreateTemplatePartCommandHandlerTests.Handle_ExamTemplateNotFound_ShouldReturnFailure`

- **Module**: TemplateParts
- **Duration**: 00:00:00.0088240

```
Expected result.IsSuccess to be False, but found True.
```
#### 138. `TemplateParts.CreateTemplatePartCommandHandlerTests.Handle_RepositoryThrowsException_ShouldReturn500`

- **Module**: TemplateParts
- **Duration**: 00:00:00.0016819

```
Expected result.StatusCode to be 500, but found 400 (difference of -100).
```
#### 139. `Titles.CreateTitleCommandHandlerTests.Handle_ValidRequest_ShouldReturn201WithTitle`

- **Module**: Titles
- **Duration**: 00:00:00.0006812

```
System.NullReferenceException : Object reference not set to an instance of an object.
```
#### 140. `Titles.UpdateTitleCommandHandlerTests.Handle_ValidRequest_UpdateCalledOnce`

- **Module**: Titles
- **Duration**: 00:00:00.0012650

```
System.NullReferenceException : Object reference not set to an instance of an object.
```
#### 141. `Titles.UpdateTitleCommandHandlerTests.Handle_SameName_ShouldNotCallGetTitleByName`

- **Module**: Titles
- **Duration**: 00:00:00.0008730

```
System.NullReferenceException : Object reference not set to an instance of an object.
```
#### 142. `Titles.CreateTitleCommandHandlerTests.Handle_ValidRequest_AddCalledOnce`

- **Module**: Titles
- **Duration**: 00:00:00.0127673

```
System.NullReferenceException : Object reference not set to an instance of an object.
```
#### 143. `Titles.CreateTitleCommandHandlerTests.Handle_NegativeXP_ShouldReturn400Failure`

- **Module**: Titles
- **Duration**: 00:00:00.0006282

```
System.NullReferenceException : Object reference not set to an instance of an object.
```
#### 144. `Titles.CreateTitleCommandHandlerTests.Handle_ValidRequest_CreatedTitleFieldsMatchCommand`

- **Module**: Titles
- **Duration**: 00:00:00.0011673

```
System.NullReferenceException : Object reference not set to an instance of an object.
```
#### 145. `Titles.UpdateTitleCommandHandlerTests.Handle_ValidRequest_ShouldReturn200WithTitle`

- **Module**: Titles
- **Duration**: 00:00:00.0005586

```
System.NullReferenceException : Object reference not set to an instance of an object.
```
#### 146. `Titles.UpdateTitleCommandHandlerTests.Handle_ValidRequest_FieldsUpdatedOnEntity`

- **Module**: Titles
- **Duration**: 00:00:00.0017689

```
System.NullReferenceException : Object reference not set to an instance of an object.
```
#### 147. `TopikWriting.SolveQuestion51HandlerTests.Handle_HangfireThrowsException_ShouldReturn500`

- **Module**: TopikWriting
- **Duration**: 00:00:00.0022221

```
Expected result.Message "Lỗi xử lý câu 51: Hangfire connection failed" to contain "Error processing sentence 51".
```
#### 148. `TopikWriting.SolveQuestion54HandlerTests.Handle_HangfireThrowsException_ShouldReturn500`

- **Module**: TopikWriting
- **Duration**: 00:00:00.0009437

```
Expected result.Message "Lỗi xử lý câu 54: Hangfire connection failed" to contain "Error processing sentence 54".
```
#### 149. `TopikWriting.SolveQuestion53HandlerTests.Handle_HangfireThrowsException_ShouldReturn500`

- **Module**: TopikWriting
- **Duration**: 00:00:00.0027627

```
Expected result.Message "Lỗi xử lý câu 53: Hangfire connection failed" to contain "Error processing sentence 53".
```
#### 150. `TopikWriting.SolveQuestion52HandlerTests.Handle_HangfireThrowsException_ShouldReturn500`

- **Module**: TopikWriting
- **Duration**: 00:00:00.0022322

```
Expected result.Message "Lỗi xử lý câu 52: Hangfire connection failed" to contain "Error processing sentence 52".
```
#### 151. `UserExam.SubmitUserExamCommandHandlerTests.Handle_ValidSubmit_ShouldReturn200WithScore`

- **Module**: UserExam
- **Duration**: 00:00:00.0023172

```
Expected result.Data!.FinalMcqScore to be 1, but found 0 (difference of -1).
```
#### 152. `UserExam.GetPracticeQuestionsQueryHandlerTests.Handle_TwoQuestionsNoPassage_ShouldReturnTwoGroups`

- **Module**: UserExam
- **Duration**: 00:00:00.0118451

```
Expected result.Data! to contain 2 item(s), but found 1: {
    Tokki.Application.UseCases.UserExam.DTOs.QuestionResultGroupDto
    {
        Questions = Tokki.Application.UseCases.UserExam.DTOs.Que...
```
#### 153. `Vocabulary.BulkCreateVocabulariesCommandHandlerTests.Handle_ExampleDuplicateInRequest_ShouldSkipDuplicateAndReturn201`

- **Module**: Vocabulary
- **Duration**: 00:00:00.0044820

```
Expected result.Data.Results[0].Message "Tạo vocabulary thành công. Đã bỏ qua 1 câu ví dụ trùng lặp." to contain "skip".
```
#### 154. `Vocabulary.Commands.CreateVocabularyByStaff.CreateVocabularyByStaffCommandHandlerTests.Handle_ExampleFiltering_ShouldFilterCorrectly`

- **Module**: Vocabulary
- **Duration**: 00:00:00.0026590

```
Expected result.Message "Tạo vocabulary thành công. Đang chờ phê duyệt. Đã bỏ qua 2 câu ví dụ trùng lặp." to contain "b? qua".
```
#### 155. `Vocabulary.Commands.BulkCreateVocabulariesCommandValidatorTests.Validate_DefinitionExceedLength_ShouldHaveError`

- **Module**: Vocabulary
- **Duration**: 00:00:00.0004145

```
FluentValidation.TestHelper.ValidationTestException : Expected an error message of 'Chiều dài của 'Definition' phải lớn hơn hoặc bằng 0 ký tự và ít hơn hoặc bằng 500 ký tự. Bạn đã nhập 501 ký tự.'. Ac...
```
#### 156. `Vocabulary.CreateVocabularyByStaffCommandHandlerTests.Handle_ValidData_ShouldReturnDraftStatus201`

- **Module**: Vocabulary
- **Duration**: 00:00:00.0016967

```
Expected result.Message "Tạo vocabulary thành công. Đang chờ phê duyệt." to contain "awaiting approval".
```
#### 157. `Vocabulary.BulkCreateVocabulariesByStaffCommandHandlerTests.Handle_ExampleDuplicateInRequest_ShouldSkipAndStillReturn201`

- **Module**: Vocabulary
- **Duration**: 00:00:00.4432429

```
Expected result.Data.Results[0].Message "Tạo thành công. Bỏ qua 1 câu ví dụ trùng." to contain "Skip".
```
#### 158. `Vocabulary.CreateVocabularyCommandHandlerTests.Handle_DuplicateTextAndDefinition_ShouldReturn400`

- **Module**: Vocabulary
- **Duration**: 00:00:00.0019687

```
Expected result.IsSuccess to be False, but found True.
```
#### 159. `Vocabulary.Commands.BulkCreateVocabulariesCommandValidatorTests.Validate_VocabulariesNull_ShouldHaveError`

- **Module**: Vocabulary
- **Duration**: 00:00:00.0004638

```
System.NullReferenceException : Object reference not set to an instance of an object.
```
#### 160. `Vocabulary.Commands.CreateVocabularyCommandValidatorTests.Validate_LongDefinition_ShouldHaveError`

- **Module**: Vocabulary
- **Duration**: 00:00:00.0004206

```
FluentValidation.TestHelper.ValidationTestException : Expected an error message of 'Definition không du?c vu?t quá 500 ký t?.'. Actual message was 'Definition không được vượt quá 500 ký tự.'
```
#### 161. `Vocabulary.Commands.BulkCreateVocabulariesByStaffCommandValidatorTests.Validate_TextEmpty_ShouldHaveError`

- **Module**: Vocabulary
- **Duration**: 00:00:00.0024430

```
FluentValidation.TestHelper.ValidationTestException : Expected an error message of ''Text' không được bỏ trống.'. Actual message was ''Text' must not be empty.'
```
#### 162. `Vocabulary.Commands.BulkCreateVocabulariesCommandValidatorTests.Validate_VocabulariesEmpty_ShouldHaveError`

- **Module**: Vocabulary
- **Duration**: 00:00:00.0004699

```
FluentValidation.TestHelper.ValidationTestException : Expected an error message of ''Danh sách vocabulary' không được bỏ trống.'. Actual message was ''Danh sách vocabulary' must not be empty.'
```
#### 163. `Vocabulary.Commands.CreateVocabularyCommandValidatorTests.Validate_EmptyText_ShouldHaveError`

- **Module**: Vocabulary
- **Duration**: 00:00:00.0002843

```
FluentValidation.TestHelper.ValidationTestException : Expected an error message of 'Text không du?c d? tr?ng.'. Actual message was 'Text không được để trống.'
```
#### 164. `Vocabulary.Commands.BulkCreateVocabulariesCommandValidatorTests.Validate_TextEmpty_ShouldHaveError`

- **Module**: Vocabulary
- **Duration**: 00:00:00.0002955

```
FluentValidation.TestHelper.ValidationTestException : Expected an error message of ''Text' không được bỏ trống.'. Actual message was ''Text' must not be empty.'
```
#### 165. `Vocabulary.BulkCreateVocabulariesCommandHandlerTests.Handle_HasDuplicateVocab_ShouldReturn400`

- **Module**: Vocabulary
- **Duration**: 00:00:00.0033641

```
Expected result.Message to match (x.Contains("VOCABULARY_DUPLICATE") OrElse x.Contains("coincide")), but found "Không thể tạo vocabulary. Phát hiện 1 từ vựng bị trùng lặp (Text + Definition):
1. Từ '안...
```
#### 166. `Vocabulary.Commands.BulkCreateVocabulariesByStaffCommandValidatorTests.Validate_VocabulariesNull_ShouldHaveError`

- **Module**: Vocabulary
- **Duration**: 00:00:00.0054101

```
System.NullReferenceException : Object reference not set to an instance of an object.
```
#### 167. `Vocabulary.Commands.CreateVocabularyCommandValidatorTests.Validate_DuplicateExamples_ShouldHaveError`

- **Module**: Vocabulary
- **Duration**: 00:00:00.0038815

```
FluentValidation.TestHelper.ValidationTestException : Expected an error message of 'Danh sách câu ví d? b? trùng: hello'. Actual message was 'Danh sách câu ví dụ bị trùng: Hello'
```
#### 168. `VocabularyExample.Commands.UpdateVocabularyExampleCommandValidatorTests.Validate_EmptyExampleId_ShouldHaveError`

- **Module**: VocabularyExample
- **Duration**: 00:00:00.0005430

```
FluentValidation.TestHelper.ValidationTestException : Expected an error message of ''ExampleId' không được bỏ trống.'. Actual message was ''ExampleId' must not be empty.'
```
#### 169. `VocabularyExample.DeleteVocabularyExampleCommandHandlerTests.Handle_RepositoryThrows_ShouldReturn500`

- **Module**: VocabularyExample
- **Duration**: 00:00:00.0026142

```
System.Exception : DB update failed
```
#### 170. `VocabularyExample.Commands.UpdateVocabularyExampleCommandValidatorTests.Validate_NullUpdateData_ShouldHaveError`

- **Module**: VocabularyExample
- **Duration**: 00:00:00.0065541

```
FluentValidation.TestHelper.ValidationTestException : Expected an error message of ''UpdateData' không được bỏ trống.'. Actual message was ''UpdateData' must not be empty.'
```
#### 171. `VocabularyExample.Commands.UpdateVocabularyExampleCommandValidatorTests.Validate_InvalidStatus_ShouldHaveError`

- **Module**: VocabularyExample
- **Duration**: 00:00:00.0007582

```
FluentValidation.TestHelper.ValidationTestException : Expected a validation error for property UpdateData.Status
----
Properties with Validation Errors:
[0]: UpdateData.Status.Value

```
---

## 5.4 Test Case Type Distribution

| Type | Code | Description |
|---|---|---|
| Normal | **N** | Happy path - valid inputs, expected successful outcomes |
| Abnormal | **A** | Error handling - invalid inputs, missing data, unauthorized access |
| Boundary | **B** | Edge cases - limits, empty collections, null values, exact boundaries |

> Approximate distribution: ~49% Normal, ~40% Abnormal, ~10% Boundary

---

## 5.5 Status Code Coverage

| Status Code | Description | Modules Using |
|---|---|---|
| **200** | Successful operations | All modules |
| **201** | Resource created | Accounts, Blogs, Categories, Vocabulary, Topics, Comments |
| **400** | Validation errors, bad requests | All modules with input validation |
| **401** | Unauthorized (missing JWT) | Accounts, Blogs, FavoriteVocabulary, Games, UserExam |
| **403** | Forbidden (wrong user/role) | Blogs, Comments, LiveChat, MiniGame |
| **404** | Resource not found | All modules with entity lookups |
| **409** | Conflict (duplicate data) | Accounts (email/phone) |
| **500** | Server error (exceptions) | All modules |

---

## 5.6 Test Infrastructure

| Component | Version | Purpose |
|---|---|---|
| xUnit | 2.9.2 | Test framework with `[Fact]` attributes |
| Moq | 4.20.72 | Mock framework with `Callback`, `Verify`, `It.Is<>()` |
| FluentAssertions | 8.8.0 | `Should().Be()`, `Should().BeTrue()`, `Should().NotBeSameAs()` |
| QACollector | Internal | Automated test case logging for Excel report |
| ExcelReportGenerator | Internal | `.xlsx` report generation via EPPlus |
| SourceCodeCounter | Internal | LOC calculation for coverage metrics |
| DefaultHttpContext | Built-in | JWT/ClaimsPrincipal simulation |

---

## 5.7 Automated Report Pipeline

```
[Fact] Test Method
    |
QACollector.LogTestCase("Feature", TestCaseDetail)
    |
SourceCodeCounter.GetLinesOfCode("Feature")
    |
ExcelReportGenerator.ExportStandardReport()
    |
Output: Project_Test_Report_{date}.xlsx
```

---

## 5.8 Branch Coverage Highlights

| Module | Extra Branch Coverage Files | Key Branches |
|---|---|---|
| Accounts | GoogleLoginBranch, FacebookLoginBranch, FacebookCompleteRegistrationBranch, CreateAccountByAdminBranch | OAuth paths, existing vs. new user |
| Comments | CreateCommentBranch | Nested reply flattening, orphan parent |
| OTPs | VerifyEmailOtpBranch | Expiration, retry, status transitions |
| Vocabulary | 6 Validator test files | Input validation boundaries |
| UserExam | SyncMCQProgressValidator, DTO tests | Input validation, DTO structure |

---

## 5.9 Defect Summary
| Round | Total Defects | Details |
|---|---|---|
| Round 1 | 171 | See Section 5.3 for failed test details |

> **Action Required**: Please review the 171 failed test case(s) above and fix the underlying issues.
---

## 5.10 Conclusion
1. **1923 / 2094** test cases passed (**91.83% pass rate**)
2. **171 test case(s) failed** â€” see Section 5.3 for details
3. Every handler targets minimum 6 test cases covering key scenarios
4. Please fix failed tests and re-run this script to regenerate the report
5. Execution time: **00:44.422**
