// 1. THÊM CÁC NAMESPACE NÀY
using FluentValidation;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models; // Dùng cho Swagger
using System.Globalization;
using System.Text;
using Tokki.Application;
using Tokki.Application.Common.Helpers;
using Tokki.Application.Common.Helpers.ValidationVietnameseLanguageManager;
using Tokki.Application.IServices;
using Tokki.Infrastructure;
using Tokki.Infrastructure.BackgroundJobs; // Nơi chứa class JwtSettings
using Tokki.Infrastructure.Configurations;
using Tokki.Infrastructure.Services;
using Tokki.WebAPI.BackgroundServices;
using Tokki.WebAPI.Hubs;
using Tokki.WebAPI.Middlewares;
using Tokki.WebAPI.Services;
using Tokki.Worker;
var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. ĐĂNG KÝ SERVICES
// ==========================================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
//Phần cho mấy mớ services thuộc webAPI 
builder.Services.AddSingleton<IChatNotificationService, ChatNotificationService>();

// 2. SỬA CẤU HÌNH SWAGGER (Để hiện nút ổ khóa)
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "Tokki API", Version = "v1" });

    // Định nghĩa nút "Authorize"
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Nhập token theo định dạng: Bearer {token}",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });

    // Yêu cầu bảo mật (Khi gọi API phải kèm token)
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });
});

// 3. CẤU HÌNH XÁC THỰC (AUTHENTICATION)
// Lấy cấu hình từ appsettings.json
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
var key = Encoding.UTF8.GetBytes(jwtSettings!.Secret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/chatHub")))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddHttpContextAccessor();


//Cấu hình Background Service
builder.Services.AddHostedService<ExamDeadlineWorker>();
builder.Services.AddHostedService<WordleGeneratorWorker>();
//Cấu hình cho tùy chọn apikey gemini 
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("Gemini"));
// 4. CẤU HÌNH FLUENTVALIDATION TIẾNG VIỆT (THÊM PHẦN NÀY)
ValidatorOptions.Global.LanguageManager = new ValidationVietnameseLanguageManager();
ValidatorOptions.Global.LanguageManager.Enabled = true;
ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("vi");

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});
// Đăng ký các layer khác (Giữ nguyên)
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplication();

builder.Services.AddHttpClient();
builder.Services.Configure<FacebookAuthSettings>(
    builder.Configuration.GetSection("FacebookAuth"));
// AutomationWorker
builder.Services.AddSingleton<Tokki.Infrastructure.BackgroundJobs.AutomationWorker>();
builder.Services.AddHostedService(provider =>
    provider.GetRequiredService<Tokki.Infrastructure.BackgroundJobs.AutomationWorker>());

builder.Services.AddSingleton<Tokki.Infrastructure.BackgroundJobs.CampaignWorker>();
builder.Services.AddHostedService(provider =>
    provider.GetRequiredService<Tokki.Infrastructure.BackgroundJobs.CampaignWorker>());
builder.Services.Configure<GoogleAuthSettings>(
    builder.Configuration.GetSection("Authentication:Google"));

builder.Services.AddHostedService<Tokki.Infrastructure.BackgroundJobs.VipExpirationWorker>();

builder.Services.AddHttpClient<IAiRoadmapService, AiRoadmapService>();

builder.Services.AddScoped<IUserRoadmapRepository, UserRoadmapRepository>();
builder.Services.AddScoped<IExamAssemblyService, ExamAssemblyService>();


builder.Services.AddMemoryCache(options =>
{
    //options.SizeLimit = 1024; // Giới hạn 1024 entries
    options.CompactionPercentage = 0.25; // Khi đầy, xóa 25% entries cũ nhất
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000",
               "https://localhost:7000",          // API itself (cho SignalR)
                  "https://localhost:7178") // Cho phép đúng cái Frontend của bạn
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMobile", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ===== HANGFIRE CONFIGURATION =====
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero,
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        }));
// Add Hangfire server
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 5; // Số worker chạy đồng thời (tùy chỉnh theo server)
});

//SignalR
builder.Services.AddSignalR();

// ==========================================

var app = builder.Build();
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() },
    DashboardTitle = "Tokki Background Jobs"
});
//ChatHub
app.MapHub<ChatHub>("/chatHub");
app.MapHub<VocabularyHub>("/vocabularyHub");
app.UseMiddleware<GlobalExceptionMiddleware>();
//mobile
//app.Urls.Add("http://0.0.0.0:5031");
app.UseCors("AllowMobile");
// ==========================================
var supportedCultures = new[] { "vi" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("vi")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseCors("AllowReactApp");
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();