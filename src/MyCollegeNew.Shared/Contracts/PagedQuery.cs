namespace Larpx.PersonalTools.MyCollegeNew.Shared.Contracts
{
    /// <summary>
    /// 分页查询基类
    /// </summary>
    public abstract record PagedQuery
    {
        /// <summary>页码（从1开始）</summary>
        public int PageIndex { get; set; } = 1;

        /// <summary>每页大小</summary>
        public int PageSize { get; set; } = 10;
    }
}