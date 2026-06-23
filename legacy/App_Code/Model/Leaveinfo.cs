using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// 请假表数据模型
/// </summary>
public class Leaveinfo
{
    /// <summary>
    /// 请假单编号，自增变量，只读
    /// </summary>
    public int lea_ID
    {
        get;
        set;
    }

    /// <summary>
    /// 请假学生学号
    /// </summary>
    public string lea_stuID
    {
        get;
        set;
    }

    /// <summary>
    /// 辅导员编号
    /// </summary>
    public string lea_conID
    {
        get;
        set;
    }

    /// <summary>
    /// 请假开始时间
    /// </summary>
    public DateTime lea_time1
    {
        get;
        set;
    }

    /// <summary>
    /// 请假结束时间
    /// </summary>
    public DateTime lea_time2
    {
        get;
        set;
    }

    /// <summary>
    /// 请假时间段内课程数量
    /// </summary>
    public int lea_times
    {
        get;
        set;
    }

    /// <summary>
    /// 请假类型
    /// </summary>
    public int lea_info
    {
        get;
        set;
    }

    /// <summary>
    /// 请假备注
    /// </summary>
    public string lea_other
    {
        get;
        set;
    }
}
