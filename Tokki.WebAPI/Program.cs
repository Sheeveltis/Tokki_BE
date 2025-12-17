// 1. THÊM CÁC NAMESPACE NÀY
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models; // Dùng cho Swagger
using System.Globalization;
using System.Text;
using Tokki.Application;
using Tokki.Application.Common.Helpers;
using Tokki.Infrastructure;
using Tokki.Infrastructure.BackgroundJobs; // Nơi chứa class JwtSettings
using Tokki.WebAPI.Hubs;
using Tokki.WebAPI.Middlewares;
var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. ĐĂNG KÝ SERVICES
// ==========================================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

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


// 4. CẤU HÌNH FLUENTVALIDATION TIẾNG VIỆT (THÊM PHẦN NÀY)
ValidatorOptions.Global.LanguageManager = new ValidationVietnameseLanguageManager();
ValidatorOptions.Global.LanguageManager.Enabled = true;
ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("vi");

// Đăng ký các layer khác (Giữ nguyên)
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplication();

// AutomationWorker
builder.Services.AddSingleton<Tokki.Infrastructure.BackgroundJobs.AutomationWorker>();
builder.Services.AddHostedService(provider =>
    provider.GetRequiredService<Tokki.Infrastructure.BackgroundJobs.AutomationWorker>());

builder.Services.AddSingleton<Tokki.Infrastructure.BackgroundJobs.CampaignWorker>();
builder.Services.AddHostedService(provider =>
    provider.GetRequiredService<Tokki.Infrastructure.BackgroundJobs.CampaignWorker>());


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
//SignalR
builder.Services.AddSignalR();
// ==========================================

var app = builder.Build();
//ChatHub
app.MapHub<ChatHub>("/chatHub");
app.MapHub<VocabularyHub>("/vocabularyHub");
app.UseMiddleware<GlobalExceptionMiddleware>();
// ==========================================
var supportedCultures = new[] { "vi" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("vi")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowReactApp");
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();