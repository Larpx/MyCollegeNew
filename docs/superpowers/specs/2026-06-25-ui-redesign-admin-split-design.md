# UI 重构与项目拆分设计方案

> 日期: 2026-06-25
> 状态: 待实施
> 分支: feat/ui-redesign-admin-split

## 1. 目标

1. 将 Web 项目拆分为 **用户端**（教师+学生，响应式）和 **管理员端**（仅桌面，可独立部署内网）
2. 全面重写 UI 样式，采用现代设计语言，兼具科技感与人文温度，避免 AI 味
3. 用户端支持响应式布局（手机 + 平板 + 桌面），管理员端仅桌面

## 2. 项目架构

```
src/
├── MyCollegeNew.Api/              # 共享 API（不变）
├── MyCollegeNew.Web/              # 用户端 - 教师 + 学生（响应式）
├── MyCollegeNew.Admin/            # 管理员端（仅桌面，独立部署）
├── MyCollegeNew.Shared/           # 共享 DTO / Entities / Enums
├── MyCollegeNew.Infrastructure/   # 数据访问 / Auth / Token
├── MyCollegeNew.ServiceDefaults/  # Aspire 服务默认配置
└── MyCollegeNew.Tests/
```

### 2.1 MyCollegeNew.Web（用户端）

| 属性 | 值 |
|------|-----|
| 端口 | 5249 |
| 目标用户 | 教师、辅导员、学生 |
| 响应式 | 手机（<768px）、平板（768-1024px）、桌面（>1024px） |
| 页面 | 登录、教师仪表盘、我的课程、考勤记录、点名、请假审批、学生首页、签到、我的考勤、请假申请、课表、个人信息 |
| 布局 | 桌面：左侧侧边栏 + 顶部栏；手机：底部 Tab 导航 |

### 2.2 MyCollegeNew.Admin（管理员端）

| 属性 | 值 |
|------|-----|
| 端口 | 5250 |
| 目标用户 | 系统管理员 |
| 响应式 | 仅桌面（>1024px），窗口过小显示提示 |
| 页面 | 登录、仪表盘、院系管理、学生管理、教师管理、课程管理、统计报表 |
| 布局 | 左侧深色侧边栏（260px）+ 顶部栏 + 内容区 |

### 2.3 拆分策略

- `MyCollegeNew.Admin` 从 `MyCollegeNew.Web` 独立创建，共享 `MyCollegeNew.Shared` / `MyCollegeNew.Infrastructure`
- 两个前端项目各自拥有独立的 `Program.cs`、`wwwroot/`、`Components/`
- API 由 `MyCollegeNew.Api` 统一提供（端口 5144）
- 管理员端可部署于内网，与公网逻辑隔离

## 3. 设计理念

### 3.1 核心原则

| 原则 | 描述 |
|------|------|
| **呼吸感** | 慷慨的留白，不拥挤。内容区块间用间距而非分割线 |
| **层次感** | 通过 elevation（z-index 分层）+ 微妙阴影 + 毛玻璃效果建立视觉深度 |
| **流畅感** | 每一个交互都有 150-250ms 的 ease-out 过渡，无突兀跳变 |
| **精致感** | 圆角统一但有层次，渐变柔和不刺眼，图标与文字对齐像素级 |
| **克制感** | 每页最多 2 种强调色，3 级阴影深度，不使用 emoji 作为功能图标 |

### 3.2 设计参考

灵感来源于：Linear、Vercel、Raycast、Notion Calendar、Arc Browser

## 4. 视觉风格

### 4.1 色彩系统

