# Docker 部署说明

## 前置要求

- [Docker](https://docs.docker.com/get-docker/) 24.0+
- [Docker Compose](https://docs.docker.com/compose/install/) v2.20+

## 架构概览

容器编排包含三个服务：

| 服务 | 镜像 | 端口 | 说明 |
|------|------|------|------|
| `db` | mysql:8.0 | 3306 | MySQL 数据库，数据持久化至命名卷 |
| `api` | dotnet/aspnet:10.0 | 5000 → 8080 | RESTful API 后端，启动时自动建表与播种 |
| `web` | dotnet/aspnet:10.0 | 8080 → 8080 | Blazor Server 前端，通过 HttpClient 调用 api |

启动顺序：`db`（健康检查通过）→ `api`（健康检查通过）→ `web`。

## 快速启动

在项目根目录执行：

```bash
docker-compose up -d --build
```

首次构建约需 3-5 分钟（还原 NuGet + 编译发布）。启动完成后访问：

```
http://localhost:8080
```

## 默认账号

| 角色 | 用户名 | 密码 | 说明 |
|------|--------|------|------|
| 管理员 | admin | 123456 | 系统管理员，拥有全部权限 |
| 任课教师 | T001 | 123456 | 示例任课教师（张老师） |
| 辅导员 | T002 | 123456 | 示例辅导员（李老师） |
| 学生 | 20220101 | 220101 | 示例学生（王同学），密码为学号后 6 位 |

## 环境变量说明

### db 服务

| 变量 | 默认值 | 说明 |
|------|--------|------|
| `MYSQL_ROOT_PASSWORD` | root | MySQL root 密码 |
| `MYSQL_DATABASE` | attendance | 自动创建的数据库名 |

### api 服务

| 变量 | 默认值 | 说明 |
|------|--------|------|
| `Db__ProviderType` | MySQL | 数据库提供程序（MySQL / SQLite） |
| `Db__ConnectionString` | Server=db;Port=3306;... | 数据库连接字符串，`db` 为容器服务名 |
| `Jwt__SecretKey` | Campus.Attendance.SecretKey... | JWT 签名密钥，生产环境务必修改 |
| `Jwt__Issuer` | Campus.Attendance.Api | JWT 签发者 |
| `Jwt__Audience` | Campus.Attendance.Client | JWT 受众 |
| `ASPNETCORE_ENVIRONMENT` | Production | 运行环境 |
| `ASPNETCORE_URLS` | http://+:8080 | 监听地址 |

### web 服务

| 变量 | 默认值 | 说明 |
|------|--------|------|
| `Api__BaseUrl` | http://api:8080 | 后端 API 基址，`api` 为容器服务名 |
| `ASPNETCORE_ENVIRONMENT` | Production | 运行环境 |
| `ASPNETCORE_URLS` | http://+:8080 | 监听地址 |

## 数据持久化

MySQL 数据通过 Docker 命名卷 `mysql_data` 持久化。容器删除后数据不会丢失。

查看数据卷：

```bash
docker volume inspect my-college-project_mysql_data
```

## 自动迁移

`api` 容器启动时，`Program.cs` 会自动执行：

1. `DbInitializer.InitializeAsync()` — SqlSugar CodeFirst 自动建表
2. `DbInitializer.SeedAsync()` — 播种默认管理员、示例院系/专业/班级/教师/学生/课程

无需手动执行迁移命令。

## 常用命令

### 启动

```bash
# 后台启动并构建
docker-compose up -d --build

# 查看日志
docker-compose logs -f

# 仅查看某服务日志
docker-compose logs -f api
docker-compose logs -f web
```

### 停止

```bash
# 停止容器（保留数据）
docker-compose stop

# 停止并删除容器（保留数据卷）
docker-compose down
```

### 完全清理（含数据）

```bash
# 停止并删除容器、网络、数据卷
docker-compose down -v

# 清理构建镜像
docker-compose build --no-cache
```

### 重新构建

```bash
# 修改代码后重新构建并启动
docker-compose up -d --build api web
```

## 生产环境注意事项

1. **修改 JWT 密钥**：编辑 `docker-compose.yml` 中 `api` 服务的 `Jwt__SecretKey`，使用至少 32 字符的随机字符串
2. **修改 MySQL 密码**：编辑 `db` 服务的 `MYSQL_ROOT_PASSWORD` 和 `api` 服务的 `Db__ConnectionString`
3. **移除端口暴露**：生产环境建议移除 `db` 服务的 `ports` 映射，仅保留内部网络通信
4. **配置反向代理**：建议在 `web` 服务前配置 Nginx 反向代理，启用 HTTPS
5. **资源限制**：根据服务器配置添加 `deploy.resources.limits` 限制 CPU 和内存

## 故障排查

### 容器启动失败

```bash
# 查看容器状态
docker-compose ps

# 查看失败容器日志
docker-compose logs api
docker-compose logs web
```

### 数据库连接失败

1. 确认 `db` 容器健康：`docker-compose ps db` 状态应为 `healthy`
2. 确认连接字符串中 `Server=db` 与服务名一致
3. 查看 `api` 日志确认 SqlSugar 初始化信息

### Web 无法访问 API

1. 确认 `api` 容器健康：`docker-compose ps api` 状态应为 `healthy`
2. 确认 `web` 服务的 `Api__BaseUrl=http://api:8080` 与 `api` 服务名一致
3. 进入 web 容器测试：`docker exec attendance-web curl -s http://api:8080/`
