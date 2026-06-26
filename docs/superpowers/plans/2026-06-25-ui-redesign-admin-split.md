# UI 重构与项目拆分实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 Web 项目拆分为用户端和管理员端，全面重写 UI 为现代 Shadcn 风格

**Architecture:** 先重写 CSS 基础层（design-system / ui-components / animations / pages），再升级 UI 组件，接着重写布局和登录页，最后创建 Admin 项目复用组件

**Tech Stack:** .NET 10 Blazor SSR + CSS Custom Properties + Lucide Icons

**Design Doc:** `docs/superpowers/specs/2026-06-25-ui-redesign-admin-split-design.md`

---

### Task 1: 创建 animations.css

**Files:**
- Create: `src/MyCollegeNew.Web/wwwroot/css/animations.css`

- [ ] **Step 1: 创建动画关键帧文件**

```css
/* ===================================================================
 * 动画关键帧：全部使用 CSS @keyframes，无 JS 依赖
 * =================================================================== */

/* 淡入上浮 —— 页面切换 */
@keyframes fade-in-up {
    from { opacity: 0; transform: translateY(8px); }
    to   { opacity: 1; transform: translateY(0); }
}

/* 淡入缩放 —— Modal / Dialog 弹入 */
@keyframes scale-in {
    from { opacity: 0; transform: scale(0.95); }
    to   { opacity: 1; transform: scale(1); }
}

/* 淡出缩放 —— Modal / Dialog 弹出 */
@keyframes scale-out {
    from { opacity: 1; transform: scale(1); }
    to   { opacity: 0; transform: scale(0.95); }
}

/* 右侧滑入 —— Toast 通知 */
@keyframes slide-in-right {
    from { opacity: 0; transform: translateX(100%); }
    to   { opacity: 1; transform: translateX(0); }
}

/* 右侧滑出 —— Toast 消失 */
@keyframes slide-out-right {
    from { opacity: 1; transform: translateX(0); }
    to   { opacity: 0; transform: translateX(100%); }
}

/* Shimmer —— 骨架屏加载 */
@keyframes shimmer {
    0%   { background-position: -200% 0; }
    100% { background-position: 200% 0; }
}

/* 脉冲环 —— 签到按钮吸引注意力 */
@keyframes pulse-ring {
    0%   { box-shadow: 0 0 0 0 rgba(129, 140, 248, 0.4); }
    70%  { box-shadow: 0 0 0 12px rgba(129, 140, 248, 0); }
    100% { box-shadow: 0 0 0 0 rgba(129, 140, 248, 0); }
}

/* 渐变光晕呼吸 —— 统计卡片悬停 */
@keyframes glow-breathe {
    0%, 100% { opacity: 0.6; }
    50%      { opacity: 1; }
}

/* 浮动 —— 登录页背景几何图形 */
@keyframes float {
    0%, 100% { transform: translateY(0) rotate(0deg); }
    33%      { transform: translateY(-12px) rotate(1deg); }
    66%      { transform: translateY(6px) rotate(-1deg); }
}

/* 浮动（延迟相位） */
@keyframes float-delayed {
    0%, 100% { transform: translateY(0) rotate(0deg); }
    33%      { transform: translateY(8px) rotate(-1deg); }
    66%      { transform: translateY(-10px) rotate(1deg); }
}

/* 旋转加载 */
@keyframes spin {
    to { transform: rotate(360deg); }
}

/* ---------- 工具类 ---------- */
.animate-fade-in-up {
    animation: fade-in-up var(--duration-normal) var(--ease-out) both;
}

.animate-scale-in {
    animation: scale-in var(--duration-slow) var(--ease-spring) both;
}

.animate-slide-in-right {
    animation: slide-in-right var(--duration-slow) var(--ease-spring) both;
}
```

- [ ] **Step 2: 在 App.razor 中引入 animations.css**

在 `<link rel="stylesheet" href="css/app.css" />` 之后添加：

```html
<link rel="stylesheet" href="css/animations.css" />
```

---

### Task 2: 重写 design-system.css

**Files:**
- Modify: `src/MyCollegeNew.Web/wwwroot/css/design-system.css`

- [ ] **Step 1: 用完整内容覆盖 design-system.css**

```css
/* ===================================================================
 * 设计系统：色彩 | 排版 | 间距 | 阴影 | 圆角 | 动画 | 断点
 * 所有视觉属性必须引用此文件中的 CSS 变量
 * =================================================================== */

:root {
    /* ---- 主色 - Iris 紫蓝渐变系 ---- */
    --color-primary: #818cf8;
    --color-primary-hover: #6366f1;
    --color-primary-pressed: #4f46e5;
    --color-primary-subtle: #eef2ff;
    --color-primary-gradient: linear-gradient(135deg, #818cf8 0%, #a78bfa 50%, #c084fc 100%);
    --color-primary-glow: 0 0 20px rgba(129, 140, 248, 0.25);

    /* ---- 语义色 ---- */
    --color-success: #22c55e;
    --color-success-hover: #16a34a;
    --color-success-subtle: #f0fdf4;
    --color-warning: #f59e0b;
    --color-warning-hover: #d97706;
    --color-warning-subtle: #fffbeb;
    --color-error: #f43f5e;
    --color-error-hover: #e11d48;
    --color-error-subtle: #fff1f2;

    /* ---- 中性分层 ---- */
    --color-bg: #f9fafb;
    --color-surface: #ffffff;
    --color-surface-hover: #f8fafc;
    --color-surface-raised: #ffffff;
    --color-border: #e5e7eb;
    --color-border-light: #f3f4f6;
    --color-border-focus: #a5b4fc;

    /* ---- 文字层次 ---- */
    --color-text-primary: #0f172a;
    --color-text-secondary: #475569;
    --color-text-tertiary: #94a3b8;
    --color-text-inverse: #ffffff;

    /* ---- 管理端侧边栏 ---- */
    --sidebar-bg: #0f0d1e;
    --sidebar-text: #a5b4fc;
    --sidebar-text-dim: #6366a0;
    --sidebar-active-bg: rgba(129, 140, 248, 0.12);
    --sidebar-active-text: #c7d2fe;
    --sidebar-border: rgba(129, 140, 248, 0.08);

    /* ---- 排版 ---- */
    --font-family: "Inter", "SF Pro Text", -apple-system, "PingFang SC",
        "Microsoft YaHei", "Noto Sans SC", sans-serif;
    --font-family-mono: "JetBrains Mono", "SF Mono", "Cascadia Code", monospace;

    --text-xs: 0.75rem;   /* 12px */
    --text-sm: 0.875rem;  /* 14px */
    --text-base: 1rem;    /* 16px */
    --text-lg: 1.125rem;  /* 18px */
    --text-xl: 1.375rem;  /* 22px */
    --text-2xl: 1.75rem;  /* 28px */
    --text-3xl: 2.25rem;  /* 36px */

    --font-weight-normal: 400;
    --font-weight-medium: 500;
    --font-weight-semibold: 600;
    --font-weight-bold: 700;

    --leading-tight: 1.2;
    --leading-normal: 1.55;
    --leading-relaxed: 1.7;

    --tracking-tight: -0.02em;
    --tracking-wide: 0.02em;

    /* ---- 间距（4px 基础网格） ---- */
    --space-1: 4px;
    --space-2: 8px;
    --space-3: 12px;
    --space-4: 16px;
    --space-5: 20px;
    --space-6: 24px;
    --space-8: 32px;
    --space-10: 40px;
    --space-12: 48px;
    --space-16: 64px;
    --space-20: 80px;

    /* ---- 圆角 ---- */
    --radius-xs: 4px;
    --radius-sm: 8px;
    --radius-md: 12px;
    --radius-lg: 16px;
    --radius-xl: 24px;
    --radius-full: 9999px;

    /* ---- 阴影 ---- */
    --shadow-raised: 0 1px 2px rgba(0, 0, 0, 0.04), 0 1px 3px rgba(0, 0, 0, 0.06);
    --shadow-overlay: 0 4px 6px rgba(0, 0, 0, 0.04), 0 2px 16px rgba(0, 0, 0, 0.08);
    --shadow-modal: 0 8px 8px rgba(0, 0, 0, 0.04), 0 4px 32px rgba(0, 0, 0, 0.12);
    --shadow-glow: 0 0 0 4px rgba(129, 140, 248, 0.15);

    /* ---- 动画 ---- */
    --ease-out: cubic-bezier(0.16, 1, 0.3, 1);
    --ease-in-out: cubic-bezier(0.65, 0, 0.35, 1);
    --ease-spring: cubic-bezier(0.34, 1.56, 0.64, 1);

    --duration-fast: 120ms;
    --duration-normal: 200ms;
    --duration-slow: 350ms;
    --duration-slower: 500ms;

    /* ---- 断点 ---- */
    --breakpoint-mobile: 767.98px;
    --breakpoint-tablet: 1023.98px;
    --breakpoint-desktop: 1024px;
}

/* ===================================================================
 * 全局基础
 * =================================================================== */
*, *::before, *::after {
    box-sizing: border-box;
}

html, body {
    margin: 0;
    padding: 0;
    font-family: var(--font-family);
    font-size: var(--text-sm);
    line-height: var(--leading-normal);
    background-color: var(--color-bg);
    color: var(--color-text-primary);
    -webkit-font-smoothing: antialiased;
    -moz-osx-font-smoothing: grayscale;
}

h1, h2, h3, h4, h5, h6 {
    margin: 0;
    font-weight: var(--font-weight-semibold);
    line-height: var(--leading-tight);
    color: var(--color-text-primary);
}
h1 { font-size: var(--text-3xl); letter-spacing: var(--tracking-tight); }
h2 { font-size: var(--text-2xl); }
h3 { font-size: var(--text-xl); }
h4 { font-size: var(--text-lg); }
h5 { font-size: var(--text-base); font-weight: var(--font-weight-medium); }
h6 { font-size: var(--text-sm); font-weight: var(--font-weight-medium); }

p { margin: 0; line-height: var(--leading-normal); }

a {
    color: var(--color-primary);
    text-decoration: none;
    transition: color var(--duration-fast) var(--ease-out);
}
a:hover {
    color: var(--color-primary-hover);
}

button {
    font-family: inherit;
    cursor: pointer;
    border: none;
    background: none;
    padding: 0;
}

ul, ol { margin: 0; padding: 0; list-style: none; }

/* ---- 聚焦环 ---- */
:focus-visible {
    outline: 2px solid var(--color-primary);
    outline-offset: 2px;
    border-radius: var(--radius-xs);
}

/* ---- 滚动条 ---- */
::-webkit-scrollbar { width: 6px; height: 6px; }
::-webkit-scrollbar-thumb {
    background-color: var(--color-border);
    border-radius: var(--radius-full);
}
::-webkit-scrollbar-thumb:hover { background-color: var(--color-text-tertiary); }
::-webkit-scrollbar-track { background-color: transparent; }

/* ---- 响应式触控最小目标 ---- */
@media (max-width: 767.98px) {
    button, .nav-link, .ui-btn, [role="button"] {
        min-height: 44px;
        min-width: 44px;
    }
}
```