```css
:root {
    /* ======== 主色 - Iris 紫蓝渐变系 ======== */
    --color-primary: #818cf8;                    /* 主按钮 / 链接 */
    --color-primary-hover: #6366f1;              /* 悬停态 */
    --color-primary-pressed: #4f46e5;            /* 按压态 */
    --color-primary-subtle: #eef2ff;             /* 浅底强调 */
    --color-primary-gradient: linear-gradient(135deg, #818cf8 0%, #a78bfa 50%, #c084fc 100%);
    --color-primary-glow: 0 0 20px rgba(129, 140, 248, 0.25);

    /* ======== 语义色 ======== */
    --color-success: #22c55e;
    --color-success-subtle: #f0fdf4;
    --color-warning: #f59e0b;
    --color-warning-subtle: #fffbeb;
    --color-error: #f43f5e;                      /* 暖红，不刺眼 */
    --color-error-subtle: #fff1f2;

    /* ======== 中性分层 ======== */
    --color-bg: #f9fafb;                         /* 页面底色 */
    --color-surface: #ffffff;                    /* 卡片/面板底色 */
    --color-surface-hover: #f8fafc;              /* 悬停态 */
    --color-surface-raised: #ffffff;             /* 浮层底色 */
    --color-border: #e5e7eb;                     /* 默认边框 */
    --color-border-light: #f3f4f6;               /* 浅边框 */
    --color-border-focus: #a5b4fc;               /* 聚焦边框 */

    /* ======== 文字层次 ======== */
    --color-text-primary: #0f172a;               /* 标题 + 正文 */
    --color-text-secondary: #475569;             /* 辅助说明 */
    --color-text-tertiary: #94a3b8;              /* 占位 / 禁用 */
    --color-text-inverse: #ffffff;               /* 深色背景上的文字 */

    /* ======== 管理端侧边栏 ======== */
    --sidebar-bg: #0f0d1e;
    --sidebar-text: #a5b4fc;
    --sidebar-text-dim: #6366a0;
    --sidebar-active-bg: rgba(129, 140, 248, 0.12);
    --sidebar-active-text: #c7d2fe;
    --sidebar-border: rgba(129, 140, 248, 0.08);
}
```

### 4.2 圆角系统

| 令牌 | 值 | 用途 |
|------|-----|------|
| `--radius-xs` | 4px | 小标签、代码块 |
| `--radius-sm` | 8px | 输入框、下拉菜单 |
| `--radius-md` | 12px | 按钮、小卡片 |
| `--radius-lg` | 16px | 卡片、面板 |
| `--radius-xl` | 24px | 大卡片、弹窗 |
| `--radius-full` | 9999px | 药丸标签、头像 |

### 4.3 阴影层次（elevation 体系）

```css
--shadow-raised:  0 1px 2px rgba(0, 0, 0, 0.04), 0 1px 3px rgba(0, 0, 0, 0.06);
--shadow-overlay: 0 4px 6px rgba(0, 0, 0, 0.04), 0 2px 16px rgba(0, 0, 0, 0.08);
--shadow-modal:   0 8px 8px rgba(0, 0, 0, 0.04), 0 4px 32px rgba(0, 0, 0, 0.12);
--shadow-glow:    0 0 0 4px rgba(129, 140, 248, 0.15);
```

### 4.4 排版升级

```css
--font-family: "Inter", "SF Pro Text", -apple-system, "PingFang SC",
    "Microsoft YaHei", "Noto Sans SC", sans-serif;
--font-family-mono: "JetBrains Mono", "SF Mono", "Cascadia Code", monospace;

/* 字号：黄金比例阶梯 */
--text-xs:  0.75rem;     /* 12px — 角标、标签 */
--text-sm:  0.875rem;    /* 14px — 辅助文字、表格 */
--text-base: 1rem;       /* 16px — 正文 */
--text-lg:  1.125rem;    /* 18px — 段落标题 */
--text-xl:  1.375rem;    /* 22px — 卡片标题 */
--text-2xl: 1.75rem;     /* 28px — 区块标题 */
--text-3xl: 2.25rem;     /* 36px — 页面标题 */

--leading-tight:  1.2;   /* 标题行高 */
--leading-normal: 1.55;  /* 正文行高 */
--leading-relaxed: 1.7;  /* 长文本行高 */

--tracking-tight: -0.02em;  /* 大标题收紧字距 */
--tracking-wide:  0.02em;   /* 小字加宽字距 */
```

### 4.5 间距（4px 网格 + 半格）

```css
--space-0:  0;
--space-1:  4px;
--space-2:  8px;
--space-3:  12px;
--space-4:  16px;
--space-5:  20px;
--space-6:  24px;
--space-8:  32px;
--space-10: 40px;
--space-12: 48px;
--space-16: 64px;
--space-20: 80px;
```

## 5. 动画与微交互

### 5.1 过渡令牌

```css
--ease-out: cubic-bezier(0.16, 1, 0.3, 1);
--ease-in-out: cubic-bezier(0.65, 0, 0.35, 1);
--ease-spring: cubic-bezier(0.34, 1.56, 0.64, 1);

--duration-fast: 120ms;    /* 悬停颜色切换 */
--duration-normal: 200ms;  /* 展开/收起 */
--duration-slow: 350ms;    /* 页面过渡 */
--duration-slower: 500ms;  /* Modal 进出 */
```

