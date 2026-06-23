using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


/// <summary>
/// 班级信息数据模型
/// </summary>
public class Classes
{

    /// <summary>
    /// 班级编号
    /// </summary>
    public int cla_ID
    {
        get;
        set;
    }

    /// <summary>
    /// 班级所属专业ID
    /// </summary>
    public int cla_majID
    {
        get;
        set;
    }

    /// <summary>
    /// 班级名称
    /// </summary>
    public string cla_name
    {
        get;
        set;
    }

    /// <summary>
    /// 班级辅导员编号
    /// </summary>
    public string cla_insID
    {
        get;
        set;
    }
        
    /// <summary>
    /// 班级年级
    /// </summary>
    public int cla_grade
    {
        get;
        set;
    }

}
