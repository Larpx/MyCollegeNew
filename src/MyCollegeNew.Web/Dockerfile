FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 复制项目文件并还原依赖
COPY ["MyCollegeNew.Shared/MyCollegeNew.Shared.csproj", "MyCollegeNew.Shared/"]
COPY ["MyCollegeNew.Infrastructure/MyCollegeNew.Infrastructure.csproj", "MyCollegeNew.Infrastructure/"]
COPY ["MyCollegeNew.ServiceDefaults/MyCollegeNew.ServiceDefaults.csproj", "MyCollegeNew.ServiceDefaults/"]
COPY ["MyCollegeNew.Web/MyCollegeNew.Web.csproj", "MyCollegeNew.Web/"]
COPY ["Directory.Build.props", "."]
RUN dotnet restore "MyCollegeNew.Web/MyCollegeNew.Web.csproj"

# 复制源代码并构建
COPY . .
RUN dotnet publish "MyCollegeNew.Web/MyCollegeNew.Web.csproj" -c Release -o /app/publish --no-restore

# 运行时镜像
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MyCollegeNew.Web.dll"]