### 5.2 全局交互

| 场景 | 效果 |
|------|------|
| 按钮悬停 | 背景加深 + `scale(1.02)` + 100ms ease-out |
| 按钮按压 | `scale(0.98)` + 80ms ease-out |
| 卡片悬停 | 上移 2px + 阴影加深 + 200ms ease-out |
| 输入框聚焦 | border 变色 + 4px glow ring + 150ms |
| 链接悬停 | 颜色过渡 120ms |
| Modal 打开 | `opacity 0→1` + `scale(0.96→1)` + 300ms ease-spring |
| 页面切换 | 内容区 `opacity 0→1` + `translateY(8px→0)` + 300ms |
| 骨架屏 | shimmer 动画，1.5s 循环 |

### 5.3 视觉点缀

- **统计卡片**：悬停时右上角出现渐变光晕
- **侧边栏激活项**：左侧 3px 渐变指示条 + 背景色过渡
- **表格行**：悬停时浅色背景 + 微弱的 `translateX(2px)`
- **成功/错误 Toast**：从右侧滑入 + 400ms 弹簧动画
- **签到按钮**：脉冲环动画（`pulse-ring`），吸引注意力

## 6. 用户端布局设计

### 6.1 桌面/平板（>=768px）

```
+------------------+---------------------------------------------------+
|  🏛               |  搜索课程/学生...        🔔  Hallo!  张三 [教师] ▾  |
|  仪表盘            +---------------------------------------------------+
|  我的课程          |                                                    |
|  考勤记录          |                                                     |
|  请假审批          |                  主内容区                             |
|                  |                                                     |
|  ──────────       |                                                     |
|  ⚙ 设置           |                                                     |
+------------------+---------------------------------------------------+
```

- 侧边栏宽度 240px，浅灰底 (`--color-surface`)，右侧 1px 边框
- Logo 区域使用渐变文字效果
- 顶部栏 56px，半透明毛玻璃效果 (`backdrop-filter: blur(12px)`)
- 平板（768-1024px）：侧边栏折叠为图标模式（72px），hover 展开完整菜单

### 6.2 手机（<768px）

```
+--------------------------------------------+
|  🏛 考勤管理                     🔔  [头像]  |
+--------------------------------------------+
|                                              |
|                                              |
|                 主内容区                       |
|          （全屏滚动，表格→卡片列表）             |
|                                              |
|                                              |
+--------------------------------------------+
|  🏠       📷        📋        📝        👤     |
| 首页     签到     考勤      请假      我的      |
+--------------------------------------------+
```

- 顶部栏 52px，固定顶部，毛玻璃效果
- 底部 Tab 栏 56px，固定底部，毛玻璃效果 + 上边框
- 当前 Tab 图标亮色渐变填充，文字加粗
- 表格转为圆角卡片列表，每行一张卡片

## 7. 管理员端布局设计

```
+--------------------------------+------------------------------------------+
|                                |  Hallo, 系统管理员   [管理员]  [登出]      |
|  ████████████████████████████   +------------------------------------------+
|   🏛 考勤管理系统                 |                                           |
|                                |                                           |
|   仪表盘               ← 3px   |               主内容区                     |
|   院系管理              指示条  |                                           |
|   学生管理                      |                                           |
|   教师管理                      |                                           |
|   课程管理                      |                                           |
|   统计报表                      |                                           |
|                                |                                           |
+--------------------------------+------------------------------------------+
```

- 侧边栏 260px 固定，深色底 (`#0f0d1e`)
- Logo 区 64px 高，logo 使用白色 + 渐变强调
- 激活菜单项：左侧 3px `--color-primary-gradient` 竖条 + `--sidebar-active-bg` 底色
- 菜单图标 20px，`--sidebar-text-dim` 色，激活时变亮
- 菜单文字 `--text-sm`，`500` weight
- 顶部栏 56px，`--color-surface` + 底部 1px `--color-border`

## 8. 登录页设计

