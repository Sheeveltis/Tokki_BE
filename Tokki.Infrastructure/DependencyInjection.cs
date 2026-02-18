using Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tokki.Application.Common.Helpers;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Infrastructure.Data;
using Tokki.Infrastructure.Repositories;
using Tokki.Infrastructure.Services;
using Tokki.Infrastructure.Services.Gemini;
namespace Tokki.Infrastructure
{
    public static class DependencyInjection
    {
        // Hàm này sẽ được gọi bên WebAPI
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. Đăng ký DbContext
            services.AddDbContext<TokkiDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // 2. Đăng ký Repositories
            services.AddScoped<IBlogRepository, BlogRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<ISystemConfigRepository, SystemConfigRepository>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IOtpRepository, OtpRepository>();
            services.AddScoped<IEmailJobRepository, EmailJobRepository>();
            services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
            services.AddScoped<IReportRepository, ReportRepository>();
            services.AddScoped<IVipPackageRepository, VipPackageRepository>();
            services.AddScoped<IStatisticsRepository, StatisticsRepository>();
            services.AddScoped<IStatisticBlogRepository, StatisticBlogRepository>();
            services.AddScoped<ITitleRepository, TitleRepository>();
            services.AddScoped<IGamificationService, GamificationService>();
            services.AddScoped<ITopicRepository, TopicRepository>();
            services.AddScoped<IQuestionTypeRepository, QuestionTypeRepository>();
            services.AddScoped<IPassageRepository, PassageRepository>();
            services.AddScoped<IQuestionBankRepository, QuestionBankRepository>();
            services.AddScoped<IQuestionOptionRepository, QuestionOptionRepository>();
            services.AddScoped<IExamTemplateRepository, ExamTemplateRepository>();
            services.AddScoped<ITemplatePartRepository, TemplatePartRepository>();
            services.AddScoped<IExamRepository, ExamRepository>();
            services.AddScoped<IExamQuestionRepository, ExamQuestionRepository>();
            services.AddScoped<IVocabularyRepository, VocabularyRepository>();
            services.AddScoped<IVocabularyTopicRepository, VocabularyTopicRepository>();
            services.AddScoped<ISocialLoginRepository, SocialLoginRepository>();
            services.AddScoped<IVocabularyExampleRepository, VocabularyExampleRepository>();
            services.AddScoped<IUserFavoriteVocabularyRepository, UserFavoriteVocabularyRepository>();
            services.AddScoped<IGameRepository, GameRepository>();
            services.AddScoped<IGameMatchSessionRepository, GameMatchSessionRepository>();
            services.AddScoped<IEmailHistoryRepository, EmailHistoryRepository>();
            services.AddScoped<IUserExamWritingAnswerRepository, UserExamWritingAnswerRepository>();
            services.AddScoped<IQuestion51Pipeline,
        Tokki.Infrastructure.Services.Gemini.Question51GeminiPipeline>();
            // Bạn cũng cần kiểm tra và đăng ký các Repository khác mà Command Handler đang yêu cầu:
            services.AddSingleton<IIdGeneratorService, IdGeneratorService>();
            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

            services.AddScoped<ISePayService, SePayService>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();

            //TextToSpeech
            services.AddScoped<ISpeechService, SpeechService>();
            //Cloudinary 
            services.AddScoped<ICloudinaryService, CloudinaryService>();
            //Comment
            services.AddScoped<ICommentRepository, CommentRepository>();
            //Live Chat
            services.AddScoped<IChatRepository, ChatRepository>();
            services.AddScoped<IChatRoomRepository, ChatRoomRepository>();
            services.AddScoped<IUserVocabProgressRepository, UserVocabProgressRepository>();
            //Mini game
            services.AddScoped<IMiniGameRepository, MiniGameRepository>();
            //Excel
            services.AddScoped<IExcelService, ExcelService>();
            //User topic progress
            services.AddScoped<IUserTopicProgressRepository, UserTopicProgressRepository>();
            // ===== Gemini TOPIK Writing =====
            services.Configure<Tokki.Infrastructure.Services.Gemini.GeminiOptions>(
                configuration.GetSection("Gemini"));

            services.AddHttpClient("Gemini", (sp, http) =>
            {
                var opt = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Tokki.Infrastructure.Services.Gemini.GeminiOptions>>().Value;
                http.BaseAddress = new Uri(opt.BaseUrl);
                http.Timeout = TimeSpan.FromSeconds(180);
            });

            services.AddHttpClient("ImageDownload", http =>
            {
                http.Timeout = TimeSpan.FromSeconds(60);
                http.DefaultRequestHeaders.Add("User-Agent", "Tokki-TOPIK-Service/1.0");
            });
            services.AddScoped<IQuestion52Pipeline, Question52GeminiPipeline>();
            services.AddScoped<IQuestion53Pipeline, Question53GeminiPipeline>();
            services.AddScoped<IQuestion54Pipeline, Question54GeminiPipeline>();

            services.AddScoped<Tokki.Infrastructure.Services.Gemini.GeminiRestClient>();
            services.AddScoped<ITopikWritingGeminiPipeline, Tokki.Infrastructure.Services.Gemini.TopikWritingGeminiPipeline>();
            //Pronunciation
            services.AddScoped<IPronunciationRuleRepository, PronunciationRuleRepository>();
            services.AddScoped<IAIPronunciationService, AIPronunciationService>();
            //User take exam
            services.AddScoped<IUserExamRepository, UserExamRepository>();
            return services;
        }
    }
}
