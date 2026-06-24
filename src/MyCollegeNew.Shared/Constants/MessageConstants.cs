namespace Larpx.PersonalTools.MyCollegeNew.Shared.Constants
{
/// <summary>
/// 消息常量集中定义
/// </summary>
public static class MessageConstants
{
    /// <summary>通用消息</summary>
    public static class Common
    {
        public static string EntityNotFound(string entity) => $"{entity} 不存在";
        public const string OperationSuccess = "操作成功";
        public const string CreateSuccess = "创建成功";
        public const string UpdateSuccess = "修改成功";
        public const string DeleteSuccess = "删除成功";
        public const string LoginSuccess = "登录成功";
        public const string LogoutSuccess = "登出成功";
        public const string InvalidCredentials = "用户名或密码错误";
        public const string NoPermission = "无权限执行此操作";
        public const string NetworkError = "网络异常，请稍后重试";
        public const string TokenExpired = "登录已过期，请重新登录";
        public const string ResponseFormatError = "响应格式错误";
        public const string ResponseEmpty = "响应为空";
        public const string RequestFailed = "请求失败";
        public const string DownloadFailed = "下载失败";
        public const string ServerError = "服务器内部错误";
        public const string ChangeSuccess = "修改成功";
        public const string PasswordChangeSuccess = "密码修改成功";
        public const string ImportComplete = "导入完成";
        public const string InvalidCsvFile = "请上传有效的 CSV 文件";
    }

    /// <summary>考勤领域消息</summary>
    public static class Attendance
    {
        public const string CheckInSuccess = "签到成功";
        public const string CheckInSuccessLate = "签到成功（迟到）";
        public const string CheckInSuccessTimeout = "签到成功（已超时，记为缺勤）";
        public const string SessionClosed = "会话已关闭，无法生成二维码";
        public const string SessionClosedCheckIn = "会话已关闭，无法签到";
        public const string SessionAlreadyClosed = "会话已关闭，无需重复操作";
        public const string DuplicateCheckIn = "已签到，请勿重复签到";
        public const string StudentNotInClass = "学生不属于该考勤班级";
        public const string OnlyOwnCourse = "仅可为自己负责的课程创建考勤会话";
        public const string OnlyOwnRecord = "仅可修改自己发起的考勤记录";
        public const string QrCodeGenerated = "二维码生成成功";
        public static string RollCallComplete(int count) => $"一键点名完成，共标记 {count} 名学生";
        public const string ManualCheckInRemark = "教师一键点名";
        public const string AutoAbsentRemark = "会话关闭自动标记缺勤";
        public const string LeaveApprovedRemark = "请假审批通过自动更新";
        public const string EndTimeMustAfterStart = "签到结束时间必须晚于开始时间";
        public const string ClassNoStudents = "班级中暂无学生";
        public const string OnlyOwnSession = "仅可操作自己发起的考勤会话";
        public const string QrTokenEmpty = "签到令牌不能为空";
        public const string QrTokenInvalid = "签到令牌无效";
        public const string QrTokenSessionMismatch = "签到令牌与会话不匹配";
        public const string QrTokenExpired = "签到令牌已过期或无效";
        public const string ManualCheckIn = "手动补签";
        public const string ManualCheckInSuccess = "补签成功";
        public const string MarkSuccess = "标记成功";
    }

    /// <summary>认证领域消息</summary>
    public static class Auth
    {
        public const string UserNotFound = "用户不存在";
        public const string OldPasswordIncorrect = "旧密码不正确";
        public const string UnsupportedRole = "不支持的用户角色";
    }

    /// <summary>用户管理领域消息</summary>
    public static class User
    {
        public static string StudentIdExists(string studentId) => $"学号 {studentId} 已存在";
        public static string TeacherIdExists(string teacherId) => $"工号 {teacherId} 已存在";
        public static string CsvColumnInsufficient(int expectedCount) => $"字段数不足，期望 {expectedCount} 个";
    }

    /// <summary>组织架构领域消息</summary>
    public static class Organization
    {
        public static string DepartmentHasMajors(object departmentId) => $"院系 {departmentId} 下存在专业，无法删除";
        public static string MajorHasClasses(object majorId) => $"专业 {majorId} 下存在班级，无法删除";
        public const string ClassCounselorNotConfigured = "学生所属班级未配置辅导员";
        public const string StudentClassNotFound = "学生所属班级不存在";
    }

    /// <summary>课程与课表领域消息</summary>
    public static class Course
    {
        public const string StartSectionAfterEnd = "起始节次不能大于结束节次";
        public const string StartWeekAfterEnd = "起始周次不能大于结束周次";
    }

    /// <summary>请假领域消息</summary>
    public static class Leave
    {
        public const string LeaveEndTimeMustAfterStart = "请假结束时间必须晚于开始时间";
        public const string LeaveAlreadyReviewed = "请假申请已审批，无法重复操作";
        public const string OnlyOwnLeave = "仅可审批分配给自己的请假申请";
        public const string LeaveSubmitted = "请假申请已提交";
        public const string ApproveSuccess = "审批通过";
        public const string RejectSuccess = "已驳回";
    }

    /// <summary>统计领域消息</summary>
    public static class Statistics
    {
        public const string EndDateBeforeStart = "结束日期不能早于开始日期";
        public const string OnlyOwnStatistics = "仅可查询自己的考勤统计";
    }
}
}