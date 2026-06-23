using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


/// <summary>
/// 课程信息数据模型
/// </summary>
public class Lession
{
    /// <summary>
    /// 课程ID
    /// </summary>
    public int les_ID
    {
        get;
        set;
    }

    /// <summary>
    /// 课程名称
    /// </summary>
    public string les_name
    {
        get;
        set;
    }

    /// <summary>
    /// 教师ID
    /// </summary>
    public string les_teacherID
    {
        get;
        set;
    }

    /// <summary>
    /// 备注信息
    /// </summary>
    public string les_other
    {
        get;
        set;
    }
}
