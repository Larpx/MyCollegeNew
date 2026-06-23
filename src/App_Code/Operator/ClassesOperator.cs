using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;

/// <summary>
/// classes表操作类
/// </summary>
public class ClassesOperator
{

    /// <summary>
    /// 根据指定的关键字，查询班级名称
    /// </summary>
    /// <param name="name">关键字</param>
    /// <returns>获取的结果，填充到DataTable中</returns>
    public DataTable SeaById_Cls(string id)
    {
        DataTable dt = new DataTable();
        dt = SQLHelper.ExecuteDataTable(@"select cla_ID, cla_majID, cla_name, cla_insID, cla_grade from [SD_Classes] where cla_ID like '%" + id + "%'");
        return dt;
    }



    /// <summary>
    /// 获取Classes数据模型
    /// </summary>
    /// <param name="row">数据集合</param>
    /// <returns>Classes数据模型</returns>
    public Classes ToClassesModel(DataRow row)
    {
        Classes cls = new Classes();
        cls.cla_ID = (int)row["cla_ID"];
        cls.cla_majID = (int)row["cla_majID"];
        cls.cla_name = (string)row["cla_name"];
        cls.cla_insID = (string)row["cla_insID"];
        cls.cla_grade =(int)row["cla_grade"];
        return cls;
    }

    /// <summary>
    /// 获取表中所有数据，存入集合中
    /// </summary>
    /// <returns>classes泛型集合</returns>
    public List<Classes> ListAll_Cls()
    {
        List<Classes> list = new List<Classes>();
        //执行查询，获取datatable
        DataTable dt = SQLHelper.ExecuteDataTable(@"select * from [SD_Classes]");
        foreach (DataRow row in dt.Rows)
        {
            //转换为Major数据结构
            Classes tmp = ToClassesModel(row);
            //添加内容项到list中
            list.Add(tmp);
        }
        return list;
    }
}