---

### Task 3: 重写 ui-components.css 

**Files:**
- Modify: `src/MyCollegeNew.Web/wwwroot/css/ui-components.css`

- [ ] **Step 1: 用完整内容覆盖 ui-components.css**

```css
/* ===================================================================
 * UI 组件样式：使用 design-system.css 中的 CSS 变量
 * 按钮 | 卡片 | 输入框 | 表格 | 徽章 | 模态框 | 页面头 | 骨架屏 | 底部导航
 * =================================================================== */

/* ======================== Button ======================== */
.ui-btn {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: var(--space-2);
    padding: 0 var(--space-4);
    font-size: var(--text-sm);
    font-weight: var(--font-weight-medium);
    line-height: 1;
    border-radius: var(--radius-md);
    border: 1px solid transparent;
    cursor: pointer;
    transition: all var(--duration-fast) var(--ease-out);
    white-space: nowrap;
    user-select: none;
    height: 40px;
}
.ui-btn:active:not(:disabled) { transform: scale(0.98); }
.ui-btn:disabled { opacity: 0.5; cursor: not-allowed; transform: none !important; }

/* Primary */
.ui-btn--primary {
    background: var(--color-primary-gradient);
    color: var(--color-text-inverse);
    border: none;
}
.ui-btn--primary:hover:not(:disabled) {
    box-shadow: var(--color-primary-glow);
    transform: translateY(-1px);
}

/* Secondary */
.ui-btn--secondary {
    background: var(--color-surface);
    color: var(--color-text-primary);
    border: 1px solid var(--color-border);
}
.ui-btn--secondary:hover:not(:disabled) {
    border-color: var(--color-primary);
    color: var(--color-primary);
    background: var(--color-primary-subtle);
}

/* Ghost */
.ui-btn--ghost {
    background: transparent;
    color: var(--color-text-secondary);
}
.ui-btn--ghost:hover:not(:disabled) {
    background: var(--color-surface-hover);
    color: var(--color-text-primary);
}

/* Danger */
.ui-btn--danger {
    background: var(--color-error);
    color: var(--color-text-inverse);
    border: none;
}
.ui-btn--danger:hover:not(:disabled) {
    background: var(--color-error-hover);
    transform: translateY(-1px);
}

/* Gradient (等同于 Primary，显式变体名) */
.ui-btn--gradient { composes: ui-btn--primary; }

/* Icon-only */
.ui-btn--icon {
    width: 40px;
    padding: 0;
    color: var(--color-text-secondary);
}
.ui-btn--icon:hover:not(:disabled) {
    color: var(--color-text-primary);
    background: var(--color-surface-hover);
}

/* 内置图标 */
.ui-btn__icon { display: inline-flex; align-items: center; }
.ui-btn__spinner {
    width: 16px; height: 16px;
    border: 2px solid currentColor;
    border-top-color: transparent;
    border-radius: 50%;
    animation: spin 0.6s linear infinite;
}

/* ======================== Card ======================== */
.ui-card {
    background: var(--color-surface);
    border-radius: var(--radius-lg);
    padding: var(--space-6);
}
.ui-card--bordered { border: 1px solid var(--color-border); }
.ui-card--shadowed {
    box-shadow: var(--shadow-raised);
    transition: box-shadow var(--duration-normal) var(--ease-out),
                transform var(--duration-normal) var(--ease-out);
}
.ui-card--shadowed:hover {
    box-shadow: var(--shadow-overlay);
    transform: translateY(-2px);
}

/* Stat Card —— 统计数值卡片 */
.ui-card--stat {
    border-radius: var(--radius-lg);
    border: 1px solid var(--color-border-light);
    position: relative;
    overflow: hidden;
}
.ui-card--stat::before {
    content: "";
    position: absolute;
    top: 0; left: 0; right: 0;
    height: 3px;
    background: var(--color-primary-gradient);
}
.ui-card--stat-success::before { background: linear-gradient(135deg, #22c55e, #10b981); }
.ui-card--stat-warning::before { background: linear-gradient(135deg, #f59e0b, #f97316); }
.ui-card--stat-error::before   { background: linear-gradient(135deg, #f43f5e, #e11d48); }

.ui-card__header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: var(--space-4);
}
.ui-card__title {
    font-size: var(--text-xl);
    font-weight: var(--font-weight-semibold);
    color: var(--color-text-primary);
}
.ui-card__actions { display: flex; gap: var(--space-2); }
.ui-card__body { font-size: var(--text-sm); color: var(--color-text-secondary); }

/* Stat 数值 */
.ui-stat-value {
    font-size: var(--text-3xl);
    font-weight: var(--font-weight-bold);
    color: var(--color-text-primary);
    line-height: var(--leading-tight);
}
.ui-stat-label {
    font-size: var(--text-sm);
    font-weight: var(--font-weight-medium);
    color: var(--color-text-tertiary);
    text-transform: uppercase;
    letter-spacing: var(--tracking-wide);
}
.ui-stat-trend {
    display: inline-flex;
    align-items: center;
    gap: var(--space-1);
    font-size: var(--text-xs);
    font-weight: var(--font-weight-medium);
    margin-top: var(--space-2);
}
.ui-stat-trend--up   { color: var(--color-success); }
.ui-stat-trend--down { color: var(--color-error); }

/* ======================== Input ======================== */
.ui-input {
    width: 100%;
    height: 40px;
    padding: 0 var(--space-3);
    font-size: var(--text-sm);
    font-family: var(--font-family);
    color: var(--color-text-primary);
    background: var(--color-surface);
    border: 1px solid var(--color-border);
    border-radius: var(--radius-sm);
    transition: border-color var(--duration-fast) var(--ease-out),
                box-shadow var(--duration-fast) var(--ease-out);
    outline: none;
}
.ui-input::placeholder { color: var(--color-text-tertiary); }
.ui-input:focus {
    border-color: var(--color-border-focus);
    box-shadow: var(--shadow-glow);
}
.ui-input--error {
    border-color: var(--color-error);
}
.ui-input--error:focus {
    box-shadow: 0 0 0 4px rgba(244, 63, 94, 0.15);
}

/* Input with icon */
.ui-input-wrapper {
    position: relative;
    display: flex;
    align-items: center;
}
.ui-input-wrapper .ui-input-icon {
    position: absolute;
    left: var(--space-3);
    color: var(--color-text-tertiary);
    pointer-events: none;
    display: inline-flex;
}
.ui-input-wrapper .ui-input {
    padding-left: var(--space-10);
}

/* ======================== Table ======================== */
.ui-table {
    width: 100%;
    border-collapse: collapse;
    font-size: var(--text-sm);
}
.ui-table thead th {
    padding: var(--space-2) var(--space-4);
    font-size: var(--text-xs);
    font-weight: var(--font-weight-medium);
    color: var(--color-text-tertiary);
    text-transform: uppercase;
    letter-spacing: var(--tracking-wide);
    text-align: left;
    border-bottom: 1px solid var(--color-border);
    height: 40px;
    white-space: nowrap;
}
.ui-table tbody td {
    padding: var(--space-2) var(--space-4);
    color: var(--color-text-secondary);
    border-bottom: 1px solid var(--color-border-light);
    height: 44px;
}
.ui-table tbody tr {
    transition: background var(--duration-fast) var(--ease-out),
                transform var(--duration-fast) var(--ease-out);
}
.ui-table tbody tr:nth-child(even) { background: var(--color-bg); }
.ui-table tbody tr:hover {
    background: var(--color-surface-hover);
    transform: translateX(2px);
}

/* 移动端：表格转为卡片列表 */
@media (max-width: 767.98px) {
    .ui-table--responsive thead { display: none; }
    .ui-table--responsive tbody tr {
        display: flex;
        flex-direction: column;
        padding: var(--space-4);
        border: 1px solid var(--color-border);
        border-radius: var(--radius-md);
        margin-bottom: var(--space-3);
        background: var(--color-surface);
    }
    .ui-table--responsive tbody td {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: var(--space-1) 0;
        border: none;
        height: auto;
    }
    .ui-table--responsive tbody td::before {
        content: attr(data-label);
        font-weight: var(--font-weight-medium);
        color: var(--color-text-primary);
        font-size: var(--text-xs);
    }
}

/* ======================== Badge ======================== */
.ui-badge {
    display: inline-flex;
    align-items: center;
    padding: 2px 10px;
    font-size: var(--text-xs);
    font-weight: var(--font-weight-medium);
    line-height: 1.5;
    border-radius: var(--radius-full);
    white-space: nowrap;
}
.ui-badge--primary   { background: var(--color-primary-subtle); color: var(--color-primary); }
.ui-badge--success   { background: var(--color-success-subtle); color: var(--color-success); }
.ui-badge--warning   { background: var(--color-warning-subtle); color: var(--color-warning); }
.ui-badge--error     { background: var(--color-error-subtle); color: var(--color-error); }

/* ======================== Modal ======================== */
.ui-modal-overlay {
    position: fixed;
    inset: 0;
    background: rgba(15, 13, 30, 0.5);
    backdrop-filter: blur(4px);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 1000;
    animation: fade-in 200ms var(--ease-out);
}
.ui-modal {
    background: var(--color-surface);
    border-radius: var(--radius-xl);
    box-shadow: var(--shadow-modal);
    max-width: 520px;
    width: calc(100% - var(--space-8));
    max-height: 85vh;
    display: flex;
    flex-direction: column;
    animation: scale-in var(--duration-slow) var(--ease-spring);
}
.ui-modal__header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: var(--space-5) var(--space-6);
    border-bottom: 1px solid var(--color-border-light);
}
.ui-modal__title { font-size: var(--text-lg); font-weight: var(--font-weight-semibold); }
.ui-modal__body { padding: var(--space-6); overflow-y: auto; flex: 1; }
.ui-modal__footer {
    display: flex;
    align-items: center;
    justify-content: flex-end;
    gap: var(--space-3);
    padding: var(--space-4) var(--space-6);
    border-top: 1px solid var(--color-border-light);
}

@keyframes fade-in { from { opacity: 0; } to { opacity: 1; } }

/* ======================== PageHeader ======================== */
.page-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: var(--space-6);
}
.page-header__title {
    font-size: var(--text-2xl);
    font-weight: var(--font-weight-semibold);
    color: var(--color-text-primary);
    letter-spacing: var(--tracking-tight);
}
.page-header__subtitle {
    font-size: var(--text-sm);
    color: var(--color-text-tertiary);
    margin-top: var(--space-1);
}
.page-header__actions { display: flex; gap: var(--space-3); }

/* ======================== Skeleton ======================== */
.skeleton {
    background: linear-gradient(90deg,
        var(--color-border-light) 0%,
        var(--color-border) 40%,
        var(--color-border-light) 80%
    );
    background-size: 200% 100%;
    animation: shimmer 1.5s ease-in-out infinite;
    border-radius: var(--radius-sm);
}
.skeleton--text { height: 14px; width: 100%; margin-bottom: var(--space-2); }
.skeleton--title { height: 22px; width: 60%; margin-bottom: var(--space-3); }
.skeleton--avatar { width: 40px; height: 40px; border-radius: var(--radius-full); }
.skeleton--card { height: 120px; border-radius: var(--radius-lg); }

/* ======================== TabBar（手机端底部导航） ======================== */
.tab-bar {
    display: none;
    position: fixed;
    bottom: 0; left: 0; right: 0;
    height: 56px;
    background: rgba(255, 255, 255, 0.85);
    backdrop-filter: blur(12px);
    -webkit-backdrop-filter: blur(12px);
    border-top: 1px solid var(--color-border);
    z-index: 200;
    justify-content: space-around;
    align-items: center;
    padding-bottom: env(safe-area-inset-bottom, 0);
}
@media (max-width: 767.98px) {
    .tab-bar { display: flex; }
}
.tab-bar__item {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 2px;
    padding: var(--space-1) var(--space-2);
    color: var(--color-text-tertiary);
    font-size: 10px;
    font-weight: var(--font-weight-medium);
    text-decoration: none;
    transition: color var(--duration-fast) var(--ease-out);
    min-width: 48px;
}
.tab-bar__item--active {
    color: var(--color-primary);
}
.tab-bar__item--active .tab-bar__icon {
    background: var(--color-primary-gradient);
    -webkit-background-clip: text;
    -webkit-text-fill-color: transparent;
    background-clip: text;
}
.tab-bar__icon {
    width: 24px;
    height: 24px;
    display: flex;
    align-items: center;
    justify-content: center;
}

/* ======================== EmptyState ======================== */
.empty-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: var(--space-16) var(--space-6);
    text-align: center;
}
.empty-state__icon {
    color: var(--color-text-tertiary);
    margin-bottom: var(--space-4);
    opacity: 0.6;
}
.empty-state__title {
    font-size: var(--text-lg);
    font-weight: var(--font-weight-medium);
    color: var(--color-text-secondary);
    margin-bottom: var(--space-2);
}
.empty-state__description {
    font-size: var(--text-sm);
    color: var(--color-text-tertiary);
    max-width: 320px;
}

/* ======================== 消息提示 ======================== */
.alert {
    display: flex;
    align-items: flex-start;
    gap: var(--space-3);
    padding: var(--space-3) var(--space-4);
    border-radius: var(--radius-md);
    font-size: var(--text-sm);
    margin-bottom: var(--space-4);
    animation: fade-in-up var(--duration-normal) var(--ease-out) both;
}
.alert--error {
    background: var(--color-error-subtle);
    color: var(--color-error);
    border: 1px solid rgba(244, 63, 94, 0.15);
}
.alert--success {
    background: var(--color-success-subtle);
    color: var(--color-success);
    border: 1px solid rgba(34, 197, 94, 0.15);
}
.alert--warning {
    background: var(--color-warning-subtle);
    color: var(--color-warning);
    border: 1px solid rgba(245, 158, 11, 0.15);
}
```

