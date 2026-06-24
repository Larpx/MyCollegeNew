using Campus.Attendance.Core.Enums;
using Xunit;

namespace Campus.Attendance.Tests.Extensions;

/// <summary>
/// AttendanceStatusExtensions 单元测试，覆盖所有枚举值的中文显示名称映射及默认分支
/// </summary>
public class AttendanceStatusExtensionsTests
{
    /// <summary>
    /// Present 状态应返回 "正常"
    /// </summary>
    [Fact]
    public void GetDisplayName_Present_ReturnsNormal()
    {
        // Arrange
        var status = AttendanceStatus.Present;

        // Act
        var displayName = status.GetDisplayName();

        // Assert
        Assert.Equal("正常", displayName);
    }

    /// <summary>
    /// Late 状态应返回 "迟到"
    /// </summary>
    [Fact]
    public void GetDisplayName_Late_ReturnsLate()
    {
        // Arrange
        var status = AttendanceStatus.Late;

        // Act
        var displayName = status.GetDisplayName();

        // Assert
        Assert.Equal("迟到", displayName);
    }

    /// <summary>
    /// Absent 状态应返回 "缺勤"
    /// </summary>
    [Fact]
    public void GetDisplayName_Absent_ReturnsAbsent()
    {
        // Arrange
        var status = AttendanceStatus.Absent;

        // Act
        var displayName = status.GetDisplayName();

        // Assert
        Assert.Equal("缺勤", displayName);
    }

    /// <summary>
    /// Leave 状态应返回 "请假"
    /// </summary>
    [Fact]
    public void GetDisplayName_Leave_ReturnsLeave()
    {
        // Arrange
        var status = AttendanceStatus.Leave;

        // Act
        var displayName = status.GetDisplayName();

        // Assert
        Assert.Equal("请假", displayName);
    }

    /// <summary>
    /// 未定义的枚举值应返回枚举的 ToString() 结果
    /// </summary>
    [Fact]
    public void GetDisplayName_UndefinedValue_ReturnsToString()
    {
        // Arrange - 使用强制类型转换模拟未定义的枚举值
        var status = (AttendanceStatus)99;

        // Act
        var displayName = status.GetDisplayName();

        // Assert
        Assert.Equal("99", displayName);
    }
}
