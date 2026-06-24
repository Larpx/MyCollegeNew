namespace Larpx.PersonalTools.MyCollegeNew.Web.Components.Ui
{
/// <summary>
/// 按钮变体枚举，控制按钮样式
/// </summary>
public enum ButtonVariant
{
    /// <summary>主按钮（实心填充）</summary>
    Primary,

    /// <summary>次按钮（轮廓）</summary>
    Secondary,

    /// <summary>危险按钮（红色实心）</summary>
    Danger,

    /// <summary>幽灵按钮（仅边框）</summary>
    Outline
}

/// <summary>
/// 徽章变体枚举，控制徽章颜色
/// </summary>
public enum BadgeVariant
{
    /// <summary>成功（绿色）</summary>
    Success,

    /// <summary>警告（橙色）</summary>
    Warning,

    /// <summary>错误（红色）</summary>
    Error,

    /// <summary>中性（灰色）</summary>
    Neutral,

    /// <summary>主色（蓝色）</summary>
    Primary
}
}