---

### Task 4: 重写 pages.css

**Files:**
- Modify: `src/MyCollegeNew.Web/wwwroot/css/pages.css`

- [ ] **Step 1: 用完整内容覆盖 pages.css**

```css
/* ===================================================================
 * 页面级样式
 * 仪表盘 | 数据列表 | 详情页 | 登录 | 响应式
 * =================================================================== */

/* ---- 仪表盘网格 ---- */
.dashboard {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
    gap: var(--space-6);
    animation: fade-in-up var(--duration-normal) var(--ease-out) both;
}

/* ---- 统计卡片行 ---- */
.stat-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
    gap: var(--space-4);
    margin-bottom: var(--space-6);
}

/* ---- 双列表单布局 ---- */
.form-grid {
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    gap: var(--space-4);
}
@media (max-width: 767.98px) {
    .form-grid { grid-template-columns: 1fr; }
}

/* ---- 筛选栏 ---- */
.filter-bar {
    display: flex;
    align-items: center;
    gap: var(--space-3);
    margin-bottom: var(--space-4);
    flex-wrap: wrap;
}

/* ---- 页面加载/错误容器 ---- */
.page-container {
    padding: var(--space-6);
    animation: fade-in-up var(--duration-normal) var(--ease-out) both;
}
@media (max-width: 767.98px) {
    .page-container {
        padding: var(--space-4);
        padding-bottom: 72px; /* 为底部 TabBar 留空 */
    }
}

/* ---- 学生端快捷操作区 ---- */
.quick-actions {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
    gap: var(--space-3);
    margin-bottom: var(--space-6);
}

/* ---- 签到按钮（脉冲环） ---- */
.checkin-btn {
    position: relative;
}
.checkin-btn::after {
    content: "";
    position: absolute;
    inset: -4px;
    border-radius: var(--radius-md);
    animation: pulse-ring 2s ease-out infinite;
}

/* ===================================================================
 * 响应式：平板 (768-1024px)
 * =================================================================== */
@media (min-width: 768px) and (max-width: 1023.98px) {
    .dashboard { grid-template-columns: repeat(2, 1fr); }
    .page-container { padding: var(--space-5); }
}
```

---

### Task 5: 升级 UI 组件

**Files:**
- Modify: `src/MyCollegeNew.Web/Components/Ui/Button.razor`
- Modify: `src/MyCollegeNew.Web/Components/Ui/Card.razor`
- Modify: `src/MyCollegeNew.Web/Components/Ui/Modal.razor`
- Modify: `src/MyCollegeNew.Web/Components/Ui/Table.razor`
- Modify: `src/MyCollegeNew.Web/Components/Ui/Badge.razor`
- Modify: `src/MyCollegeNew.Web/Components/Ui/Input.razor`
- Modify: `src/MyCollegeNew.Web/Components/Ui/PageHeader.razor`
- Create: `src/MyCollegeNew.Web/Components/Ui/StatCard.razor`
- Create: `src/MyCollegeNew.Web/Components/Ui/TabBar.razor`
- Create: `src/MyCollegeNew.Web/Components/Ui/Skeleton.razor`

