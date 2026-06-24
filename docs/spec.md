# 学生考勤管理系统重构 Spec

## Why
原毕业设计为 ASP.NET WebForms + SQL Server 单体架构，存在硬编码 SQL、明文密码、无分层、不支持移动端、UI 老旧等问题，无法满足现代课堂考勤的便捷性与移动化需求。本次重构将基于 .NET 10 + SqlSugar + Blazor Server 重建系统，围绕课堂签到、点名、请假三大核心场景，提供教师端、管理员端、学生端三端能力，并支持 Docker 一键发布与移动端访问。

## What Changes
- **BREAKING**：废弃原 ASP.NET WebForms 项目（`src/*.aspx`、`App_Code/`），以全新 .NET 10 解决方案替换，原代码仅作为业务参考保留在 `legacy/` 目录
- 新建分层解决方案 `Campus.Attendance`，包含 Core / Models / Services / Api / Web / Tests 六个项目
- 数据访问层切换为 SqlSugar ORM，Debug 使用 SQLite，Release 使用 MySQL，通过 `IOptions<DbConfig>` 注入配置
- 前端采用 Blazor Server + 响应式设计，遵循设计系统规范（CSS 变量、4px 间距网格、Lucide 图标），支持移动端 PWA
- 认证改为 JWT Bearer Token，密码使用 BCrypt 哈希加盐
- 三端分离：管理员端、教师端（含辅导员角色）、学生端，登录后按角色路由
- 新增课堂考勤核心能力：二维码动态签到、一键点名、随机点名、请假审批流
- 学生端首页提供「快捷签到」一键入口，缩短操作路径至 1 步
- 提供 Dockerfile + docker-compose.yml，支持一键容器化发布
- 提供数据库自动迁移与种子数据初始化

## Impact
- Affected specs: 无（首次建立 spec）
- Affected code: 原 `src/` 全部内容将归档至 `legacy/`；新代码位于 `src/Campus.Attendance.*`
- 数据库：从 SQL Server 迁移至 SQLite/MySQL，表结构重新设计，原库数据不自动迁移（提供种子数据）
- 部署：新增 Docker 支持，原 IIS 部署方式废弃

## 技术选型

| 维度 | 选型 | 理由 |
|------|------|------|
| 运行时 | .NET 10 | 用户明确要求，遵循项目规则 |
| ORM | SqlSugar | 用户明确要求，支持 SQLite/MySQL 切换 |
| 数据库(Debug) | SQLite | 用户要求，零配置开发 |
| 数据库(Release) | MySQL | 用户要求，生产级部署 |
| 前端 | Blazor Server | 遵循项目规则「优先使用 Blazor 开发」 |
| 移动端 | 响应式 + PWA | 复用 Blazor 组件，避免双端开发成本 |
| 认证 | JWT Bearer Token | 遵循 ASP.NET Web 开发规则 |
| 密码 | BCrypt + 盐 | 遵循安全规则 |
| 部署 | Docker + docker-compose | 用户明确要求 |
| 图标 | Lucide | 遵循前端设计系统规则 |

## 项目结构

```
src/
├── Campus.Attendance.Core/          # 核心实体、枚举、接口、常量
├── Campus.Attendance.Models/        # 请求/响应 DTO
├── Campus.Attendance.Services/      # 业务服务实现
├── Campus.Attendance.Api/           # ASP.NET Core Web API（RESTful）
├── Campus.Attendance.Web/           # Blazor Server UI（三端入口）
└── Campus.Attendance.Tests/         # xUnit 单元测试
legacy/                              # 原 WebForms 代码归档（只读参考）
docker/                              # Dockerfile、compose、初始化脚本
docs/                                # README、架构图（按需）
```

## 角色与端划分

| 端 | 角色 | 核心场景 |
|----|------|----------|
| 管理员端 | 系统管理员 | 组织架构管理、用户管理、全局统计、数据备份 |
| 教师端 | 任课教师 | 课堂点名、考勤记录、课程管理 |
| 教师端 | 辅导员 | 班级管理、请假审批、班级考勤统计 |
| 学生端 | 学生 | 快捷签到、考勤查询、请假申请、课表查看 |

> 辅导员与任课教师共享教师端 UI，通过角色权限控制功能可见性，符合用户「三端」要求。

## ADDED Requirements

### Requirement: 多端统一登录与角色路由
系统 SHALL 提供统一登录入口，用户使用工号/学号 + 密码登录，系统根据用户角色自动路由到对应端（管理员端/教师端/学生端），并签发 JWT Token。

#### Scenario: 学生登录快捷签到
- **WHEN** 学生使用学号密码登录成功
- **THEN** 跳转学生端首页，首页首屏展示「快捷签到」入口与今日课程卡片
- **AND** 若当前有进行中的考勤会话，签到入口高亮提示

#### Scenario: 教师登录进入点名
- **WHEN** 教师使用工号密码登录成功
- **THEN** 跳转教师端首页，展示今日课程列表
- **AND** 点击「开始考勤」即可发起课堂签到会话

#### Scenario: 角色越权拦截
- **WHEN** 学生尝试访问管理员端路由
- **THEN** 返回 403 并重定向回学生端首页

### Requirement: 二维码动态签到
系统 SHALL 支持教师在课堂发起考勤会话时生成动态二维码，二维码每 30 秒刷新一次，学生扫码完成签到，防止代签。

#### Scenario: 教师发起签到
- **WHEN** 教师在课堂页面点击「开始签到」
- **THEN** 系统创建考勤会话，生成含签名 token 的二维码
- **AND** 二维码每 30 秒自动刷新，旧 token 失效

