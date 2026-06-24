namespace Campus.Attendance.Core.Constants;

/// <summary>
/// 消息常量集中定义，将项目中散落的中文提示字符串统一管理，便于国际化与维护
/// </summary>
public static class MessageConstants
{
    /// <summary>
    /// 通用消息，适用于跨领域的公共提示场景
    /// </summary>
    public static class Common
    {
        /// <summary>实体不存在（参数：实体标识，如 "课程 1"）</summary>
        public static string EntityNotFound(string entity) => $"{entity} 不存在";

        /// <summary>操作成功</summary>
        public const string OperationSuccess = "操作成功";

        /// <summary>创建成功</summary>
        public const string CreateSuccess = "创建成功";

        /// <summary>修改成功</summary>
        public const string UpdateSuccess = "修改成功";

        /// <summary>删除成功</summary>
        public const string DeleteSuccess = "删除成功";

        /// <summary>登录成功</summary>
        public const string LoginSuccess = "登录成功";

        /// <summary>登出成功</summary>
        public const string LogoutSuccess = "登出成功";

        /// <summary>用户名或密码错误</summary>
        public const string InvalidCredentials = "用户名或密码错误";

        /// <summary>无权限执行此操作</summary>
        public const string NoPermission = "无权限执行此操作";

        /// <summary>网络异常，请稍后重试</summary>
        public const string NetworkError = "网络异常，请稍后重试";

        /// <summary>登录已过期，请重新登录</summary>
        public const string TokenExpired = "登录已过期，请重新登录";

        /// <summary>响应格式错误</summary>
        public const string ResponseFormatError = "响应格式错误";

        /// <summary>响应为空</summary>
        public const string ResponseEmpty = "响应为空";

        /// <summary>请求失败</summary>
        public const string RequestFailed = "请求失败";

        /// <summary>下载失败</summary>
        public const string DownloadFailed = "下载失败";

        /// <summary>服务器内部错误</summary>
        public const string ServerError = "服务器内部错误";

        /// <summary>修改成功</summary>
        public const string ChangeSuccess = "修改成功";

        /// <summary>密码修改成功</summary>
        public const string PasswordChangeSuccess = "密码修改成功";

        /// <summary>导入完成</summary>
        public const string ImportComplete = "导入完成";

        /// <summary>请上传有效的 CSV 文件</summary>
        public const string InvalidCsvFile = "请上传有效的 CSV 文件";
    }

    /// <summary>
    /// 考勤领域消息，涵盖签到、会话、二维码、点名等业务场景
    /// </summary>
    public static class Attendance
    {
        /// <summary>签到成功</summary>
        public const string CheckInSuccess = "签到成功";

        /// <summary>签到成功（迟到）</summary>
        public const string CheckInSuccessLate = "签到成功（迟到）";

        /// <summary>签到成功（已超时，记为缺勤）</summary>
        public const string CheckInSuccessTimeout = "签到成功（已超时，记为缺勤）";

        /// <summary>会话已关闭，无法生成二维码</summary>
        public const string SessionClosed = "会话已关闭，无法生成二维码";

        /// <summary>会话已关闭，无法签到</summary>
        public const string SessionClosedCheckIn = "会话已关闭，无法签到";

        /// <summary>会话已关闭，无需重复操作</summary>
        public const string SessionAlreadyClosed = "会话已关闭，无需重复操作";

        /// <summary>已签到，请勿重复签到</summary>
        public const string DuplicateCheckIn = "已签到，请勿重复签到";

        /// <summary>学生不属于该考勤班级</summary>
        public const string StudentNotInClass = "学生不属于该考勤班级";

        /// <summary>仅可为自己负责的课程创建考勤会话</summary>
        public const string OnlyOwnCourse = "仅可为自己负责的课程创建考勤会话";

        /// <summary>仅可修改自己发起的考勤记录</summary>
        public const string OnlyOwnRecord = "仅可修改自己发起的考勤记录";

        /// <summary>二维码生成成功</summary>
        public const string QrCodeGenerated = "二维码生成成功";

        /// <summary>一键点名完成（参数：标记学生人数）</summary>
        public static string RollCallComplete(int count) => $"一键点名完成，共标记 {count} 名学生";

        /// <summary>教师一键点名（考勤记录备注）</summary>
        public const string ManualCheckInRemark = "教师一键点名";

        /// <summary>会话关闭自动标记缺勤（考勤记录备注）</summary>
        public const string AutoAbsentRemark = "会话关闭自动标记缺勤";

        /// <summary>请假审批通过自动更新（考勤记录备注）</summary>
        public const string LeaveApprovedRemark = "请假审批通过自动更新";

        /// <summary>签到结束时间必须晚于开始时间</summary>
        public const string EndTimeMustAfterStart = "签到结束时间必须晚于开始时间";