- [ ] **Step 1: 升级 Button.razor — 新增 Gradient、Danger、Ghost、Icon 变体**

```razor
@* 按钮组件：支持 Primary | Secondary | Gradient | Danger | Ghost | Icon *@
<button type="@Type"
        class="ui-btn ui-btn--@GetVariantClass() @GetSizeClass() @Class"
        disabled="@Disabled"
        @onclick="OnClick">
    @if (Loading)
    {
        <span class="ui-btn__spinner" aria-hidden="true"></span>
    }
    else if (!string.IsNullOrEmpty(Icon))
    {
        <span class="ui-btn__icon">
            <Icon Name="@Icon" Size="16" />
        </span>
    }
    @ChildContent
</button>

@code {
    [Parameter] public ButtonVariant Variant { get; set; } = ButtonVariant.Primary;
    [Parameter] public ButtonSize Size { get; set; } = ButtonSize.Md;
    [Parameter] public string Type { get; set; } = "button";
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public bool Loading { get; set; }
    [Parameter] public string? Icon { get; set; }
    [Parameter] public string? Class { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private string GetVariantClass() => Variant.ToString().ToLowerInvariant();
    private string GetSizeClass() => Size switch
    {
        ButtonSize.Sm => "ui-btn--sm",
        ButtonSize.Lg => "ui-btn--lg",
        _ => ""
    };
}
```

同步更新 `UiEnums.cs`，添加 `Ghost`, `Gradient`, `Icon` 到 `ButtonVariant` 枚举，添加 `ButtonSize` 枚举和 `Sm`, `Lg` 尺寸类 CSS。

- [ ] **Step 2: 升级 Card.razor — 新增 Stat 模式**

```razor
@* 卡片组件：bordered | shadowed | stat *@
<div class="ui-card @GetCardClass() @Class">
    @if (!string.IsNullOrEmpty(Title) || HasHeader)
    {
        <div class="ui-card__header">
            @if (!string.IsNullOrEmpty(Title))
            {
                <h3 class="ui-card__title">@Title</h3>
            }
            @if (Actions is not null)
            {
                <div class="ui-card__actions">@Actions</div>
            }
        </div>
    }
    <div class="ui-card__body">@ChildContent</div>
</div>

@code {
    [Parameter] public string? Title { get; set; }
    [Parameter] public bool HasHeader { get; set; }
    [Parameter] public bool Bordered { get; set; }
    [Parameter] public bool Shadowed { get; set; } = true;
    [Parameter] public CardAccent? Accent { get; set; }
    [Parameter] public string? Class { get; set; }
    [Parameter] public RenderFragment? Actions { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private string GetCardClass()
    {
        var cls = "";
        if (Bordered) cls += " ui-card--bordered";
        if (Shadowed && Accent is null) cls += " ui-card--shadowed";
        if (Accent is not null) cls += " ui-card--stat ui-card--stat-" + Accent.ToString().ToLowerInvariant();
        return cls;
    }
}
```

在 `UiEnums.cs` 新增 `CardAccent` 枚举：`None, Primary, Success, Warning, Error`。

- [ ] **Step 3: 升级 Modal.razor — 毛玻璃遮罩 + 弹簧动画**

```razor
@* 模态对话框：毛玻璃遮罩 + 弹簧动画 *@
@if (IsOpen)
{
    <div class="ui-modal-overlay" @onclick="CloseOnOverlay">
        <div class="ui-modal @Class" @onclick:stopPropagation="true"
             style="max-width: @Width">
            @if (!string.IsNullOrEmpty(Title))
            {
                <div class="ui-modal__header">
                    <h3 class="ui-modal__title">@Title</h3>
                    <button type="button" class="ui-btn ui-btn--icon" @onclick="Close" aria-label="关闭">
                        <Icon Name="x" Size="20" />
                    </button>
                </div>
            }
            <div class="ui-modal__body">@ChildContent</div>
            @if (Footer is not null)
            {
                <div class="ui-modal__footer">@Footer</div>
            }
        </div>
    </div>
}

@code {
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public string? Title { get; set; }
    [Parameter] public string Width { get; set; } = "520px";
    [Parameter] public string? Class { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public RenderFragment? Footer { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private async Task CloseOnOverlay() { await OnClose.InvokeAsync(); }
    private async Task Close() { await OnClose.InvokeAsync(); }
}
```

- [ ] **Step 4: 创建 StatCard.razor**

```razor
@* 统计数值卡片，顶部渐变色条 *@
<div class="ui-card ui-card--stat @AccentClass @Class">
    <div class="ui-stat-label">@Label</div>
    <div class="ui-stat-value">@Value</div>
    @if (!string.IsNullOrEmpty(Trend))
    {
        <div class="ui-stat-trend @TrendClass">
            <Icon Name="@TrendIcon" Size="14" />
            @TrendDescription
        </div>
    }
</div>

@code {
    [Parameter] public string Label { get; set; } = "";
    [Parameter] public string Value { get; set; } = "";
    [Parameter] public string? Trend { get; set; }
    [Parameter] public string? TrendDescription { get; set; }
    [Parameter] public string? TrendIcon { get; set; }
    [Parameter] public StatAccent Accent { get; set; } = StatAccent.Primary;
    [Parameter] public string? Class { get; set; }

    private string AccentClass => Accent switch
    {
        StatAccent.Success => "ui-card--stat-success",
        StatAccent.Warning => "ui-card--stat-warning",
        StatAccent.Error => "ui-card--stat-error",
        _ => ""
    };

    private string TrendClass => Trend switch
    {
        "up" => "ui-stat-trend--up",
        "down" => "ui-stat-trend--down",
        _ => ""
    };
}
```

在 `UiEnums.cs` 新增 `StatAccent` 枚举：`Primary, Success, Warning, Error`。

- [ ] **Step 5: 创建 TabBar.razor**

```razor
@* 手机端底部导航栏，仅 <768px 显示 *@
<nav class="tab-bar" role="navigation" aria-label="底部导航">
    @foreach (var item in Items)
    {
        <NavLink class="tab-bar__item" href="@item.Href" Match="@NavLinkMatch.Prefix"
                 ActiveClass="tab-bar__item--active">
            <span class="tab-bar__icon">
                <Icon Name="@item.Icon" Size="24" />
            </span>
            <span>@item.Label</span>
        </NavLink>
    }
</nav>

@code {
    [Parameter] public List<TabItem> Items { get; set; } = new();

    public class TabItem
    {
        public string Label { get; set; } = "";
        public string Href { get; set; } = "";
        public string Icon { get; set; } = "";
    }
}
```

- [ ] **Step 6: 创建 Skeleton.razor**

```razor
@* 骨架屏加载占位 *@
<div class="@GetClass() @Class" aria-hidden="true"></div>

@code {
    [Parameter] public SkeletonType Type { get; set; } = SkeletonType.Text;
    [Parameter] public string? Class { get; set; }

    private string GetClass() => Type switch
    {
        SkeletonType.Text => "skeleton skeleton--text",
        SkeletonType.Title => "skeleton skeleton--title",
        SkeletonType.Avatar => "skeleton skeleton--avatar",
        SkeletonType.Card => "skeleton skeleton--card",
        _ => "skeleton"
    };
}
```

在 `UiEnums.cs` 新增 `SkeletonType` 枚举：`Text, Title, Avatar, Card`。

---

### Task 6: 重写 MainLayout 和 NavMenu

**Files:**
- Modify: `src/MyCollegeNew.Web/Components/Layout/MainLayout.razor`
- Modify: `src/MyCollegeNew.Web/Components/Layout/MainLayout.razor.css`
- Modify: `src/MyCollegeNew.Web/Components/Layout/NavMenu.razor`
- Modify: `src/MyCollegeNew.Web/Components/Layout/NavMenu.razor.css`

- [ ] **Step 1: 重写 MainLayout.razor — 支持 TabBar + 毛玻璃顶部栏**

