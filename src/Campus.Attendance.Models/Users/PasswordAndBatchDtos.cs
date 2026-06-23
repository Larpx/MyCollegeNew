using System.ComponentModel.DataAnnotations;

namespace Campus.Attendance.Models.Users;

/// <summary>
/// 修改密码 DTO
/// </summary>
public class PasswordChangeDto
{
    /// <summary>旧密码</summary>
    [Required(ErrorMessage = "旧密码不能为空")]
    public string OldPassword { get; set; } = string.Empty;

    /// <summary>新密码（不少于 6 个字符）</summary>
    [Required(ErrorMessage = "新密码不能为空")]
    [StringLength(128, MinimumLength = 6, ErrorMessage = "新密码长度需在 6-128 个字符之间")]
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// 批量导入结果 DTO
/// </summary>
public class BatchImportResultDto
{
    /// <summary>成功导入数量</summary>
    public int SuccessCount { get; set; }

    /// <summary>失败数量</summary>
    public int FailedCount { get; set; }

    /// <summary>失败明细列表（行号 + 原因）</summary>
    public List<BatchImportFailureItem> Failures { get; set; } = new();
}

/// <summary>
/// 批量导入失败明细项
/// </summary>
public class BatchImportFailureItem
{
    /// <summary>CSV 行号（从 1 开始，不含表头）</summary>
    public int Row { get; set; }

    /// <summary>失败原因</summary>
    public string Reason { get; set; } = string.Empty;
}
