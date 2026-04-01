using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Tokki.UnitTest.Utilities
{
    /// <summary>
    /// Scans the Tokki.Application source tree and counts the real non-blank,
    /// non-comment lines of code for specific UseCase folders.
    /// </summary>
    public static class SourceCodeCounter
    {
        // ─────────────────────────────────────────────────────────────────
        // CONFIGURE: path to Tokki.Application/UseCases relative to the
        // test assembly output directory (bin/Debug/net9.0/).
        // ─────────────────────────────────────────────────────────────────
        private const string RelativeApplicationPath =
            @"..\..\..\..\Tokki.Application\UseCases";

        // ─────────────────────────────────────────────────────────────────
        // MAPPING TABLE
        //
        // Maps QACollector feature names to specific Application UseCases 
        // sub-folder paths (e.g., "Accounts\Commands\AdminSoftDeleteAccount")
        //
        // Add an entry here whenever you add a new feature/test group.
        // ─────────────────────────────────────────────────────────────────
        public static readonly Dictionary<string, string> FeatureToFolderMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // ── Account ───────────────────────────────────────────────
                { "Account - Admin Soft Delete",        @"Accounts\Commands\AdminSoftDeleteAccount" },
                { "Account - Admin Update User",         @"Accounts\Commands\AdminUpdateUser" },
                { "Account - Change Password",           @"Accounts\Commands\ChangePassword" },
                { "Account - Create By Admin",           @"Accounts\Commands\CreateAccountByAdmin" },
                { "Account - Get Account Detail By Id",  @"Accounts\Queries\GetAccountDetailById" },
                { "Account - Get All Accounts",          @"Accounts\Queries\GetAccount" }, 
                { "Account - Get User Profile",          @"Accounts\Queries\GetUserProfile" },
                { "Account - Get My Level",              @"Accounts\Queries\GetMyLevel" },
                { "Account - Get VIP Accounts",          @"Accounts\Queries\GetInternalUserVipAccounts" },
                { "Account - Login",                     @"Accounts\Commands\Login" },
                { "Account - Register",                  @"Accounts\Commands\Register" },
                { "Account - Reset Password",            @"Accounts\Commands\ResetPasswordWhenForgotPass" },
                { "Account - Update Profile",            @"Accounts\Commands\UpdateProfile" },

                // ── Topics ────────────────────────────────────────────────
                { "Topic - Add Vocabularies",            @"Topics\Commands\AddVocabulariesToTopic" },
                { "Topic - Approve",                     @"Topics\Commands\ApproveTopic" },
                { "Topic - Check Completion",            @"Topics\Queries\CheckTopicCompletion" },
                { "Topic - Create",                      @"Topics\Commands\CreateTopic" },
                { "Topic - Create By Staff",             @"Topics\Commands\CreateTopicByStaff" },
                { "Topic - Delete",                      @"Topics\Commands\DeleteTopic" },
                { "Topic - Get All",                     @"Topics\Queries\GetAllTopics" },
                { "Topic - Get All For User",            @"Topics\Queries\GetAllTopicsForUser" },
                { "Topic - Get Detail By Id",            @"Topics\Queries\GetTopicDetailById" },
                { "Topic - Get For User",                @"Topics\Queries\GetTopicForUser" },
                { "Topic - Get Study Vocabs",            @"Topics\Queries\GetStudyVocabs" },
                { "Topic - Publish",                     @"Topics\Commands\PublishTopic" },
                { "Topic - Reject",                      @"Topics\Commands\RejectTopic" },
                { "Topic - Remove Vocabularies",         @"Topics\Commands\RemoveVocabulariesFromTopic" },
                { "Topic - Submit For Approval",         @"Topics\Commands\SubmitTopicForApproval" },
                { "Topic - Update",                      @"Topics\Commands\UpdateTopic" },
                { "Topic - Update Order Index",          @"Topics\Commands\UpdateTopicOrderIndex" },
                { "Topic - Update Status",               @"Topics\Commands\UpdateTopicStatus" },

                // ── Vocabulary ────────────────────────────────────────────
                { "Vocabulary - Approve",                @"Vocabulary\Commands\ApproveVocabularies" },
                { "Vocabulary - Bulk Create",            @"Vocabulary\Commands\BulkCreateVocabularies" },
                { "Vocabulary - Bulk Create By Staff",   @"Vocabulary\Commands\BulkCreateVocabulariesByStaff" },
                { "Vocabulary - Create",                 @"Vocabulary\Commands\CreateVocabulary" },
                { "Vocabulary - Create By Staff",        @"Vocabulary\Commands\CreateVocabularyByStaff" },
                { "Vocabulary - Delete",                 @"Vocabulary\Commands\DeleteVocabulary" },
                { "Vocabulary - Flash Card",             @"Vocabulary\Queries\FlashCard" },
                { "Vocabulary - Get By Text",            @"Vocabulary\Queries\GetVocabularyByText" },
                { "Vocabulary - Get Detail (Admin)",     @"Vocabulary\Queries\GetVocabularyDetailByIdForAdmin" },
                { "Vocabulary - Get Detail",             @"Vocabulary\Queries\GetVocabularyDetailById" },
                { "Vocabulary - Get All For Manager",    @"Vocabulary\Queries\GetAllForManager" },
                { "Vocabulary - Get By Topic",           @"Vocabulary\Queries\GetVocabulariesByTopic" },
                { "Vocabulary - Reject",                 @"Vocabulary\Commands\RejectVocabularies" },
                { "Vocabulary - Search",                 @"Vocabulary\Queries\SearchVocabulary" },
                { "Vocabulary - Submit For Approval",    @"Vocabulary\Commands\SubmitVocabulariesForApproval" },
                { "Vocabulary - Update",                 @"Vocabulary\Commands\UpdateVocabulary" },

                // ── VocabularyExample ─────────────────────────────────────
                { "Vocabulary Example - Add",            @"VocabularyExample\Commands\AddVocabularyExamples" },
                { "Vocabulary Example - Delete",         @"VocabularyExample\Commands\DeleteVocabularyExample" },
                { "Vocabulary Example - Get By Vocab",   @"VocabularyExample\Queries\GetVocabularyExamplesByVocabularyId" },
                { "Vocabulary Example - Update",         @"VocabularyExample\Commands\UpdateVocabularyExample" },

                // ── VocabSpacedRepetition ─────────────────────────────────
                { "Spaced Repetition - Get Due Reviews", @"VocabSpacedRepetition\Queries\GetDueReviews" },
                { "Spaced Repetition - Submit Review",   @"VocabSpacedRepetition\Commands\SubmitReview" },

                // ── Payments ──────────────────────────────────────────────
                { "Payment - Create",                    @"Payments\Commands\CreatePayment" },
                { "Payment - Get QR",                    @"Payments\Queries\GetPaymentQr" },

                // ── VipPackages ───────────────────────────────────────────
                { "VIP Package - Create",                @"VipPackages\Commands\CreateVipPackage" },
                { "VIP Package - Delete",                @"VipPackages\Commands\DeleteVipPackage" },
                { "VIP Package - Get All",               @"VipPackages\Queries\GetAllVipPackages" },
                { "VIP Package - Update",                @"VipPackages\Commands\UpdateVipPackage" },

                // ── LiveChat ──────────────────────────────────────────────
                { "LiveChat - Create Support",           @"LiveChat\Commands\CreateSupportChat" },
                { "LiveChat - Join Support",             @"LiveChat\Commands\JoinSupportChat" },
                { "LiveChat - Close Support",            @"LiveChat\Commands\CloseSupportChat" },
                { "LiveChat - Get Chat History",         @"LiveChat\Queries\GetChatHistory" },
                { "LiveChat - Get My Rooms",             @"LiveChat\Queries\GetMyRooms" },
                { "LiveChat - Get Pending Supports",     @"LiveChat\Queries\GetPendingSupports" },

                // ── Games ─────────────────────────────────────────────────
                { "Games - Save Result",                 @"Games\Commands\SaveGameResult" },
                { "Games - Update Result",               @"Games\Commands\UpdateGameResult" },
                { "Games - Check Played Level",          @"Games\Queries\CheckUserPlayedLevel" },
                { "Games - Get All For User",            @"Games\Queries\GetAllGamesForUser" },
                { "Games - Get Result For User",         @"Games\Queries\GetGameResultForUser" },
                { "Games - Get Results All Users",       @"Games\Queries\GetGameResultsForAllUsers" },

                // ── MiniGame ──────────────────────────────────────────────
                { "MiniGame - Submit Wordle Guess",      @"MiniGame\Commands\SubmitWordleGuess" },
                { "MiniGame - Submit Wordle Sentence",   @"MiniGame\Commands\SubmitWordleSentence" },
                { "MiniGame - Toggle Like Wordle",       @"MiniGame\Commands\ToggleLikeWordle" },
                { "MiniGame - Publish Wordle",           @"MiniGame\Commands\PublishWordleSentence" },
                { "Wordle - Submit Guess",               @"MiniGame\Commands\SubmitWordleGuess" },
                { "Wordle - Submit Sentence",            @"MiniGame\Commands\SubmitWordleSentence" },
                { "Wordle - Toggle Like",                @"MiniGame\Commands\ToggleLikeWordle" },
                { "MiniGame - Daily Wordle Status",      @"MiniGame\Queries\Wordle" },
                { "MiniGame - Get Wordle Result",        @"MiniGame\Queries\Wordle" },

                // ── EmailTemplates ────────────────────────────────────────
                { "Email Template - Create Auto",        @"EmailTemplates\Commands\CreateEmailAutoTemplate" },
                { "Email Template - Delete Auto",        @"EmailTemplates\Commands\DeleteEmailTemplate" },
                { "Email Template - Update Auto",        @"EmailTemplates\Commands\UpdateEmailAutoTemplate" },
                { "Email - Create Campaign",             @"EmailTemplates\Commands\CreateEmailCampaign" },
                { "Email - Delete Campaign",             @"EmailTemplates\Commands\DeleteEmailCampaign" },
                { "Email - Update Campaign",             @"EmailTemplates\Commands\UpdateEmailCampaign" },
                { "Email Template - Get All",            @"EmailTemplates\Queries\GetAllEmailAutoTemplates" },
                { "Email Template - Get By Id",          @"EmailTemplates\Queries\GetEmailAutoTemplateById" },
                { "Email - Get Campaign By Id",          @"EmailTemplates\Queries\GetEmailCampaignById" },
                { "Email - Get Campaigns List",          @"EmailTemplates\Queries\GetEmailCampaigns" },

                // ── ExamTemplates ─────────────────────────────────────────
                { "Exam Template - Create",              @"ExamTemplates\Commands\CreateExamTemplate" },
                { "Exam Template - Approve",             @"ExamTemplates\Commands\ApproveExamTemplate" },
                { "Exam Template - Delete",              @"ExamTemplates\Commands\DeleteExamTemplate" },
                { "Exam Template - Duplicate",           @"ExamTemplates\Commands\DuplicateExamTemplate" },
                { "Exam Template - Reject",              @"ExamTemplates\Commands\RejectExamTemplate" },
                { "Exam Template - Reset To Draft",      @"ExamTemplates\Commands\ResetExamTemplateToDraft" },
                { "Exam Template - Submit",              @"ExamTemplates\Commands\SubmitExamTemplate" },
                { "Exam Template - Update",              @"ExamTemplates\Commands\UpdateExamTemplate" },
                { "Exam Template - Get Admin List",      @"ExamTemplates\Queries\GetAdminExamTemplates" },
                { "Exam Template - Get By Id",           @"ExamTemplates\Queries\GetExamTemplateById" },

                // ── Exam ──────────────────────────────────────────────────
                { "Exam - Add Question",                 @"Exam\Commands\AddQuestionToExam" },
                { "Exam - Create",                       @"Exam\Commands\CreateExam" },
                { "Exam - Delete Exam",                  @"Exam\Commands\DeleteExam" },
                { "Exam - Export PDF",                   @"Exam\Commands\ExportExamToPdf" },
                { "Exam - Regenerate Part",              @"Exam\Commands\RegenerateExamPart" },
                { "Exam - Remove Question",              @"Exam\Commands\RemoveQuestionFromExam" },
                { "Exam - Update Info",                  @"Exam\Commands\UpdateExamInfo" },
                { "Exam - Update Status",                @"Exam\Commands\UpdateExamStatus" },
                { "Exam - Get Detail",                   @"Exam\Queries\GetExamDetailQuery" },
                { "Exam - Get Detail Stats",             @"Exam\Queries\GetExamDetailStats" },
                { "Exam - Get List",                     @"Exam\Queries\GetExams" },
                { "Exam - Get Stats List",               @"Exam\Queries\GetExamsStats" },
                { "Exam - Get Questions By Part",        @"Exam\Queries\GetQuestionsByPart" },
                { "Exam - Get Template Skills",          @"Exam\Queries\GetTemplateSkills" },
                { "Exam - Get User Exams",               @"Exam\Queries\GetUserExamsByExamId" },

                // ── UserExam ──────────────────────────────────────────────
                { "UserExam - Create Session",           @"UserExam\Commands\CreateUserTakeExam" },
                { "UserExam - Move To Next Skill",       @"UserExam\Commands\MoveToNextSkill" },
                { "UserExam - Submit",                   @"UserExam\Commands\SubmitUserExam" },
                { "UserExam - Sync MCQ",                 @"UserExam\Commands\SyncMCQProgress" },
                { "UserExam - Sync Writing",             @"UserExam\Commands\SyncWritingProgress" },
                { "UserExam - Check Grading",            @"UserExam\Queries\CheckGradingStatus" },
                { "UserExam - Get Analysis",             @"UserExam\Queries\GetExamAnalysis" },
                { "UserExam - Get In Progress",          @"UserExam\Queries\GetInProgressExam" },
                { "UserExam - Get Listening Detail",     @"UserExam\Queries\GetListeningDetail" },
                { "UserExam - Get Practice Questions",   @"UserExam\Queries\GetPracticeQuestions" },
                { "UserExam - Get Reading Detail",       @"UserExam\Queries\GetReadingDetail" },
                { "UserExam - Get Result",               @"UserExam\Queries\GetUserExamResult" },
                { "UserExam - Get Review",               @"UserExam\Queries\GetUserExamReview" },
                { "UserExam - Get List",                 @"UserExam\Queries\GetUserExams" },
                { "UserExam - Get Writing Detail",       @"UserExam\Queries\GetWritingDetail" },

                // ── TopikWriting (ClassifyAndSolve is commented-out/disabled) ──
                { "TopikWriting - Classify And Solve",   @"TopikWriting\Commands\ClassifyAndSolve" },

                // ── UserTopicProgress ─────────────────────────────────────
                { "User Topic Progress - Complete",      @"UserTopicProgress\Commands\CompleteTopic" },

                // ── Excel ─────────────────────────────────────────────────
                { "Excel - Add Vocab",                   @"Excel\Commands\AddVocabByExcel" },
                { "Excel - Import Accounts",             @"Excel\Commands\ImportAccounts" },
                { "Excel - Import Pronunciation",        @"Excel\Commands\ImportPronunciationExample" },
                { "Excel - Import Questions",            @"Excel\Commands\ImportQuestionsFromExcel" },
                { "Excel - Import Question Types",       @"Excel\Commands\ImportQuestionTypes" },
                { "Excel - Export Accounts",             @"Excel\Queries\ExportAccounts" },
                { "Excel - Export Question Types",       @"Excel\Queries\ExportQuestionTypes" },
                { "Excel - Export Vocab Topic",          @"Excel\Queries\ExportVocabByTopic" },
                { "Excel - Get Account Template",        @"Excel\Queries\TemplateAccount" },
                { "Excel - Get QuestionType Template",   @"Excel\Queries\TemplateQuestionType" },

                // ── TopikWriting ──────────────────────────────────────────
                { "TopikWriting - Solve Q51",            @"TopikWriting\Commands\SolveQuestion51" },
                { "TopikWriting - Solve Q52",            @"TopikWriting\Commands\SolveQuestion52" },
                { "TopikWriting - Solve Q53",            @"TopikWriting\Commands\SolveQuestion53" },
                { "TopikWriting - Solve Q54",            @"TopikWriting\Commands\SolveQuestion54" },

                // ── TextToSpeech ──────────────────────────────────────────
                { "TextToSpeech - Generate Audio",       @"TextToSpeech\Commands\GenerateVocabularyAudioUrl" },

                // ── PronunciationExample ──────────────────────────────────
                { "Pronunciation Example - Get Detail",  @"PronunciationExample\Queries\GetExampleDetail" },
                { "Pronunciation Example - Get By Rule", @"PronunciationExample\Queries\GetExamplesByRuleId" },

                // ── PronunciationRule ─────────────────────────────────────
                { "Pronunciation - Evaluate",            @"PronunciationRule\Commands\EvaluatePronunciation" },
                { "Pronunciation Rule - Create",         @"PronunciationRule\Commands\CreatePronunciationRule" },
                { "Pronunciation Rule - Delete",         @"PronunciationRule\Commands\DeletePronunciationRule" },
                { "Pronunciation Rule - Update",         @"PronunciationRule\Commands\UpdatePronunciationRule" },
                { "Pronunciation Rule - Get By Id",      @"PronunciationRule\Queries\GetPronunciationRuleById" },
                { "Pronunciation Rule - Get List",       @"PronunciationRule\Queries\GetPronunciationRules" },

                // ── QuestionBanks ─────────────────────────────────────────
                { "Question Bank - Approve",             @"QuestionBanks\Commands\ApproveQuestionBank" },
                { "Question Bank - Reject",              @"QuestionBanks\Commands\RejectQuestionBank" },
                { "Question Bank - Delete",              @"QuestionBanks\Commands\DeleteQuestionBank" },
                { "Question Bank - Submit For Approval", @"QuestionBanks\Commands\SubmitQuestionBankForApproval" },
                { "Question Bank - Activate",            @"QuestionBanks\Commands\ActivateQuestionBanks" },
                { "Question Bank - Create",              @"QuestionBanks\Commands\CreateQuestionBank" },
                { "Question Bank - Create By Staff",     @"QuestionBanks\Commands\CreateQuestionBankByStaff" },
                { "Question Bank - Update",              @"QuestionBanks\Commands\UpdateQuestionBank" },
                { "Question Bank Option - Create",       @"QuestionBanks\Commands\QuestionOptions\Create" },
                { "Question Bank Option - Delete",       @"QuestionBanks\Commands\QuestionOptions\Delete" },
                { "Question Bank Option - Update",       @"QuestionBanks\Commands\QuestionOptions\Update" },
                { "Question Bank - Get By Id",           @"QuestionBanks\Queries\GetQuestionBankById" },
                { "Question Bank - Get List",            @"QuestionBanks\Queries\GetQuestionBanks" },
                { "Question Bank - Get By TypeId",       @"QuestionBanks\Queries\GetByQuestionTypeId" },

                // ── QuestionTypes ─────────────────────────────────────────
                { "Question Type - Create",              @"QuestionTypes\Commands\CreateQuestionType" },
                { "Question Type - Delete",              @"QuestionTypes\Commands\DeleteQuestionType" },
                { "Question Type - Update",              @"QuestionTypes\Commands\UpdateQuestionType" },
                { "Question Type - Get By Id",           @"QuestionTypes\Queries\GetQuestionTypeById" },
                { "Question Type - Get List",            @"QuestionTypes\Queries\GetQuestionTypes" },

                // ── Roadmap ───────────────────────────────────────────────
                { "Roadmap - Cancel",                    @"Roadmap\Commands\CancelRoadmapCommandHandler.cs" },
                { "Roadmap - Complete Task",             @"Roadmap\Commands\CompleteTaskCommandHandler.cs" },
                { "Roadmap - Submit Exam",               @"Roadmap\Commands\SubmitExamCommandHandler.cs" },
                { "Roadmap - Process Weekly Result",     @"Roadmap\Commands\ProcessWeeklyResultCommandHandler.cs" },
                { "Roadmap - Generate Next Week",        @"Roadmap\Commands\GenerateNextWeekCommandHandler.cs" },
                { "Roadmap - Generate Roadmap",          @"Roadmap\Commands\GenerateRoadmapCommandHandler.cs" },
                { "Roadmap - Get",                       @"Roadmap\Queries\GetRoadmapQueryHandler.cs" },
                { "Roadmap - Get Task Detail",           @"Roadmap\Queries\GetTaskDetailQueryHandler.cs" },
                { "Roadmap - Get Entrance Exam",         @"Roadmap\Queries\GetEntranceExamQueryHandler.cs" },
                { "Roadmap - Get Entrance Feedback",     @"Roadmap\Queries\GetEntranceFeedbackQueryHandler.cs" },
                { "Roadmap - Get Virtual Quiz",          @"Roadmap\Queries\GetVirtualQuizQueryHandler.cs" },

                // ── Reports ───────────────────────────────────────────────
                { "Report - Create",                     @"Reports\Commands\CreateReport" },
                { "Report - Delete",                     @"Reports\Commands\DeleteReport" },
                { "Report - Mark Read",                  @"Reports\Commands\MarkReportRead" },
                { "Report - Update Status",              @"Reports\Commands\UpdateReportStatus" },
                { "Report - Get All",                    @"Reports\Queries\GetAllReports" },
                { "Report - Get Notifications",          @"Reports\Queries\GetReportNotifications" },

                // ── Cloudinary ────────────────────────────────────────────
                { "Cloudinary - Upload Audio",               @"Cloudinary\Commands\UploadAudio" },
                { "Cloudinary - Upload Image",               @"Cloudinary\Commands\UploadImage" },
                { "Cloudinary - Upload Vocab Image By Url",  @"Cloudinary\Commands\UploadVocabularyImageByUrl" },

                // ── Categories ────────────────────────────────────────────
                { "Category - Create",                   @"Categories\Commands\CreateCategory" },
                { "Category - Delete",                   @"Categories\Commands\DeleteCategory" },
                { "Category - Update",                   @"Categories\Commands\UpdateCategory" },
                { "Category - Get All",                  @"Categories\Queries\GetAllCategoriesQueryHandler.cs" },

                // ── Comments ─────────────────────────────────────────────
                { "Comment - Create",                    @"Comments\Commands\CreateComment" },
                { "Comment - Update",                    @"Comments\Commands\UpdateComment" },
                { "Comment - Get",                       @"Comments\Queries\GetCommentsQueryHandler.cs" },

                // ── Titles ────────────────────────────────────────────────
                { "Title - Create",                      @"Titles\Commands\CreateTitle" },
                { "Title - Delete",                      @"Titles\Commands\DeleteTitle" },
                { "Title - Update",                      @"Titles\Commands\UpdateTitle" },
                { "Title - Get All",                     @"Titles\Queries\GetAllTitlesQueryHandler.cs" },
                { "Title - Get By Id",                   @"Titles\Queries\GetTitleByIdQueryHandler.cs" },

                // ── Blogs ─────────────────────────────────────────────────
                { "Blog - Approve",                      @"Blogs\Commands\ApproveBlog" },
                { "Blog - Create",                       @"Blogs\Commands\CreateBlog" },
                { "Blog - Delete",                       @"Blogs\Commands\DeleteBlog" },
                { "Blog - Increase View Count",          @"Blogs\Commands\IncreaseViewCount" },
                { "Blog - Reject",                       @"Blogs\Commands\RejectBlog" },
                { "Blog - Submit For Approval",          @"Blogs\Commands\SubmitBlogForApproval" },
                { "Blog - Update",                       @"Blogs\Commands\UpdateBlog" },
                { "Blog - Get By Id",                    @"Blogs\Queries\GetBlogByIdQueryHandler.cs" },
                { "Blog - Get Paged Blogs",              @"Blogs\Queries\GetPagedBlogs" },

                // ── FavoriteVocabulary ────────────────────────────────────
                { "Favorite Vocabulary - Add",           @"FavoriteVocabulary\Commands\AddFavoriteVocabulary" },
                { "Favorite Vocabulary - Remove",        @"FavoriteVocabulary\Commands\RemoveFavoriteVocabulary" },
                { "Favorite Vocabulary - Get",           @"FavoriteVocabulary\Queries\GetFavoriteVocabularies" },

                // ── Leaderboard ───────────────────────────────────────────
                { "Leaderboard - Get",                   @"Leaderboard\Queries" },

                // ── Passages ──────────────────────────────────────────────
                { "Passage - Create",                    @"Passages\Commands\CreatePassage" },
                { "Passage - Delete",                    @"Passages\Commands\DeletePassage" },
                { "Passage - Update",                    @"Passages\Commands\UpdatePassage" },
                { "Passage - Get By Id",                 @"Passages\Queries\GetPassageById" },
                { "Passage - Get List",                  @"Passages\Queries\GetPassages" },

                // ── OTP ───────────────────────────────────────────────────
                { "OTP - Send General",                  @"Otps\Commands\SendOTPForAuth" },
                { "OTP - Send Email Verify",             @"Otps\Commands\SendOtpForEmailVerification" },
                { "OTP - Verify Email",                  @"Otps\Commands\VerifyEmailOtp" },
                { "OTP - Send Forgot Password",          @"Otps\Commands\ForgotPassword" },
                { "OTP - Verify Forgot Password",        @"Otps\Commands\VerifyForgotPasswordOtp" },

                // ── StatisticBlog ─────────────────────────────────────────
                { "StatisticBlog - Dashboard Stats",     @"StatisticBlog\Queries\GetDashboardStatsQueryHandler.cs" },
                { "StatisticBlog - Top Authors",         @"StatisticBlog\Queries\GetTopAuthorsQuery.cs" },
                { "StatisticBlog - Top Blogs",           @"StatisticBlog\Queries\GetTopBlogsQuery.cs" },

                // ── Solitaire ─────────────────────────────────────────────
                { "Solitaire - Save Result",             @"Solitaire\Commands\SaveSolitaireResult" },
                { "Solitaire - Get Result For User",     @"Solitaire\Queries\GetSolitaireResultForUser" },
                { "Solitaire - Get All Results",         @"Solitaire\Queries\GetSolitaireResultsForAllUsers" },

                // ── Statistics ────────────────────────────────────────────
                { "Statistics - Dashboard Overview",     @"Statistics\Queries\GetDashboardOverviewHandler.cs" },
                { "Statistics - Revenue By Package",     @"Statistics\Queries\GetRevenueByPackage" },
                { "Statistics - Revenue Chart",          @"Statistics\Queries\GetRevenueChart" },
                { "Statistics - Transactions Report",    @"Statistics\Queries\GetTransactionsReport" },

                // ── SystemConfigs ─────────────────────────────────────────
                { "SystemConfig - Create",               @"SystemConfigs\Commands\CreateSystemConfig" },
                { "SystemConfig - Update",               @"SystemConfigs\Commands\UpdateSystemConfig" },
                { "SystemConfig - Get All",              @"SystemConfigs\Queries\GetAll" },
                { "SystemConfig - Get By Key",           @"SystemConfigs\Queries\GetSystemConfigByKey" },

                // ── TemplateParts ─────────────────────────────────────────
                { "TemplatePart - Create",               @"TemplateParts\Commands\CreateTemplatePart" },
                { "TemplatePart - Delete",               @"TemplateParts\Commands\DeleteTemplatePart" },
                { "TemplatePart - Update",               @"TemplateParts\Commands\UpdateTemplatePart" },
                { "TemplatePart - Get By Id",            @"TemplateParts\Queries\GetTemplatePartByIdQueryHandler.cs" },
                { "TemplatePart - Get List",             @"TemplateParts\Queries\GetTemplatePartsQueryHandler.cs" },

                // ── TextToSpeech ──────────────────────────────────────────
                { "TextToSpeech - Generate Audio URL",   @"TextToSpeech\Commands\GenerateVocabularyAudioUrl" },

                // ── VocabSpacedRepetition ─────────────────────────────────
                { "VocabSR - Submit Review",             @"VocabSpacedRepetition\Commands\SubmitReview" },
                { "VocabSR - Get Due Reviews",           @"VocabSpacedRepetition\Queries\GetDueReviews" }
            };

        /// <summary>
        /// Gets the lines of code specifically for the mapped sub-folder for 
        /// the given QACollector feature name.
        /// </summary>
        public static int GetLinesOfCode(string featureName)
        {
            if (!FeatureToFolderMap.TryGetValue(featureName, out var relativeFolder))
                return 0;

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string targetPath = Path.GetFullPath(Path.Combine(baseDir, RelativeApplicationPath, relativeFolder));

            if (File.Exists(targetPath))
                return CountMeaningfulLines(targetPath);

            if (!Directory.Exists(targetPath))
                return 0;

            return CountLinesInFolder(targetPath);
        }

        private static int CountLinesInFolder(string folder)
        {
            int total = 0;
            foreach (string file in Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories))
            {
                total += CountMeaningfulLines(file);
            }
            return total;
        }

        private static int CountMeaningfulLines(string filePath)
        {
            try
            {
                return File.ReadAllLines(filePath)
                           .Select(line => line.Trim())
                           .Count(line => line.Length > 0 &&
                                          !line.StartsWith("//", StringComparison.Ordinal));
            }
            catch
            {
                return 0;
            }
        }
    }
}