```razor
@inherits LayoutComponentBase
@inject NavigationManager NavigationManager
@inject CustomAuthStateProvider AuthStateProvider
@inject TokenService TokenService
@inject IApiClient ApiClient

<div class="app-layout">
    @* 侧边栏 *@
    <aside class="app-layout__sidebar">
        <NavMenu />
    </aside>

    @* 主区域 *@
    <div class="app-layout__main">
        @* 顶部栏 - 毛玻璃效果 *@
        <header class="app-layout__header">
            <div class="app-layout__brand-mobile">
                <span class="app-layout__logo-text">考勤管理</span>
            </div>
            <div class="app-layout__header-spacer"></div>
            <AuthorizeView>
                <Authorized>
                    <div class="app-layout__user">
                        <span class="app-layout__user-greeting">Hallo,</span>
                        <span class="app-layout__user-name">@context.User.Identity?.Name</span>
                        @{
                            var role = context.User.Claims.FirstOrDefault(
                                c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
                        }
                        @if (!string.IsNullOrEmpty(role))
                        {
                            <Badge Variant="BadgeVariant.Primary">@GetRoleDisplayName(role)</Badge>
                        }
                    </div>
                    <button type="button" class="app-layout__logout" @onclick="HandleLogout">
                        <Icon Name="log-out" Size="18" />
                    </button>
                </Authorized>
            </AuthorizeView>
        </header>

        @* 主内容 *@
        <main class="app-layout__content">
            @Body
        </main>
    </div>
</div>

@* 手机端底部导航 *@
<AuthorizeView>
    <Authorized>
        <TabBar Items="GetTabItems(context.User)" />
    </Authorized>
</AuthorizeView>

<div id="blazor-error-ui" data-nosnippet>
    An unhandled error has occurred.
    <a href="." class="reload">Reload</a>
    <span class="dismiss">&times;</span>
</div>

@code {
    private static string GetRoleDisplayName(string role) => role switch
    {
        "Admin" => "管理员",
        "Teacher" => "教师",
        "Counselor" => "辅导员",
        "Student" => "学生",
        _ => role
    };

    private static List<TabBar.TabItem> GetTabItems(System.Security.Claims.ClaimsPrincipal user)
    {
        if (user.IsInRole("Teacher") || user.IsInRole("Counselor"))
        {
            var items = new List<TabBar.TabItem>
            {
                new() { Label = "首页", Href = "/teacher/dashboard", Icon = "layout-dashboard" },
                new() { Label = "课程", Href = "/teacher/courses", Icon = "book-open" },
                new() { Label = "考勤", Href = "/teacher/attendance", Icon = "clipboard-list" },
            };
            if (user.IsInRole("Counselor"))
                items.Add(new() { Label = "审批", Href = "/teacher/leaves", Icon = "file-text" });
            return items;
        }
        if (user.IsInRole("Student"))
        {
            return new()
            {
                new() { Label = "首页", Href = "/student/home", Icon = "home" },
                new() { Label = "签到", Href = "/student/checkin", Icon = "qr-code" },
                new() { Label = "考勤", Href = "/student/attendance", Icon = "clipboard-list" },
                new() { Label = "请假", Href = "/student/leaves", Icon = "file-text" },
                new() { Label = "我的", Href = "/student/profile", Icon = "user" },
            };
        }
        return new();
    }

    private async Task HandleLogout()
    {
        try { await ApiClient.PostNoContentAsync("auth/logout"); } catch { }
        TokenService.RemoveToken();
        AuthStateProvider.NotifyUserLogout();
        NavigationManager.NavigateTo("/login", forceLoad: true);
    }
}
```

- [ ] **Step 2: 重写 MainLayout.razor.css — 现代毛玻璃布局**

```css
/* ---- 整体 ---- */
.app-layout {
    display: flex;
    min-height: 100vh;
    min-height: 100dvh;
    background-color: var(--color-bg);
}

/* ---- 侧边栏 ---- */
.app-layout__sidebar {
    width: 240px;
    flex-shrink: 0;
    background: var(--color-surface);
    border-right: 1px solid var(--color-border);
    position: sticky;
    top: 0;
    height: 100vh;
    height: 100dvh;
    overflow-y: auto;
    z-index: 100;
}

/* ---- 主区域 ---- */
.app-layout__main {
    flex: 1;
    display: flex;
    flex-direction: column;
    min-width: 0;
}

/* ---- 顶部栏（毛玻璃） ---- */
.app-layout__header {
    display: flex;
    align-items: center;
    gap: var(--space-3);
    padding: 0 var(--space-6);
    height: 56px;
    background: rgba(255, 255, 255, 0.72);
    backdrop-filter: blur(12px);
    -webkit-backdrop-filter: blur(12px);
    border-bottom: 1px solid var(--color-border);
    position: sticky;
    top: 0;
    z-index: 50;
}

.app-layout__brand-mobile { display: none; }
.app-layout__logo-text {
    font-size: var(--text-base);
    font-weight: var(--font-weight-semibold);
    background: var(--color-primary-gradient);
    -webkit-background-clip: text;
    -webkit-text-fill-color: transparent;
    background-clip: text;
}

.app-layout__header-spacer { flex: 1; }

.app-layout__user {
    display: flex;
    align-items: center;
    gap: var(--space-2);
}
.app-layout__user-greeting {
    font-size: var(--text-xs);
    color: var(--color-text-tertiary);
}
.app-layout__user-name {
    font-size: var(--text-sm);
    font-weight: var(--font-weight-medium);
    color: var(--color-text-primary);
}

.app-layout__logout {
    display: inline-flex;
    align-items: center;
    gap: var(--space-1);
    padding: var(--space-2);
    color: var(--color-text-tertiary);
    border-radius: var(--radius-sm);
    transition: all var(--duration-fast) var(--ease-out);
}
.app-layout__logout:hover {
    color: var(--color-error);
    background: var(--color-error-subtle);
}

/* ---- 主内容 ---- */
.app-layout__content {
    flex: 1;
    padding: var(--space-6);
    overflow-x: auto;
}

/* ---- 平板：侧边栏折叠 ---- */
@media (min-width: 768px) and (max-width: 1023.98px) {
    .app-layout__sidebar {
        width: 72px;
        overflow: visible;
    }
    .app-layout__content { padding: var(--space-4); }
}

/* ---- 手机 ---- */
@media (max-width: 767.98px) {
    .app-layout__sidebar { display: none; }
    .app-layout__brand-mobile { display: block; }
    .app-layout__header {
        padding: 0 var(--space-4);
        height: 52px;
        background: rgba(255, 255, 255, 0.85);
    }
    .app-layout__content {
        padding: var(--space-4);
        padding-bottom: 72px;
    }
    .app-layout__user-greeting { display: none; }
}
```

- [ ] **Step 3: 重写 NavMenu.razor — 保留现有逻辑，升级 HTML 类名**

仅需更改类名以匹配新 CSS：
- `nav-menu` → 保持不变，添加 `nav-menu__brand` 区域使用渐变文字
- `nav-menu__item` 中 `NavLink` 添加 `ActiveClass="nav-link--active"`
- 移除 Admin 菜单项（Admin 将迁移到新项目）

```razor
@* 侧边栏导航 *@
<nav class="nav-menu">
    <div class="nav-menu__brand">
        <Icon Name="graduation-cap" Size="22" />
        <span class="nav-menu__brand-text">考勤管理系统</span>
    </div>

    <AuthorizeView>
        <Authorized>
            <ul class="nav-menu__list">
                @foreach (var item in GetMenuItems(context.User))
                {
                    <li class="nav-menu__item">
                        <NavLink class="nav-menu__link" href="@item.Href" Match="@NavLinkMatch.Prefix"
                                 ActiveClass="nav-menu__link--active">
                            <Icon Name="@item.Icon" Size="20" />
                            <span>@item.Title</span>
                        </NavLink>
                    </li>
                }
            </ul>
        </Authorized>
    </AuthorizeView>
</nav>

@code {
    private sealed record MenuItem(string Title, string Href, string Icon);
    /* ... GetMenuItems 移除 Admin 分支 ... */
}
```

- [ ] **Step 4: 重写 NavMenu.razor.css**

```css
.nav-menu {
    display: flex;
    flex-direction: column;
    height: 100%;
}

.nav-menu__brand {
    display: flex;
    align-items: center;
    gap: var(--space-3);
    padding: var(--space-4) var(--space-5);
    height: 56px;
    border-bottom: 1px solid var(--color-border-light);
    color: var(--color-text-primary);
}

.nav-menu__brand-text {
    font-size: var(--text-base);
    font-weight: var(--font-weight-semibold);
    background: var(--color-primary-gradient);
    -webkit-background-clip: text;
    -webkit-text-fill-color: transparent;
    background-clip: text;
}

.nav-menu__list {
    padding: var(--space-3);
    flex: 1;
}

.nav-menu__item + .nav-menu__item { margin-top: var(--space-1); }

.nav-menu__link {
    display: flex;
    align-items: center;
    gap: var(--space-3);
    padding: var(--space-2) var(--space-3);
    height: 40px;
    font-size: var(--text-sm);
    font-weight: var(--font-weight-medium);
    color: var(--color-text-secondary);
    border-radius: var(--radius-md);
    text-decoration: none;
    transition: all var(--duration-fast) var(--ease-out);
}

.nav-menu__link:hover {
    background: var(--color-surface-hover);
    color: var(--color-text-primary);
}

.nav-menu__link--active {
    background: var(--color-primary-subtle);
    color: var(--color-primary);
    font-weight: var(--font-weight-semibold);
}

/* 平板：侧边栏折叠为图标 */
@media (min-width: 768px) and (max-width: 1023.98px) {
    .nav-menu__brand { justify-content: center; padding: var(--space-3); }
    .nav-menu__brand-text { display: none; }
    .nav-menu__link { justify-content: center; padding: var(--space-2); }
    .nav-menu__link span { display: none; }
}
```

---

### Task 7: 重写登录页

**Files:**
- Modify: `src/MyCollegeNew.Web/Components/Pages/Login.razor`
- Modify: `src/MyCollegeNew.Web/Components/Pages/Login.razor.css`
- Modify: `src/MyCollegeNew.Web/Components/Layout/LoginLayout.razor`
- Modify: `src/MyCollegeNew.Web/Components/Layout/LoginLayout.razor.css`

- [ ] **Step 1: 重写 Login.razor — 左右分屏设计**

