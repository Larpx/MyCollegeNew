using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// 辅导员数据模型类
/// </summary>
public class Counselor
{
    /// <summary>
    /// 辅导员ID
    /// </summary>
    public string cou_ID
    {
        get;
        set;
    }

    /// <summary>
    /// 辅导员登录密码
    /// </summary>
    public string cou_password
    {
        get;
        set;
    }

    /// <summary>
    /// 辅导员姓名
    /// </summary>
    public string cou_name
    {
        get;
        set;
    }

    /// <summary>
    /// 辅导员联系方式
    /// </summary>
    public string cou_contactinfo
    {
        get;
        set;
    }

    /// <summary>
    /// 辅导员权限
    /// </summary>
    public int cou_competence
    {
        get;
        set;
    }

    /// <summary>
    /// 软删除标记
    /// </summary>
    public int cou_isDel
    {
        get;
        set;
    }
}
