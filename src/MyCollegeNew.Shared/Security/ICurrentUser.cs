using Larpx.PersonalTools.MyCollegeNew.Shared.Enums;

namespace Larpx.PersonalTools.MyCollegeNew.Shared.Security
{
    /// <summary>
    /// 当前登录用户上下文接口
    /// </summary>
    public interface ICurrentUser
    {
        /// <summary>
        /// 用户ID（学号/工号；Admin 角色为 SystemUser.Id 数字字符串）
        /// 该字段用于业务实体按主键查询（Student.Id / Teacher.Id / SystemUser.Id 字符串化）
        /// </summary>
        string UserId { get; }

        /// <summary>
        /// 系统用户主键（仅 Admin 角色有值，对应 SystemUser.Id）；
        /// 用于 SystemUser 表的自助操作校验（修改自身密码、禁止自删除等），避免与 UserId 类型混淆
        /// </summary>
        long? SystemUserId { get; }

        /// <summary>用户名</summary>
        string UserName { get; }

        /// <summary>用户角色</summary>
        UserRole Role { get; }

        /// <summary>是否已认证</summary>
        bool IsAuthenticated { get; }
    }
}