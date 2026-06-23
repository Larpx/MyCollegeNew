# ============================================================
# Campus.Attendance.Api Dockerfile（RESTful API 后端）
# 多阶段构建：sdk 阶段编译发布，aspnet 阶段运行
# 构建上下文为解决方案根目录（docker-compose context: .）
# ============================================================

# ---- 阶段1：构建 ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /build

# 先复制 Directory.Build.props 与项目文件，利用 Docker 层缓存加速 restore
COPY Directory.Build.props ./
COPY src/Campus.Attendance.Core/Campus.Attendance.Core.csproj src/Campus.Attendance.Core/
COPY src/Campus.Attendance.Models/Campus.Attendance.Models.csproj src/Campus.Attendance.Models/
COPY src/Campus.Attendance.Services/Campus.Attendance.Services.csproj src/Campus.Attendance.Services/
COPY src/Campus.Attendance.Api/Campus.Attendance.Api.csproj src/Campus.Attendance.Api/

# 还原 NuGet 依赖
RUN dotnet restore src/Campus.Attendance.Api/Campus.Attendance.Api.csproj

# 复制全部源代码并发布
COPY src/ src/
RUN dotnet publish src/Campus.Attendance.Api/Campus.Attendance.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ---- 阶段2：运行时 ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# 安装 curl 用于容器健康检查
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# 暴露 API 监听端口
EXPOSE 8080

# 复制发布产物
COPY --from=build /app/publish .

# 入口点
ENTRYPOINT ["dotnet", "Campus.Attendance.Api.dll"]
