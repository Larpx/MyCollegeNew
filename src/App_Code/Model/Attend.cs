using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// 考勤信息数据模型
/// </summary>
public class Attend
{
    /// <summary>
    /// 考勤记录表的ID，主键，自增，不需要赋值
    /// </summary>
    //public int att_ID
    //{
    //    get;
    //    set;
    //}

    /// <summary>
    /// 考勤记录对应的学生学号
    /// </summary>
    public string att_stuId
    {
        get;
        set;
    }

    /// <summary>
    /// 考勤记录对应的学生姓名
    /// </summary>
    public string att_name
    {
        get;
        set;
    }

    /// <summary>
    /// 记录对应的课程ID
    /// </summary>
    public int att_lesid
    {
        get;
        set;
    }

    /// <summary>
    /// 本次课的上课时间
    /// </summary>
    public DateTime att_time
    {
        get;
        set;
    }

    /// <summary>
    /// 上课第一次签到
    /// </summary>
    public int att_att1
    {
        get;
        set;
    }
        
    /// <summary>
    /// 临近下课时的第二次签到
    /// </summary>
    public  int att_att2
    {
        get;
        set;
    }

}