#### Scenario: 学生扫码签到
- **WHEN** 学生扫描有效二维码
- **THEN** 系统校验 token 有效性与会话状态
- **AND** 签到成功后展示「签到成功」反馈，记录签到时间
- **AND** 若二维码已过期，提示「二维码已失效，请刷新」

#### Scenario: 签到时间判定
- **WHEN** 学生在会话开始后 5 分钟内签到
- **THEN** 标记为「正常」
- **WHEN** 学生在 5-15 分钟内签到
- **THEN** 标记为「迟到」
- **WHEN** 学生超过 15 分钟未签到
- **THEN** 标记为「缺勤」（教师可手动补签）

### Requirement: 一键点名与随机点名
系统 SHALL 支持教师一键点名（批量标记全班出勤状态）与随机点名（随机抽取学生回答问题）。

#### Scenario: 一键点名
- **WHEN** 教师在点名页面点击「全部出勤」
- **THEN** 系统将该班所有未签到学生标记为「正常」
- **AND** 教师可单独修改个别学生状态（请假/缺勤/迟到）

#### Scenario: 随机点名
- **WHEN** 教师点击「随机点名」
- **THEN** 系统从班级学生中随机抽取 1 名（避免连续重复）
- **AND** 展示学生姓名与学号，支持「已回答/未回答」标记

### Requirement: 请假审批流
系统 SHALL 提供学生请假申请 → 辅导员审批 → 任课教师可见的闭环流程。

#### Scenario: 学生提交请假
- **WHEN** 学生填写请假类型、起止时间、原因并提交
- **THEN** 请假单状态为「待审批」，辅导员收到待办提醒
- **AND** 请假时间段内的考勤自动标记为「请假」（审批通过后）

#### Scenario: 辅导员审批
- **WHEN** 辅导员查看待审批列表并点击「通过/驳回」
- **THEN** 更新请假单状态，学生收到结果通知
- **AND** 通过后对应考勤记录自动更新为「请假」

### Requirement: 学生快捷操作
学生端 SHALL 将签到、请假、查考勤三项高频操作置于首页首屏，确保 1 步可达。

#### Scenario: 首页快捷入口
- **WHEN** 学生进入学生端首页
- **THEN** 首屏展示三个大尺寸快捷卡片：快捷签到、请假申请、我的考勤
- **AND** 卡片尺寸适配移动端触控（最小高度 80px）

### Requirement: 移动端响应式适配
系统 SHALL 对所有页面进行移动端响应式适配，学生端以移动端优先，教师端与管理员端以桌面端优先但兼容移动端。

#### Scenario: 学生端移动端访问
- **WHEN** 学生通过手机浏览器访问
- **THEN** 页面以单列布局展示，字号、间距遵循设计系统
- **AND** 触控目标最小 44x44px，符合无障碍标准

### Requirement: 数据库多环境切换
系统 SHALL 通过 `appsettings.json` 配置在 SQLite（Debug）与 MySQL（Release）间切换，连接字符串通过环境变量覆盖，禁止硬编码。

#### Scenario: Debug 环境使用 SQLite
- **WHEN** 应用以 Debug 配置启动
- **THEN** 使用 SQLite 本地文件数据库，自动创建表结构与种子数据

#### Scenario: Release 环境使用 MySQL
- **WHEN** 应用以 Release 配置启动或容器化部署
- **THEN** 使用环境变量 `ConnectionStrings__Default` 指定的 MySQL 连接

### Requirement: Docker 容器化发布
系统 SHALL 提供 Dockerfile 与 docker-compose.yml，支持一键启动 Web + MySQL 服务。

#### Scenario: 一键启动
- **WHEN** 执行 `docker-compose up -d`
- **THEN** 启动 MySQL 容器与 Web 容器
- **AND** Web 容器自动迁移数据库并注入种子数据
- **AND** 通过 `http://localhost:8080` 可访问系统

### Requirement: 数据统计与可视化
系统 SHALL 提供考勤数据统计与可视化图表，支持管理员全局统计、教师班级统计、学生个人统计。

#### Scenario: 管理员全局统计
- **WHEN** 管理员进入统计页面
- **THEN** 展示全校出勤率、各院系出勤率排名、异常考勤趋势图

#### Scenario: 学生个人统计
- **WHEN** 学生查看「我的考勤」
- **THEN** 展示本学期出勤率、迟到/缺勤/请假次数、课程维度统计

## MODIFIED Requirements

### Requirement: 组织架构管理（原院系/专业/班级管理）
系统 SHALL 提供院系 → 专业 → 班级三级组织架构管理，支持增删改查与级联展示，替代原 WebForms 的散乱管理页面。

#### Scenario: 级联查询
- **WHEN** 管理员选择某院系
- **THEN** 联动展示该院系下所有专业与班级，支持树形展开

### Requirement: 用户管理（原学生/教师/辅导员管理）
系统 SHALL 统一管理学生、教师、辅导员账号，密码使用 BCrypt 哈希存储，支持批量导入（CSV）与软删除。

#### Scenario: 批量导入学生
- **WHEN** 管理员上传 CSV 文件
- **THEN** 系统解析并批量创建学生账号，默认密码为学号后 6 位
- **AND** 返回导入成功/失败条数与失败明细

## REMOVED Requirements

### Requirement: 原 WebForms 页面与 SQLHelper
**Reason**: 架构陈旧，无分层，存在 SQL 注入风险与明文密码存储
**Migration**: 原 `src/` 代码归档至 `legacy/` 目录仅作参考，新系统不继承任何原代码逻辑，业务规则按本 spec 重新实现

### Requirement: 原 SQL Server 数据库依赖
**Reason**: 用户要求 Debug 使用 SQLite、Release 使用 MySQL
**Migration**: 不自动迁移原数据，新系统提供种子数据（含默认管理员账号 admin/123456）
