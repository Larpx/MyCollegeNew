FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 复制项目文件并还原依赖
COPY ["Campus.Attendance.Shared/Campus.Attendance.Shared.csproj", "Campus.Attendance.Shared/"]
COPY ["Campus.Attendance.Infrastructure/Campus.Attendance.Infrastructure.csproj", "Campus.Attendance.Infrastructure/"]
COPY ["Campus.Attendance.ServiceDefaults/Campus.Attendance.ServiceDefaults.csproj", "Campus.Attendance.ServiceDefaults/"]
COPY ["Campus.Attendance.Web/Campus.Attendance.Web.csproj", "Campus.Attendance.Web/"]
COPY ["Directory.Build.props", "."]
RUN dotnet restore "Campus.Attendance.Web/Campus.Attendance.Web.csproj"

# 复制源代码并构建
COPY . .
RUN dotnet publish "Campus.Attendance.Web/Campus.Attendance.Web.csproj" -c Release -o /app/publish --no-restore

# 运行时镜像
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Campus.Attendance.Web.dll"]