```razor
@page "/login"
@layout LoginLayout
@inject IAntiforgery Antiforgery
@inject IHttpContextAccessor HttpContextAccessor

<PageTitle>登录 - 考勤管理系统</PageTitle>

@{
    var httpContext = HttpContextAccessor.HttpContext;
    var tokenSet = httpContext is not null
        ? Antiforgery.GetAndStoreTokens(httpContext)
        : null;
    var requestToken = tokenSet?.RequestToken ?? string.Empty;
}

<div class="login-page">
    @* 左侧品牌展示区 *@
    <div class="login-page__brand">
        @* 浮动装饰几何图形 *@
        <div class="login-page__deco login-page__deco--1"></div>
        <div class="login-page__deco login-page__deco--2"></div>
        <div class="login-page__deco login-page__deco--3"></div>

        <div class="login-page__brand-content">
            <div class="login-page__brand-icon">
                <Icon Name="graduation-cap" Size="40" />
            </div>
            <h1 class="login-page__brand-title">考勤管理系统</h1>
            <p class="login-page__brand-subtitle">智能考勤 &middot; 高效管理</p>
            <p class="login-page__brand-quote">让每一次考勤都有迹可循</p>
        </div>
    </div>

    @* 右侧登录表单区 *@
    <div class="login-page__form-side">
        <form method="post" action="/auth/login" class="login-form">
            <input type="hidden" name="__RequestVerificationToken" value="@requestToken" />

            <div class="login-form__header">
                <h2 class="login-form__title">欢迎回来</h2>
                <p class="login-form__subtitle">登录你的账号以继续</p>
            </div>

            @if (!string.IsNullOrEmpty(_errorMessage))
            {
                <div class="alert alert--error" role="alert">
                    <Icon Name="alert-circle" Size="18" />
                    <span>@_errorMessage</span>
                </div>
            }

            <div class="ui-input-wrapper">
                <span class="ui-input-icon">
                    <Icon Name="user" Size="18" />
                </span>
                <input id="username" name="Username" class="ui-input"
                       placeholder="学号 / 工号" required autocomplete="username" />
            </div>

            <div class="ui-input-wrapper">
                <span class="ui-input-icon">
                    <Icon Name="lock" Size="18" />
                </span>
                <input id="password" name="Password" type="password" class="ui-input"
                       placeholder="密码" required autocomplete="current-password" />
            </div>

            <button type="submit" class="login-form__submit">
                登录
            </button>
        </form>
    </div>
</div>

@code {
    [SupplyParameterFromQuery(Name = "error")]
    public string? ErrorMessage { get; set; }
    private string? _errorMessage;

    protected override void OnInitialized()
    {
        _errorMessage = ErrorMessage switch
        {
            "invalid" => "用户名或密码错误",
            "empty" => "用户名和密码不能为空",
            _ => null
        };
    }
}
```

- [ ] **Step 2: 重写 Login.razor.css**

```css
/* ---- 登录页布局 ---- */
.login-page {
    display: flex;
    min-height: 100vh;
    min-height: 100dvh;
}

/* ---- 左侧品牌区 ---- */
.login-page__brand {
    flex: 1;
    position: relative;
    background: linear-gradient(160deg, #4f46e5 0%, #7c3aed 40%, #a21caf 100%);
    display: flex;
    align-items: center;
    justify-content: center;
    overflow: hidden;
}

/* 浮动几何装饰 */
.login-page__deco {
    position: absolute;
    border-radius: 50%;
    opacity: 0.08;
    background: #ffffff;
}
.login-page__deco--1 {
    width: 400px; height: 400px;
    top: -100px; right: -100px;
    animation: float 12s ease-in-out infinite;
}
.login-page__deco--2 {
    width: 240px; height: 240px;
    bottom: 60px; left: -40px;
    animation: float-delayed 10s ease-in-out infinite;
}
.login-page__deco--3 {
    width: 160px; height: 160px;
    top: 40%; right: 60px;
    border-radius: var(--radius-xs);
    animation: float 14s ease-in-out infinite;
}

.login-page__brand-content {
    position: relative;
    text-align: center;
    color: var(--color-text-inverse);
    z-index: 1;
}
.login-page__brand-icon {
    margin-bottom: var(--space-6);
    opacity: 0.9;
}
.login-page__brand-title {
    font-size: 2.5rem;
    font-weight: var(--font-weight-bold);
    letter-spacing: var(--tracking-tight);
    margin-bottom: var(--space-3);
    color: var(--color-text-inverse);
}
.login-page__brand-subtitle {
    font-size: var(--text-lg);
    opacity: 0.75;
    margin-bottom: var(--space-8);
}
.login-page__brand-quote {
    font-size: var(--text-sm);
    opacity: 0.5;
    font-style: italic;
}

/* ---- 右侧表单区 ---- */
.login-page__form-side {
    flex: 1;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: var(--space-6);
    background: var(--color-surface);
}

.login-form {
    width: 100%;
    max-width: 360px;
}
.login-form__header {
    text-align: left;
    margin-bottom: var(--space-6);
}
.login-form__title {
    font-size: var(--text-2xl);
    font-weight: var(--font-weight-semibold);
    margin-bottom: var(--space-2);
    color: var(--color-text-primary);
}
.login-form__subtitle {
    font-size: var(--text-sm);
    color: var(--color-text-tertiary);
}
.login-form .ui-input-wrapper {
    margin-bottom: var(--space-4);
}

.login-form__submit {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 100%;
    height: 44px;
    margin-top: var(--space-6);
    font-size: var(--text-base);
    font-weight: var(--font-weight-medium);
    color: var(--color-text-inverse);
    background: var(--color-primary-gradient);
    border: none;
    border-radius: var(--radius-md);
    cursor: pointer;
    transition: all var(--duration-fast) var(--ease-out);
}
.login-form__submit:hover {
    box-shadow: var(--color-primary-glow);
    transform: translateY(-1px);
}
.login-form__submit:active {
    transform: translateY(0);
}

/* ---- 响应式 ---- */
@media (max-width: 767.98px) {
    .login-page { flex-direction: column; }
    .login-page__brand {
        height: 200px;
        flex: none;
    }
    .login-page__brand-title { font-size: var(--text-2xl); }
    .login-page__brand-subtitle { margin-bottom: 0; }
    .login-page__brand-quote { display: none; }
    .login-page__deco--1 { width: 200px; height: 200px; }
    .login-page__deco--2 { width: 120px; height: 120px; }
    .login-page__form-side { padding: var(--space-6) var(--space-4); }
}
```

- [ ] **Step 3: 更新 LoginLayout.razor**

简化布局，直接渲染 Body，让 Login.razor 自行管理布局：

```razor
@inherits LayoutComponentBase
@Body
```

- [ ] **Step 4: 清空 LoginLayout.razor.css**（布局移到 Login.razor.css）

---

### Task 8: 移除 Web 端的管理员页面

**Files:**
- Delete: `src/MyCollegeNew.Web/Components/Pages/Admin/` (整个目录)
- Modify: `src/MyCollegeNew.Web/Components/Layout/NavMenu.razor` — 移除 Admin 分支

- [ ] **Step 1: 删除 Admin 目录**

```bash
Remove-Item -Recurse -Force "src\MyCollegeNew.Web\Components\Pages\Admin"
```

- [ ] **Step 2: 更新 NavMenu.razor 的 GetMenuItems 方法，移除 Admin 分支**

确保 Task 6 Step 3 中的 NavMenu 代码不包含 `if (user.IsInRole("Admin"))` 分支。

- [ ] **Step 3: 更新 Routes.razor**

无需修改，Blazor 路由自动发现。Admin 页面已删除，路由自动消失。

- [ ] **Step 4: 删除不再使用的布局文件**

```bash
Remove-Item -Force "src\MyCollegeNew.Web\Components\Layout\AdminLayout.razor"
```

---

### Task 9: 创建 Admin 项目

**Files:**
- Create: `src/MyCollegeNew.Admin/` (完整项目结构)

- [ ] **Step 1: 创建项目目录结构**

```bash
New-Item -ItemType Directory -Force -Path "src\MyCollegeNew.Admin\Properties"
New-Item -ItemType Directory -Force -Path "src\MyCollegeNew.Admin\Components\Layout"
New-Item -ItemType Directory -Force -Path "src\MyCollegeNew.Admin\Components\Pages\Admin"
New-Item -ItemType Directory -Force -Path "src\MyCollegeNew.Admin\Components\Ui"
New-Item -ItemType Directory -Force -Path "src\MyCollegeNew.Admin\Services"
New-Item -ItemType Directory -Force -Path "src\MyCollegeNew.Admin\wwwroot\css"
```

- [ ] **Step 2: 创建 MyCollegeNew.Admin.csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <ItemGroup>
    <ProjectReference Include="..\MyCollegeNew.Shared\MyCollegeNew.Shared.csproj" />
    <ProjectReference Include="..\MyCollegeNew.Infrastructure\MyCollegeNew.Infrastructure.csproj" />
    <ProjectReference Include="..\MyCollegeNew.ServiceDefaults\MyCollegeNew.ServiceDefaults.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Serilog.AspNetCore" Version="9.0.0" />
    <PackageReference Include="Serilog.Sinks.Async" Version="2.1.0" />
  </ItemGroup>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>Larpx.PersonalTools.MyCollegeNew.Admin</RootNamespace>
    <BlazorDisableThrowNavigationException>true</BlazorDisableThrowNavigationException>
  </PropertyGroup>