        /// <summary>班级中暂无学生</summary>
        public const string ClassNoStudents = "班级中暂无学生";

        /// <summary>仅可操作自己发起的考勤会话</summary>
        public const string OnlyOwnSession = "仅可操作自己发起的考勤会话";

        /// <summary>签到令牌不能为空</summary>
        public const string QrTokenEmpty = "签到令牌不能为空";

        /// <summary>签到令牌无效</summary>
        public const string QrTokenInvalid = "签到令牌无效";

        /// <summary>签到令牌与会话不匹配</summary>
        public const string QrTokenSessionMismatch = "签到令牌与会话不匹配";

        /// <summary>签到令牌已过期或无效</summary>
        public const string QrTokenExpired = "签到令牌已过期或无效";

        /// <summary>手动补签</summary>
        public const string ManualCheckIn = "手动补签";

        /// <summary>补签成功</summary>
        public const string ManualCheckInSuccess = "补签成功";

        /// <summary>标记成功</summary>
        public const string MarkSuccess = "标记成功";
    }

    /// <summary>
    /// 认证领域消息，涵盖登录、密码校验、角色校验等场景
    /// </summary>
    public static class Auth
    {
        /// <summary>用户不存在</summary>
        public const string UserNotFound = "用户不存在";

        /// <summary>旧密码不正确</summary>
        public const string OldPasswordIncorrect = "旧密码不正确";

        /// <summary>不支持的用户角色</summary>
        public const string UnsupportedRole = "不支持的用户角色";
    }

    /// <summary>
    /// 用户管理领域消息，涵盖学生/教师的唯一性校验与 CSV 导入等场景
    /// </summary>
    public static class User
    {
        /// <summary>学号已存在（参数：学号）</summary>
        public static string StudentIdExists(string studentId) => $"学号 {studentId} 已存在";

        /// <summary>工号已存在（参数：工号）</summary>
        public static string TeacherIdExists(string teacherId) => $"工号 {teacherId} 已存在";

        /// <summary>CSV 字段数不足（参数：期望字段数）</summary>
        public static string CsvColumnInsufficient(int expectedCount) => $"字段数不足，期望 {expectedCount} 个";
    }

    /// <summary>
    /// 组织架构领域消息，涵盖院系/专业/班级的删除约束与辅导员配置校验
    /// </summary>
    public static class Organization
    {
        /// <summary>院系下存在专业，无法删除（参数：院系标识）</summary>
        public static string DepartmentHasMajors(object departmentId) => $"院系 {departmentId} 下存在专业，无法删除";

        /// <summary>专业下存在班级，无法删除（参数：专业标识）</summary>
        public static string MajorHasClasses(object majorId) => $"专业 {majorId} 下存在班级，无法删除";

        /// <summary>学生所属班级未配置辅导员</summary>
        public const string ClassCounselorNotConfigured = "学生所属班级未配置辅导员";

        /// <summary>学生所属班级不存在</summary>
        public const string StudentClassNotFound = "学生所属班级不存在";
    }

    /// <summary>
    /// 课程与课表领域消息，涵盖节次/周次范围校验
    /// </summary>
    public static class Course
    {
        /// <summary>起始节次不能大于结束节次</summary>
        public const string StartSectionAfterEnd = "起始节次不能大于结束节次";

        /// <summary>起始周次不能大于结束周次</summary>
        public const string StartWeekAfterEnd = "起始周次不能大于结束周次";
    }

    /// <summary>
    /// 请假领域消息，涵盖时间校验、重复审批与归属校验
    /// </summary>
    public static class Leave
    {
        /// <summary>请假结束时间必须晚于开始时间</summary>
        public const string LeaveEndTimeMustAfterStart = "请假结束时间必须晚于开始时间";

        /// <summary>请假申请已审批，无法重复操作</summary>
        public const string LeaveAlreadyReviewed = "请假申请已审批，无法重复操作";

        /// <summary>仅可审批分配给自己的请假申请</summary>
        public const string OnlyOwnLeave = "仅可审批分配给自己的请假申请";

        /// <summary>请假申请已提交</summary>
        public const string LeaveSubmitted = "请假申请已提交";

        /// <summary>审批通过</summary>
        public const string ApproveSuccess = "审批通过";

        /// <summary>已驳回</summary>
        public const string RejectSuccess = "已驳回";
    }

    /// <summary>
    /// 统计领域消息，涵盖日期区间校验
    /// </summary>
    public static class Statistics
    {
        /// <summary>结束日期不能早于开始日期</summary>
        public const string EndDateBeforeStart = "结束日期不能早于开始日期";

        /// <summary>仅可查询自己的考勤统计</summary>
        public const string OnlyOwnStatistics = "仅可查询自己的考勤统计";
    }
}
