using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


/// <summary>
/// 教师数据模型类
/// </summary>
public class Teacher
{
    /// <summary>
    /// 教师ID 
    /// </summary>
    public string tea_ID
    {
        get;
        set;
    }

    /// <summary>
    /// 教师姓名
    /// </summary>
    public string tif_name
    {
        get;
        set;
    }

    /// <summary>
    /// 教师密码
    /// </summary>
    public string tif_password
    {
        get;
        set;
    }

    /// <summary>
    /// 教师所在专业
    /// </summary>
    public int tif_major        
    {
        get;
        set;
    } 

    /// <summary>
    /// 教师所在院系
    /// </summary>
    public int tif_department   
    {
        get;
        set;
    }

    /// <summary>
    /// 软删除
    /// </summary>
    public int tif_isDel      
    {
        get;
        set;
    }

}