</Project>
```

- [ ] **Step 3: 创建 Program.cs**

```csharp
using Larpx.PersonalTools.MyCollegeNew.Admin.Components;
using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Auth;
using Larpx.PersonalTools.MyCollegeNew.Shared.Responses;
using Larpx.PersonalTools.MyCollegeNew.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Serilog;
using System.Net.Http.Json;
using System.Security.Claims;

namespace Larpx.PersonalTools.MyCollegeNew.Admin;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Async(w => w.Console())
            .CreateLogger();
        builder.Host.UseSerilog();

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5144";
        var apiBase = apiBaseUrl.EndsWith("/") ? apiBaseUrl : apiBaseUrl + "/";
        builder.Services.AddHttpClient<ApiClient>(client =>
        {
            client.BaseAddress = new Uri(apiBase + "api/v1/");
        });
        builder.Services.AddScoped<IApiClient>(sp => sp.GetRequiredService<ApiClient>());
        builder.Services.AddHttpContextAccessor();

        builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("Jwt"));
        builder.Services.AddScoped<TokenService>();
        builder.Services.AddSingleton<ITokenService, Larpx.PersonalTools.MyCollegeNew.Infrastructure.Auth.TokenService>();
        builder.Services.AddScoped<CustomAuthStateProvider>();
        builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = "Cookies";
            options.DefaultChallengeScheme = "Cookies";
        })
        .AddCookie("Cookies", options =>
        {
            options.LoginPath = "/login";
            options.AccessDeniedPath = "/login";
            options.ExpireTimeSpan = TimeSpan.FromHours(2);
        });
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        });

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        app.UseStatusCodePagesWithReExecute("/not-found");
        app.MapDefaultEndpoints();

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();
        app.MapStaticAssets();

        // 登录端点
        app.MapPost("/auth/login", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
        {
            var username = context.Request.Form["Username"].FirstOrDefault();
            var password = context.Request.Form["Password"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                context.Response.Redirect("/login?error=empty");
                return;
            }

            try
            {
                var httpClient = httpClientFactory.CreateClient("ApiClient");
                var request = new LoginRequest { Username = username, Password = password };
                var apiResponse = await httpClient.PostAsJsonAsync("login", request);

                if (!apiResponse.IsSuccessStatusCode)
                {
                    context.Response.Redirect("/login?error=invalid");
                    return;
                }

                var result = await apiResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResult>>();
                if (result?.Data is null || string.IsNullOrEmpty(result.Data.Token))
                {
                    context.Response.Redirect("/login?error=invalid");
                    return;
                }

                // Admin 端仅允许 Admin 角色登录
                if (result.Data.Role != "Admin")
                {
                    context.Response.Redirect("/login?error=invalid");
                    return;
                }

                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, result.Data.UserId),
                    new Claim(ClaimTypes.Name, result.Data.UserName),
                    new Claim(ClaimTypes.Role, result.Data.Role),
                    new Claim("token", result.Data.Token)
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                context.Response.Redirect("/admin/dashboard");
            }
            catch
            {
                context.Response.Redirect("/login?error=invalid");
            }
        });

        // 登出端点
        app.MapPost("/auth/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Ok();
        });

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}
```

- [ ] **Step 4: 复制 CSS、Services 和 UI 组件到 Admin 项目**

从 Web 项目复制以下文件到 Admin 对应目录：
- `src/MyCollegeNew.Web/Services/` → `src/MyCollegeNew.Admin/Services/`
- `src/MyCollegeNew.Web/Components/Ui/` → `src/MyCollegeNew.Admin/Components/Ui/`
- `src/MyCollegeNew.Web/wwwroot/css/design-system.css` → `src/MyCollegeNew.Admin/wwwroot/css/design-system.css`
- `src/MyCollegeNew.Web/wwwroot/css/ui-components.css` → `src/MyCollegeNew.Admin/wwwroot/css/ui-components.css`
- `src/MyCollegeNew.Web/wwwroot/css/animations.css` → `src/MyCollegeNew.Admin/wwwroot/css/animations.css`
- `src/MyCollegeNew.Web/wwwroot/css/pages.css` → `src/MyCollegeNew.Admin/wwwroot/css/pages.css`

命令：
```bash
Copy-Item -Recurse "src\MyCollegeNew.Web\Services" "src\MyCollegeNew.Admin\"
Copy-Item -Recurse "src\MyCollegeNew.Web\Components\Ui" "src\MyCollegeNew.Admin\Components\"
Copy-Item "src\MyCollegeNew.Web\wwwroot\css\design-system.css" "src\MyCollegeNew.Admin\wwwroot\css\"
Copy-Item "src\MyCollegeNew.Web\wwwroot\css\ui-components.css" "src\MyCollegeNew.Admin\wwwroot\css\"
Copy-Item "src\MyCollegeNew.Web\wwwroot\css\animations.css" "src\MyCollegeNew.Admin\wwwroot\css\"
Copy-Item "src\MyCollegeNew.Web\wwwroot\css\pages.css" "src\MyCollegeNew.Admin\wwwroot\css\"
```

- [ ] **Step 5: 创建 Admin 端 CSS（admin.css）**

```css
/* ===================================================================
 * 管理端专用样式：深色侧边栏 + 桌面限定
 * =================================================================== */

.admin-layout {
    display: flex;
    min-height: 100vh;
    background-color: var(--color-bg);
}

/* ---- 深色侧边栏 ---- */
.admin-sidebar {
    width: 260px;
    flex-shrink: 0;
    background: var(--sidebar-bg);
    display: flex;
    flex-direction: column;
    position: sticky;
    top: 0;
    height: 100vh;
    overflow-y: auto;
    z-index: 100;
}

.admin-sidebar__brand {
    display: flex;
    align-items: center;
    gap: var(--space-3);
    padding: 0 var(--space-5);
    height: 64px;
    border-bottom: 1px solid var(--sidebar-border);
    color: var(--color-text-inverse);
}

.admin-sidebar__brand-text {
    font-size: var(--text-lg);
    font-weight: var(--font-weight-semibold);
    background: var(--color-primary-gradient);
    -webkit-background-clip: text;
    -webkit-text-fill-color: transparent;
    background-clip: text;
}

.admin-sidebar__nav {
    padding: var(--space-3);
    flex: 1;
}

.admin-sidebar__item {
    margin-bottom: var(--space-1);
}

.admin-sidebar__link {
    display: flex;
    align-items: center;
    gap: var(--space-3);
    padding: var(--space-2) var(--space-3);
    height: 40px;
    font-size: var(--text-sm);
    font-weight: var(--font-weight-medium);
    color: var(--sidebar-text-dim);
    border-radius: var(--radius-md);
    text-decoration: none;
    transition: all var(--duration-fast) var(--ease-out);
    position: relative;
}

.admin-sidebar__link:hover {
    background: var(--sidebar-active-bg);
    color: var(--sidebar-text);
}

.admin-sidebar__link--active {
    background: var(--sidebar-active-bg);
    color: var(--sidebar-active-text);
    font-weight: var(--font-weight-semibold);
}

.admin-sidebar__link--active::before {
    content: "";
    position: absolute;
    left: -12px;
    top: 50%;
    transform: translateY(-50%);
    width: 3px;
    height: 20px;
    background: var(--color-primary-gradient);
    border-radius: var(--radius-full);
}

/* ---- 主区域 ---- */
.admin-main {
    flex: 1;
    display: flex;
    flex-direction: column;
    min-width: 0;
}

/* ---- 顶部栏 ---- */
.admin-header {
    display: flex;
    align-items: center;
    padding: 0 var(--space-6);
    height: 56px;
    background: var(--color-surface);
    border-bottom: 1px solid var(--color-border);
    position: sticky;
    top: 0;
    z-index: 50;
}

.admin-header__spacer { flex: 1; }

.admin-header__user {
    display: flex;
    align-items: center;
    gap: var(--space-2);
}

.admin-header__logout {
    display: inline-flex;
    align-items: center;
    gap: var(--space-1);
    padding: var(--space-2);
    color: var(--color-text-tertiary);
    border-radius: var(--radius-sm);
    transition: all var(--duration-fast) var(--ease-out);
    margin-left: var(--space-3);
}

.admin-header__logout:hover {
    color: var(--color-error);
    background: var(--color-error-subtle);
}

/* ---- 内容区 ---- */
.admin-content {
    flex: 1;
    padding: var(--space-6);
    overflow-x: auto;
}

/* ---- 窗口过小提示 ---- */
.admin-min-warning {
    display: none;
}

@media (max-width: 1023.98px) {
    .admin-layout { display: none; }
    .admin-min-warning {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        min-height: 100vh;
        padding: var(--space-6);
        text-align: center;
        color: var(--color-text-secondary);
    }
}
```

- [ ] **Step 6: 创建 Admin 端布局文件**

**AdminMainLayout.razor：**
```razor
@inherits LayoutComponentBase
@inject NavigationManager NavigationManager
@inject CustomAuthStateProvider AuthStateProvider
@inject TokenService TokenService
@inject IApiClient ApiClient

