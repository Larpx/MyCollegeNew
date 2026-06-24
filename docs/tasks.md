# Tasks

## 阶段一：项目骨架与基础设施

- [x] Task 1: 归档原 WebForms 代码并搭建 .NET 10 解决方案骨架
  - [x] SubTask 1.1: 将 `src/` 原 WebForms 代码移动至 `legacy/` 目录
  - [x] SubTask 1.2: 在 `src/` 下创建 `Campus.Attendance.Core`、`Models`、`Services`、`Api`、`Web`、`Tests` 六个项目，目标框架 `net10.0`
  - [x] SubTask 1.3: 配置解决方案级 `Directory.Build.props` 统一 LangVersion、Nullable、TreatWarningsAsErrors
  - [x] SubTask 1.4: 添加各项目引用关系（Core ← Models ← Services ← Api/Web，Tests 引用 Services）

- [x] Task 2: 配置 SqlSugar 多数据库支持
  - [x] SubTask 2.1: 在 `Core` 中定义 `IDbContext` 接口与 `DbConfig` 配置类
  - [x] SubTask 2.2: 在 `Services` 中实现 `SqlSugarDbContext`，根据配置创建 SQLite（Debug）或 MySQL（Release）连接
  - [x] SubTask 2.3: 配置 `appsettings.Development.json`（SQLite）与 `appsettings.json`（MySQL 占位）
  - [x] SubTask 2.4: 注册依赖注入，连接字符串支持环境变量 `ConnectionStrings__Default` 覆盖

- [x] Task 3: 实现数据库实体与自动迁移
  - [x] SubTask 3.1: 在 `Core/Entities` 下定义实体：Department、Major、Class、Student、Teacher、Course、CourseSchedule、AttendanceSession、AttendanceRecord、LeaveRequest、SystemUser、AuditLog
  - [Task 3.1 实体清单]：使用 SqlSugar 特性标注主键、自增、软删除字段（IsDeleted）、时间戳（CreateTime/UpdateTime）
  - [x] SubTask 3.2: 实现数据库初始化器 `DbInitializer`，启动时自动建表（CodeFirst）
  - [x] SubTask 3.3: 实现种子数据：默认管理员 admin/123456（BCrypt）、示例院系/专业/班级/课程
  - [x] SubTask 3.4: 编写实体字段单元测试，验证特性标注正确

## 阶段二：认证与用户管理

- [x] Task 4: 实现 JWT 认证与权限体系
  - [x] SubTask 4.1: 在 `Core` 中定义 `UserRole` 枚举（Admin/Teacher/Counselor/Student）与 `ICurrentUser` 接口
  - [x] SubTask 4.2: 在 `Services` 实现 `AuthService`：登录校验（BCrypt）、JWT 签发、Token 刷新
  - [x] SubTask 4.3: 在 `Api` 配置 JWT Bearer 认证中间件与基于角色的授权策略
  - [x] SubTask 4.4: 实现全局异常处理中间件，统一 `ApiResponse<T>` 响应包装
  - [x] SubTask 4.5: 编写 `AuthService` 单元测试（登录成功/失败/角色路由）

- [x] Task 5: 实现用户管理服务与 API
  - [x] SubTask 5.1: 在 `Models` 定义用户相关 DTO（CreateRequestDto/UpdateRequestDto/ResponseDto）
  - [x] SubTask 5.2: 在 `Services` 实现 `UserService`：学生/教师/辅导员的 CRUD、批量导入 CSV、软删除
  - [x] SubTask 5.3: 在 `Api` 实现用户管理 RESTful 端点（`api/users`、`api/students`、`api/teachers`），标注 `[Authorize(Roles="Admin")]`
  - [x] SubTask 5.4: 实现密码修改端点，校验旧密码
  - [x] SubTask 5.5: 编写 `UserService` 单元测试（CRUD、批量导入、软删除）

## 阶段三：组织架构与课程管理

- [x] Task 6: 实现组织架构管理（院系/专业/班级）
  - [x] SubTask 6.1: 在 `Services` 实现 `OrganizationService`：院系→专业→班级级联 CRUD
  - [x] SubTask 6.2: 在 `Api` 实现组织架构端点（`api/departments`、`api/majors`、`api/classes`）
  - [x] SubTask 6.3: 实现级联查询端点（按院系查专业、按专业查班级）
  - [x] SubTask 6.4: 编写 `OrganizationService` 单元测试

