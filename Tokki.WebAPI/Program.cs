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

// ==========================================
var app = builder.Build();
// ==========================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();