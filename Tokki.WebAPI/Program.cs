using Tokki.Infrastructure; // Để dùng AddInfrastructureServices
using Tokki.Application;    // Để dùng AddApplicationServices
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. ĐĂNG KÝ SERVICES (GỌN GÀNG)
// ==========================================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplication();
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
// ==========================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("AllowReactApp");
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();