```
+------------------------------------------------------------------+
|                                                                   |
|  ┌─────────────────────────────┐   ┌───────────────────────────┐  |
|  │                             │   │                            │  |
|  │      [抽象几何装饰图案]        │   │    👋 欢迎回来             │  |
|  │                             │   │                            │  |
|  │    🏛 考勤管理系统            │   │   登录你的账号              │  |
|  │    智能考勤 · 高效管理        │   │                            │  |
|  │                             │   │   ┌──────────────────────┐ │  |
|  │  "让每一次考勤都有迹可循"      │   │   │ 👤 学号 / 工号        │ │  |
|  │                             │   │   └──────────────────────┘ │  |
|  │  ── ● ● ● ──               │   │   ┌──────────────────────┐ │  |
|  │                             │   │   │ 🔒 密码              │ │  |
|  │                             │   │   └──────────────────────┘ │  |
|  │                             │   │                            │  |
|  │                             │   │   [ 登  录  ]              │  |
|  └─────────────────────────────┘   └───────────────────────────┘  |
|                                                                   |
+------------------------------------------------------------------+
```

- 左 50%：渐变底色 (`linear-gradient(160deg, #4f46e5 0%, #7c3aed 40%, #a21caf 100%)`) 
  - 叠加 CSS `noise` 纹理（`background-image: url("data:image/svg+xml,...")`），制造微颗粒质感
  - 叠加浮动几何图形（CSS 动画，大圆/三角形半透明移动）
  - 系统名使用大号白字 + 微弱 `text-shadow`
  - 底部 3 个圆点轮播指示器
- 右 50%：纯白底，垂直居中表单
  - 输入框：12px 圆角，`--color-border` 默认边框，前置图标
  - 输入框聚焦：`--color-border-focus` 边框 + `--shadow-glow` 光晕
  - 主按钮：`--color-primary-gradient` + 12px 圆角 + 全宽
  - 按钮悬停：`translateY(-1px)` + `--color-primary-glow` 发光
- 手机端：上下堆叠，左侧 Banner 缩小为顶部 200px

## 9. 组件升级详情

### 9.1 Button 组件

| Variant | 默认 | 悬停 | 按压 |
|---------|------|------|------|
| `Primary` | 渐变底 + 白字 | glow + translateY(-1px) | translateY(0) + 变暗 |
| `Secondary` | 白底 + border + 暗字 | border 加深 | bg 微变 |
| `Ghost` | 透明 + 暗字 | bg `--color-primary-subtle` | bg 加深 |
| `Danger` | 暖红底 + 白字 | glow(red) + translateY | 变暗 |
| `Gradient` | 渐变底 + 白字 | 更强的 glow | 变暗 |
| `Icon` | 透明 + 图标色 | bg `--color-neutral-100` | bg 加深 |

尺寸：`sm`(32px) / `md`(40px) / `lg`(48px)

### 9.2 Card 组件

- 白底 + `--shadow-raised` + `--radius-lg`
- 悬停：`translateY(-2px)` + `--shadow-overlay` + 300ms ease-out
- 卡片内标题 `--text-xl` + `600` weight
- 统计数据卡片特殊样式：顶部 3px 渐变条

### 9.3 Modal 组件

- 遮罩层：`rgba(15, 13, 30, 0.5)` + `backdrop-filter: blur(4px)`
- 弹窗本体：`--shadow-modal` + `--radius-xl` + 白底
- 进场：`opacity 0→1` + `scale(0.95→1)` + 300ms `--ease-spring`
- 顶部标题栏 + 底部按钮栏，中间内容可滚动

### 9.4 Table 组件

- 表头：`--text-xs` + `--color-text-tertiary` + uppercase + tracking-wide
- 表体行：`--text-sm`，行高 44px
- 偶数行：`--color-bg` 背景
- 悬停行：`--color-surface-hover` + `translateX(2px)` 
- 移动端：每行变为卡片（label + value 左右布局）

### 9.5 Input 组件

- 默认：`--color-border` 边框 + `--radius-sm` + 40px 高
- 聚焦：`--color-border-focus` + `--shadow-glow`
- 错误态：`--color-error` 边框 + 红色 shadow
- 前置图标内嵌（颜色 `--color-text-tertiary`）
- 占位文字 `--color-text-tertiary`

### 9.6 Badge 组件

- `--radius-full` 药丸形，padding 4px 10px
- 默认：`--color-primary-subtle` 底 + `--color-primary` 字
- Success：绿色底 + 绿色字
- Warning：黄色底 + 深黄字
- Error：粉色底 + 暖红字

### 9.7 新增组件

