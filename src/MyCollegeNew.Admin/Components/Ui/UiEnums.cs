namespace Larpx.PersonalTools.MyCollegeNew.Admin.Components.Ui
{
    /// <summary>
    /// 按钮变体枚举，控制按钮样式
    /// </summary>
    public enum ButtonVariant
    {
        /// <summary>主按钮（渐变填充）</summary>
        Primary,

        /// <summary>次按钮（轮廓）</summary>
        Secondary,

        /// <summary>危险按钮（红色实心）</summary>
        Danger,

        /// <summary>幽灵按钮（透明底）</summary>
        Ghost,

        /// <summary>渐变按钮（等同于 Primary）</summary>
        Gradient,

        /// <summary>图标按钮（仅图标）</summary>
        Icon
    }

    /// <summary>
    /// 按钮尺寸枚举
    /// </summary>
    public enum ButtonSize
    {
        /// <summary>小尺寸（32px）</summary>
        Sm,

        /// <summary>中等尺寸（40px，默认）</summary>
        Md,

        /// <summary>大尺寸（48px）</summary>
        Lg
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

        /// <summary>主色（紫蓝色）</summary>
        Primary
    }

    /// <summary>
    /// 卡片强调色枚举，用于统计卡片顶部渐变条
    /// </summary>
    public enum CardAccent
    {
        /// <summary>无强调</summary>
        None,

        /// <summary>主色（紫蓝渐变）</summary>
        Primary,

        /// <summary>成功（绿色渐变）</summary>
        Success,

        /// <summary>警告（橙色渐变）</summary>
        Warning,

        /// <summary>错误（红色渐变）</summary>
        Error
    }

    /// <summary>
    /// 统计卡片强调色枚举
    /// </summary>
    public enum StatAccent
    {
        /// <summary>主色（紫蓝渐变）</summary>
        Primary,

        /// <summary>成功（绿色渐变）</summary>
        Success,

        /// <summary>警告（橙色渐变）</summary>
        Warning,

        /// <summary>错误（红色渐变）</summary>
        Error
    }

    /// <summary>
    /// 骨架屏类型枚举
    /// </summary>
    public enum SkeletonType
    {
        /// <summary>文本行</summary>
        Text,

        /// <summary>标题</summary>
        Title,

        /// <summary>头像</summary>
        Avatar,

        /// <summary>卡片</summary>
        Card
    }
}
