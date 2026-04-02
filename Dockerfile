# ================================
# Stage 1: Base runtime image
# ================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# ================================
# Stage 2: Build
# ================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy từng .csproj trước để Docker cache layer restore NuGet
# (Nếu code thay đổi nhưng .csproj không đổi → bỏ qua bước restore → build nhanh hơn)
COPY ["Tokki.WebAPI/Tokki.WebAPI.csproj", "Tokki.WebAPI/"]
COPY ["Tokki.Application/Tokki.Application.csproj", "Tokki.Application/"]
COPY ["Tokki.Domain/Tokki.Domain.csproj", "Tokki.Domain/"]
COPY ["Tokki.Infrastructure/Tokki.Infrastructure.csproj", "Tokki.Infrastructure/"]

# Restore NuGet packages
RUN dotnet restore "Tokki.WebAPI/Tokki.WebAPI.csproj"

# Copy toàn bộ source code
COPY . .

# Build Release
WORKDIR "/src/Tokki.WebAPI"
RUN dotnet build "Tokki.WebAPI.csproj" -c Release -o /app/build

# ================================
# Stage 3: Publish
# ================================
FROM build AS publish
RUN dotnet publish "Tokki.WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ================================
# Stage 4: Final image (chỉ chứa runtime, không có SDK → image nhỏ gọn)
# ================================
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tokki.WebAPI.dll"]