- [x] Task 7: 实现课程与课表管理
  - [x] SubTask 7.1: 在 `Services` 实现 `CourseService`：课程 CRUD、教师关联
  - [x] SubTask 7.2: 在 `Services` 实现 `ScheduleService`：排课（班级-课程-教师-周次-节次）
  - [x] SubTask 7.3: 在 `Api` 实现课程与课表端点（`api/courses`、`api/schedules`）
  - [x] SubTask 7.4: 实现按教师/学生/班级查询课表端点
  - [x] SubTask 7.5: 编写 `CourseService`、`ScheduleService` 单元测试

## 阶段四：考勤核心功能

- [x] Task 8: 实现考勤会话与签到服务
  - [x] SubTask 8.1: 在 `Services` 实现 `AttendanceService`：创建考勤会话（关联课程+班级+教师+时间）
  - [x] SubTask 8.2: 实现动态二维码生成：含签名 token（JWT 短期 token，30 秒过期），返回 Base64 图片
  - [x] SubTask 8.3: 实现学生签到逻辑：校验 token、判定签到状态（正常/迟到/缺勤）、写入 `AttendanceRecord`
  - [x] SubTask 8.4: 实现一键点名（批量标记全班出勤）与单条状态修改
  - [x] SubTask 8.5: 实现随机点名（避免连续重复，记录回答状态）
  - [x] SubTask 8.6: 在 `Api` 实现考勤端点（`api/sessions`、`api/sessions/{id}/qrcode`、`api/sessions/{id}/checkin`、`api/sessions/{id}/roll-call`）
  - [x] SubTask 8.7: 编写 `AttendanceService` 单元测试（签到状态判定、token 过期、一键点名）

- [x] Task 9: 实现请假审批流服务
  - [x] SubTask 9.1: 在 `Services` 实现 `LeaveService`：学生提交请假、辅导员审批（通过/驳回）
  - [x] SubTask 9.2: 实现审批通过后自动更新对应考勤记录为「请假」
  - [x] SubTask 9.3: 实现请假待办提醒（辅导员首页展示待审批数）
  - [x] SubTask 9.4: 在 `Api` 实现请假端点（`api/leaves`、`api/leaves/{id}/approve`）
  - [x] SubTask 9.5: 编写 `LeaveService` 单元测试（提交、审批、考勤联动）

- [x] Task 10: 实现考勤统计与报表
  - [x] SubTask 10.1: 在 `Services` 实现 `StatisticsService`：按院系/班级/课程/学生维度统计出勤率
  - [x] SubTask 10.2: 实现管理员全局统计（全校出勤率、院系排名、异常趋势）
  - [x] SubTask 10.3: 实现学生个人统计（本学期出勤率、迟到/缺勤/请假次数）
  - [x] SubTask 10.4: 实现报表导出（CSV/Excel，使用 EPPlusCore 或 ClosedXML）
  - [x] SubTask 10.5: 在 `Api` 实现统计端点（`api/statistics/overview`、`api/statistics/student/{id}`）
  - [x] SubTask 10.6: 编写 `StatisticsService` 单元测试

## 阶段五：Blazor Web UI 实现

- [x] Task 11: 搭建 Blazor Server 项目骨架与设计系统
  - [x] SubTask 11.1: 创建 `Campus.Attendance.Web` Blazor Server 项目，配置认证与 HttpClient 调用 Api
  - [x] SubTask 11.2: 定义设计系统 CSS 变量（色彩、排版、间距、阴影），遵循前端设计系统规则
  - [x] SubTask 11.3: 实现基础布局组件：`MainLayout`、`NavMenu`、`PageHeader`、`Card`、`Button`、`Input`、`Table`
  - [x] SubTask 11.4: 引入 Lucide 图标库，封装 `Icon` 组件
  - [x] SubTask 11.5: 实现响应式断点（移动端/平板/桌面），移动端抽屉式导航

- [x] Task 12: 实现登录页与角色路由
  - [x] SubTask 12.1: 实现 `Login.razor` 页面（学号/工号 + 密码），调用 `api/auth/login`
  - [x] SubTask 12.2: 实现 `AuthStateProvider`，存储 JWT Token，根据角色重定向
  - [x] SubTask 12.3: 实现 `AuthorizeRouteView` 角色路由守卫（Admin/Teacher/Student 区域隔离）
  - [x] SubTask 12.4: 实现登出逻辑，清除 Token