<div class="admin-layout">
    <aside class="admin-sidebar">
        <AdminNavMenu />
    </aside>

    <div class="admin-main">
        <header class="admin-header">
            <div class="admin-header__spacer"></div>
            <AuthorizeView>
                <Authorized>
                    <div class="admin-header__user">
                        <span>Hallo,</span>
                        <span style="font-weight:var(--font-weight-medium)">@context.User.Identity?.Name</span>
                        <Badge Variant="BadgeVariant.Primary">管理员</Badge>
                    </div>
                    <button type="button" class="admin-header__logout" @onclick="HandleLogout">
                        <Icon Name="log-out" Size="18" />
                    </button>
                </Authorized>
            </AuthorizeView>
        </header>

        <main class="admin-content">
            @Body
        </main>
    </div>
</div>

<div class="admin-min-warning">
    <Icon Name="monitor" Size="48" />
    <h2 style="margin-top:var(--space-4)">请在桌面端使用</h2>
    <p>管理员端需要在屏幕宽度 1024px 以上的设备上操作</p>
</div>

<div id="blazor-error-ui" data-nosnippet>
    An unhandled error has occurred.
    <a href="." class="reload">Reload</a>
    <span class="dismiss">&times;</span>
</div>

@code {
    private async Task HandleLogout()
    {
        try { await ApiClient.PostNoContentAsync("auth/logout"); } catch { }
        TokenService.RemoveToken();
        AuthStateProvider.NotifyUserLogout();
        NavigationManager.NavigateTo("/login", forceLoad: true);
    }
}
```

**AdminNavMenu.razor：**
```razor
<nav class="admin-sidebar__nav">
    <div class="admin-sidebar__brand">
        <Icon Name="graduation-cap" Size="24" />
        <span class="admin-sidebar__brand-text">考勤管理</span>
    </div>

    <AuthorizeView>
        <Authorized>
            @foreach (var item in MenuItems)
            {
                <div class="admin-sidebar__item">
                    <NavLink class="admin-sidebar__link" href="@item.Href"
                             Match="@NavLinkMatch.Prefix"
                             ActiveClass="admin-sidebar__link--active">
                        <Icon Name="@item.Icon" Size="20" />
                        <span>@item.Title</span>
                    </NavLink>
                </div>
            }
        </Authorized>
    </AuthorizeView>
</nav>

@code {
    private static readonly List<MenuItem> MenuItems = new()
    {
        new("仪表盘", "/admin/dashboard", "layout-dashboard"),
        new("院系管理", "/admin/departments", "building"),
        new("学生管理", "/admin/students", "users"),
        new("教师管理", "/admin/teachers", "user"),
        new("课程管理", "/admin/courses", "book-open"),
        new("统计报表", "/admin/statistics", "bar-chart"),
    };

    private sealed record MenuItem(string Title, string Href, string Icon);
}
```

- [ ] **Step 7: 创建 Admin 端 App.razor、Routes.razor、_Imports.razor**

**App.razor：** 从 Web 端复制并调整路径
```razor
<!DOCTYPE html>
<html lang="zh-CN">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <base href="/" />
    <link rel="icon" type="image/png" href="favicon.png" />
    <link rel="stylesheet" href="css/design-system.css" />
    <link rel="stylesheet" href="css/ui-components.css" />
    <link rel="stylesheet" href="css/animations.css" />
    <link rel="stylesheet" href="css/admin.css" />
    <link rel="stylesheet" href="css/pages.css" />
    <HeadOutlet @rendermode="InteractiveServer" />
</head>
<body>
    <Routes @rendermode="InteractiveServer" />
    <script src="_framework/blazor.web.js"></script>
</body>
</html>
```

**Routes.razor：** 管理员端路由（AdminOnly 策略 + AdminMainLayout）
```razor
@using Microsoft.AspNetCore.Authorization

<Router AppAssembly="typeof(Program).Assembly" NotFoundPage="typeof(Pages.NotFound)">
    <Found Context="routeData">
        <AuthorizeRouteView RouteData="routeData" DefaultLayout="typeof(Layout.AdminMainLayout)">
            <NotAuthorized>
                <RedirectToLogin />
            </NotAuthorized>
        </AuthorizeRouteView>
        <FocusOnNavigate RouteData="routeData" Selector="h1" />
    </Found>
</Router>
```

**AccessGuard.razor：** 从 Web 端复制

**_Imports.razor：**
```razor
@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Authorization
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Authorization
@using Microsoft.JSInterop
@using Larpx.PersonalTools.MyCollegeNew.Admin
@using Larpx.PersonalTools.MyCollegeNew.Admin.Components
@using Larpx.PersonalTools.MyCollegeNew.Admin.Components.Layout
@using Larpx.PersonalTools.MyCollegeNew.Admin.Components.Ui
@using Larpx.PersonalTools.MyCollegeNew.Web.Services
@using Larpx.PersonalTools.MyCollegeNew.Shared.Features.Auth
@using Larpx.PersonalTools.MyCollegeNew.Shared.Responses
@using Microsoft.AspNetCore.Http
```

- [ ] **Step 8: 创建 Admin 端页面**

从 `src/MyCollegeNew.Web/Components/Pages/Admin/` 备份中恢复 Admin 页面到 `src/MyCollegeNew.Admin/Components/Pages/Admin/`：
- Dashboard.razor
- Departments.razor + DepartmentNode.razor
- Students.razor
- Teachers.razor
- Courses.razor
- Statistics.razor

以及 Login.razor + Login.razor.css（与 Web 端使用相同的左右分屏设计，但 `@layout` 指向 `LoginLayout`）。

**LoginLayout.razor（Admin 端）：**
```razor
@inherits LayoutComponentBase
@Body
```

- [ ] **Step 9: 创建 Admin 端 appsettings.json 和 launchSettings.json**

**appsettings.json：**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Api": {
    "BaseUrl": "http://localhost:5144"
  },
  "Jwt": {
    "Secret": "my-college-project-jwt-secret-key-2024",
    "Issuer": "MyCollegeNew.Api",
    "Audience": "MyCollegeNew.Web",
    "ExpireHours": 8
  }
}
```

**Properties/launchSettings.json：**
```json
{
  "profiles": {
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "https://localhost:7250;http://localhost:5250",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

---

### Task 10: 验证构建

- [ ] **Step 1: 构建 Web 项目**

```bash
dotnet build src\MyCollegeNew.Web\MyCollegeNew.Web.csproj
```
预期：0 错误，0 警告

- [ ] **Step 2: 构建 Admin 项目**

```bash
dotnet build src\MyCollegeNew.Admin\MyCollegeNew.Admin.csproj
```
预期：0 错误，0 警告（可能有命名空间引用问题，根据实际情况调整 _Imports.razor）

- [ ] **Step 3: 修复构建问题**

根据 `dotnet build` 的错误输出修复：
- 缺失的 `using` 语句 → 添加到 _Imports.razor 或文件顶部
- 类型引用错误 → 确保 Services/CustomAuthStateProvider.cs 等文件的命名空间与新项目一致
- 缺失文件 → 检查是否遗漏复制

- [ ] **Step 4: 构建全部项目**

```bash
dotnet build
```
预期：全部项目 0 错误，0 警告

---

### Task 11: 运行验证

- [ ] **Step 1: 启动 API**

```bash
dotnet run --project src\MyCollegeNew.Api --urls http://localhost:5144
```

- [ ] **Step 2: 启动 Web 用户端**

```bash
dotnet run --project src\MyCollegeNew.Web --urls http://localhost:5249
```

- [ ] **Step 3: 启动 Admin 管理端**

```bash
dotnet run --project src\MyCollegeNew.Admin --urls http://localhost:5250
```

- [ ] **Step 4: 用 Playwright 验证**

1. 浏览器访问 `http://localhost:5249/login` → 验证左右分屏登录页
2. 输入 admin / 123456 登录 → 验证跳转到 `/admin/dashboard`（即使 Web 端 Admin 页面已移除，登录重定向逻辑仍在 Program.cs 的 `/auth/login` 端点中）
3. 浏览器访问 `http://localhost:5250/login` → 验证 Admin 端登录
4. 输入 admin / 123456 → 验证跳转到 `/admin/dashboard`，页面正常加载
5. 用教师账号登录用户端 → 验证仪表盘正常
6. 缩窄浏览器宽度到 <768px → 验证底部 TabBar 出现
7. 验证平板模式（768-1024px）→ 侧边栏折叠为图标

---

## 自审

**1. 规格覆盖：** 设计文档的所有要求均已映射到任务：CSS 层（T1-T4）→ 组件升级（T5）→ 布局/登录重写（T6-T7）→ Admin 页面移除（T8）→ Admin 新建（T9）→ 构建验证（T10-T11）

**2. 占位符扫描：** 无 TBD / TODO / "add appropriate" / "test the above" 等模式

**3. 类型一致性：** 所有组件参数名与 CSS 类名一致（`ButtonVariant.Gradient` → `ui-btn--gradient`，`CardAccent.Success` → `ui-card--stat-success`），TabBar.TabItem 类同时被 MainLayout.razor 和 TabBar.razor 引用

---

**计划完成。两种执行方式：**

**1. Subagent-Driven（推荐）** — 每任务一个独立子 Agent，任务间审查，快速迭代

**2. Inline Execution** — 当前会话内使用 executing-plans 逐步执行，批量提交

**哪种方式？**
