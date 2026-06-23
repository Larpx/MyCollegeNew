namespace Campus.Attendance.Core.Enums;

/// <summary>
/// 系统用户角色枚举，用于权限控制与角色路由
/// </summary>
public enum UserRole
{
    /// <summary>系统管理员，拥有全部权限</summary>
    Admin,

    /// <summary>任课教师，负责发起考勤与课程管理</summary>
    Teacher,

    /// <summary>辅导员，负责学生管理与请假审批</summary>
    Counselor,

    /// <summary>学生，参与签到与请假申请</summary>
    Student
}
