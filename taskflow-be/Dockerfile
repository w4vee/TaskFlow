# Stage 1: Build (Bên trong container này có chứa bộ SDK nặng để build code)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Sao chép các file cấu trúc dự án vào trước để tải thư viện (giúp tận dụng cache của Docker, chạy nhanh hơn ở những lần sau)
COPY ["TaskFlow.sln", "./"]
COPY ["TaskFlow.API/TaskFlow.API.csproj", "TaskFlow.API/"]
COPY ["TaskFlow.Application/TaskFlow.Application.csproj", "TaskFlow.Application/"]
COPY ["TaskFlow.Domain/TaskFlow.Domain.csproj", "TaskFlow.Domain/"]
COPY ["TaskFlow.Infrastructure/TaskFlow.Infrastructure.csproj", "TaskFlow.Infrastructure/"]
COPY ["TaskFlow.Tests/TaskFlow.Tests.csproj", "TaskFlow.Tests/"]

# Tải các thư viện (Nuget packages)
RUN dotnet restore "TaskFlow.sln"

# Sao chép toàn bộ code còn lại vào container
COPY . .

# Build dự án API ra thư mục /app/publish
WORKDIR "/src/TaskFlow.API"
RUN dotnet publish "TaskFlow.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Run (Tạo ra một container mới siêu nhẹ, chỉ chứa Runtime để chạy app, không chứa mã nguồn)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080

# Copy file đã build từ Stage 1 sang Stage 2
COPY --from=build /app/publish .

# Lệnh khởi chạy ứng dụng khi container bắt đầu
ENTRYPOINT ["dotnet", "TaskFlow.API.dll"]