- [x] Task 13: 实现管理员端页面
  - [x] SubTask 13.1: 实现 `Admin/Dashboard.razor`（全局统计卡片 + 图表）
  - [x] SubTask 13.2: 实现 `Admin/Departments.razor`（院系/专业/班级树形管理）
  - [x] SubTask 13.3: 实现 `Admin/Students.razor`、`Admin/Teachers.razor`（用户管理 + CSV 导入）
  - [x] SubTask 13.4: 实现 `Admin/Courses.razor`（课程管理）
  - [x] SubTask 13.5: 实现 `Admin/Statistics.razor`（全局统计与报表导出）

- [x] Task 14: 实现教师端页面
  - [x] SubTask 14.1: 实现 `Teacher/Dashboard.razor`（今日课程 + 待审批请假数）
  - [x] SubTask 14.2: 实现 `Teacher/Courses.razor`（我的课程与课表）
  - [x] SubTask 14.3: 实现 `Teacher/Session.razor`（发起签到、二维码展示、实时签到列表）
  - [x] SubTask 14.4: 实现 `Teacher/RollCall.razor`（一键点名、随机点名、状态修改）
  - [x] SubTask 14.5: 实现 `Teacher/Attendance.razor`（考勤记录查询与导出）
  - [x] SubTask 14.6: 实现 `Teacher/Leaves.razor`（辅导员请假审批，仅辅导员可见）

- [x] Task 15: 实现学生端页面（移动端优先）
  - [x] SubTask 15.1: 实现 `Student/Home.razor`（首屏三快捷卡片：签到/请假/考勤 + 今日课程）
  - [x] SubTask 15.2: 实现 `Student/CheckIn.razor`（扫码签到，调用摄像头或输入签到码）
  - [x] SubTask 15.3: 实现 `Student/Attendance.razor`（我的考勤记录与统计）
  - [x] SubTask 15.4: 实现 `Student/Leave.razor`（请假申请与历史记录）
  - [x] SubTask 15.5: 实现 `Student/Schedule.razor`（我的课表）
  - [x] SubTask 15.6: 实现 `Student/Profile.razor`（个人信息与密码修改）

## 阶段六：Docker 化与发布

- [x] Task 16: 实现 Docker 容器化部署
  - [x] SubTask 16.1: 编写 `Campus.Attendance.Api` 与 `Campus.Attendance.Web` 的多阶段 Dockerfile（net10.0 SDK 构建 + runtime 运行）
  - [x] SubTask 16.2: 编写 `docker-compose.yml`（mysql + web 服务，含健康检查与依赖顺序）
  - [x] SubTask 16.3: 配置 Web 容器启动时自动迁移数据库与种子初始化
  - [x] SubTask 16.4: 编写 `.dockerignore` 与 `docker/README.md` 部署说明
  - [ ] SubTask 16.5: 验证 `docker-compose up -d` 可正常启动并访问 `http://localhost:8080`（环境不支持 Docker，跳过实际运行验证）

## 阶段七：文档与验收

- [x] Task 17: 编写项目文档
  - [x] SubTask 17.1: 编写根 `README.md`（项目简介、技术栈、本地运行、Docker 部署、默认账号）
  - [x] SubTask 17.2: 为每个项目编写 `README.md`（职责说明、关键类型）
  - [x] SubTask 17.3: 编写 `docs/architecture.md` 架构说明（分层、数据流、认证流程）

# Task Dependencies
- Task 2 依赖 Task 1（项目骨架）
- Task 3 依赖 Task 2（数据库配置）
- Task 4 依赖 Task 3（实体定义）
- Task 5、6、7 依赖 Task 4（认证授权）
- Task 8、9、10 依赖 Task 7（课程课表）
- Task 11 依赖 Task 4（认证）
- Task 12 依赖 Task 11
- Task 13、14、15 依赖 Task 12 + 对应后端 Task（5-10）
- Task 16 依赖 Task 15（全部功能完成）
- Task 17 依赖 Task 16

# 可并行任务
- Task 5、6、7 可并行（均依赖 Task 4）
- Task 8、9、10 可并行（均依赖 Task 7）
- Task 13、14、15 可并行（均依赖 Task 12 + 后端 API）
