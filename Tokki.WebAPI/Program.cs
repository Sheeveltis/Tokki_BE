// 1. THÊM CÁC NAMESPACE NÀY
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models; // Dùng cho Swagger
using System.Text;
using Tokki.Application;
using Tokki.Application.Common.Helpers;
using Tokki.Infrastructure;
using Tokki.Infrastructure.BackgroundJobs; // Nơi chứa class JwtSettings
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

        // Quan trọng: Set ClockSkew về 0 để token hết hạn đúng chính xác từng giây
        ClockSkew = TimeSpan.Zero
    };
});


// Đăng ký các layer khác (Giữ nguyên)
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddSingleton<Tokki.Infrastructure.BackgroundJobs.AutomationWorker>();
builder.Services.AddHostedService(provider =>
    provider.GetRequiredService<Tokki.Infrastructure.BackgroundJobs.AutomationWorker>());

// CampaignWorker (chỉ chạy background, không cần inject)
builder.Services.AddHostedService<Tokki.Infrastructure.BackgroundJobs.CampaignWorker>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // Cho phép đúng cái Frontend của bạn
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
// ==========================================

var app = builder.Build();
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