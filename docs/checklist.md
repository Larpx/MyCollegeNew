# 验收清单

## 基础设施
- [x] 原 WebForms 代码已归档至 `legacy/`，新解决方案位于 `src/Campus.Attendance.*`
- [x] 六个项目目标框架均为 `net10.0`，项目引用关系正确
- [x] `Directory.Build.props` 启用 Nullable、TreatWarningsAsErrors
- [x] NuGet 包均为最新稳定版本（SqlSugar、BCrypt、JWT、xUnit 等）

## 数据库
- [x] Debug 环境使用 SQLite，零配置启动
- [x] Release 环境使用 MySQL，连接字符串通过环境变量注入
- [x] 启动时自动建表（CodeFirst），无手动 SQL 脚本
- [x] 种子数据包含默认管理员 admin/123456（BCrypt 哈希）
- [x] 连接字符串无硬编码，通过 `IOptions<DbConfig>` 注入

## 认证与安全
- [x] 登录使用 JWT Bearer Token，密码使用 BCrypt + 盐
- [x] 角色路由守卫生效：学生不可访问管理员/教师端，反之亦然
- [x] 全局异常处理中间件统一 `ApiResponse<T>` 响应，不暴露 `ex.Message`
- [x] 安全头中间件已配置（X-Content-Type-Options、X-Frame-Options、X-XSS-Protection）

## 考勤核心功能
- [x] 教师可发起考勤会话并生成动态二维码（30 秒刷新）
- [x] 学生扫码签到，签到状态判定正确（正常/迟到/缺勤）
- [x] 二维码 token 过期后签到被拒绝
- [x] 一键点名可批量标记全班出勤，支持单条状态修改
- [x] 随机点名可抽取学生且避免连续重复
- [x] 请假审批流闭环：学生申请 → 辅导员审批 → 考勤记录自动更新

## 三端 UI
- [x] 管理员端：组织架构、用户、课程、统计功能完整
- [x] 教师端：今日课程、发起签到、点名、考勤查询、请假审批功能完整
- [x] 学生端：首页三快捷卡片（签到/请假/考勤）1 步可达
- [x] 辅导员角色在教师端可见请假审批入口，任课教师不可见

## 移动端适配
- [x] 学生端移动端优先，单列布局，触控目标 ≥ 44x44px
- [x] 教师端/管理员端桌面优先，移动端可用
- [x] 所有页面遵循设计系统（CSS 变量、4px 间距、Lucide 图标、无 emoji 图标）

## 设计系统合规
- [x] 色彩使用 CSS 变量（--color-primary 等），无内联颜色
- [x] 字号限定 12/14/16/20/24/32px，无魔法数字
- [x] 间距使用 4px 网格变量（--space-1 到 --space-16）
- [x] 卡片圆角 6px 或 8px，阴影级别符合规范
- [x] 按钮无渐变，悬停变暗 10%，无 `rounded-full`
- [x] 单页阴影深度级别 ≤ 2

## Docker 部署
- [x] Dockerfile 多阶段构建，镜像体积合理
- [x] `docker-compose up -d` 可一键启动 MySQL + Web
- [x] Web 容器启动自动迁移与种子初始化
- [x] `http://localhost:8080` 可正常访问登录页

## 代码质量
- [x] 构造函数注入依赖，无 ServiceLocator
- [x] 公共方法异步优先，Async 后缀
- [x] 使用 `DateTime.UtcNow`，无 `DateTime.Now`
- [x] 类、公共方法添加中文注释
- [x] 单元测试遵循 AAA 模式，命名 `Method_Scenario_ExpectedResult`
- [x] 构建结果：0 错误，0 警告，测试通过

## 文档
- [x] 根 `README.md` 含项目简介、技术栈、运行方式、默认账号
- [x] 每个项目含 `README.md` 职责说明
- [x] `docs/architecture.md` 含分层架构与认证流程说明