| 组件 | 功能 |
|------|------|
| `TabBar` | 手机端底部固定导航，5 个图标 + 文字，当前项渐变填充 |
| `StatCard` | 统计数值卡片，顶部分色渐变条，悬停微动效 |
| `Skeleton` | 骨架屏加载占位，shimmer 动画 |
| `Toast` | 右上角通知，滑入 + 自动消失 |
| `TrendChart` | 纯 CSS 迷你趋势图（柱状或折线） |
| `BrandBanner` | 登录页左侧品牌展示区 |

## 10. 响应式断点

| 断点 | 宽度 | 用户端行为 |
|------|------|------|
| 手机 | <768px | 底部 Tab + 单列 + 表格→卡片列表 |
| 平板 | 768-1024px | 折叠侧边栏（72px 图标）+ Grid 2 列 |
| 桌面 | >1024px | 完整侧边栏（240px）+ Grid 多列自适应 |

## 11. 文件变更范围

### 11.1 新增项目

```
src/MyCollegeNew.Admin/          # 管理员端
  MyCollegeNew.Admin.csproj
  Program.cs
  appsettings.json / Development.json
  Properties/launchSettings.json
  Components/
    App.razor, Routes.razor, _Imports.razor, AccessGuard.razor
    Layout/
      AdminMainLayout.razor + .css
      AdminNavMenu.razor + .css
      LoginLayout.razor + .css
    Pages/
      Admin/
        Dashboard.razor, Departments.razor, DepartmentNode.razor
        Students.razor, Teachers.razor, Courses.razor, Statistics.razor
      Login.razor + .css
    Ui/                          # 从 Web 复制
  Services/                      # 从 Web 复制
  wwwroot/css/
    design-system.css, admin.css, ui-components.css, pages.css
```

### 11.2 改动项目

```
src/MyCollegeNew.Web/
  Components/
    Layout/
      MainLayout.razor + .css    # 支持底部 Tab + 毛玻璃顶部栏
      NavMenu.razor + .css       # 新视觉
      LoginLayout.razor           # 左右分屏
    Pages/
      Login.razor + .css          # 左右分屏
      Admin/                      # 移除整个目录
    Ui/
      Button.razor                # 新增 Gradient / Icon variants
      Card.razor                  # 悬停动效 + stat 模式
      Modal.razor                 # 毛玻璃遮罩 + 弹簧动画
      Table.razor                 # 悬停行 + 移动端卡片化
      Badge.razor                 # 全圆药丸
      Input.razor                 # 聚焦 glow
      PageHeader.razor            # 轻量化
      StatCard.razor              # 新增
      TabBar.razor                # 新增
      Skeleton.razor              # 新增
    App.razor
  wwwroot/css/
    design-system.css             # 完全重写
    ui-components.css             # 完全重写
    pages.css                     # 重写 + 响应式
    animations.css                # 新增：动画关键帧
```

### 11.3 不变项目

```
src/MyCollegeNew.Api/           # 不变
src/MyCollegeNew.Shared/        # 不变
src/MyCollegeNew.Infrastructure/# 不变
src/MyCollegeNew.ServiceDefaults/# 不变
src/MyCollegeNew.Tests/         # 不变
```

## 12. 非目标

- 不修改后端 API 结构和接口
- 不修改数据模型和业务逻辑
- 不引入外部 CSS 框架（不依赖 npm 包，纯手写 CSS）
- 不引入 JS 动效库（CSS transition / animation 全覆盖）
- 不添加新功能页面

## 13. 成功标准

| 检查项 | 标准 |
|--------|------|
| 构建 | `dotnet build` 全部项目 0 错误 0 警告 |
| 登录 | admin/123456 在两端均能登录成功，跳转正确 |
| 用户端响应式 | 手机底部 Tab / 平板图标侧边栏 / 桌面完整侧边栏 三个断点正常 |
| 管理端页面 | 仪表盘、院系、学生、教师、课程、统计全部加载正常，无 API 错误 |
| 动画流畅 | 所有过渡使用 CSS `transition`/`animation`，60fps |
| 设计令牌 | 所有颜色、尺寸、圆角、阴影、间距引用 CSS 变量 |
| 图标 | 使用 Lucide SVG 图标，禁止 emoji 作为功能图标 |
| 配色 | 无蓝紫色渐变滥用（渐变仅用于按钮/品牌区/accent，非大面积使用） |
