using SqlSugar;

namespace Larpx.PersonalTools.MyCollegeNew.Shared.Entities
{
    /// <summary>
    /// 实体基类，统一包含创建时间、更新时间与软删除标记，供业务实体继承
    /// </summary>
    public abstract class EntityBase
    {
        /// <summary>创建时间（UTC）</summary>
        [SugarColumn(IsOnlyIgnoreUpdate = true, ColumnDescription = "创建时间")]
        public DateTime CreateTime { get; set; } = DateTime.UtcNow;

        /// <summary>最后更新时间（UTC），首次创建时与 CreateTime 一致</summary>
        [SugarColumn(ColumnDescription = "更新时间")]
        public DateTime? UpdateTime { get; set; }

        /// <summary>软删除标记，true 表示已逻辑删除</summary>
        [SugarColumn(ColumnDescription = "是否删除")]
        public bool IsDeleted { get; set; }
    }
}