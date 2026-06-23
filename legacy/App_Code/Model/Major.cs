using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


/// <summary>
/// 专业模型类
/// </summary>
public class Major
{
    /// <summary>
    /// 专业编号
    /// </summary>
    public int maj_ID
    {
        get;
        set;
       
    }
    /// <summary>
    /// 专业所在学院编号
    /// </summary>
    public int maj_depID
    {
        get;
        set;
    }
    /// <summary>
    /// 专业名字
    /// </summary>
    public string maj_name
    {
        get;
        set;
    }
}
