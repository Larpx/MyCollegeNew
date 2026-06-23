using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;

/// <summary>
/// 院系操作类
/// </summary>
public class DepartOperator
{
    #region 学院操作

    /// <summary>
    /// 学院增加操作
    /// </summary>
    /// <param name="index">学院编号</param>
    /// <param name="name">学院名称</param>
    /// <returns>1成功，-1失败</returns>
    public  int AddDe(int index,string name)
    {
        if (String.IsNullOrEmpty(name) && (index < 0))
        {
            return -1;
        }
        else {
            int i = SQLHelper.ExecuteNonQuery(@"insert SD_Department (dep_ID,dep_name) values(@id,@name)",
                    new SqlParameter("@id",index),
                    new SqlParameter("@name",name));
            return i;    
        }
    }

    /// <summary>
    /// 按编号删除学院操作
    /// </summary>
    /// <param name="index">编号</param>
    /// <returns>1成功，-1失败</returns>
    public  int DelDeByID(int index)
    {
        if ((index < 0))
        {
            return -1;
        }
        else
        {
            int i = SQLHelper.ExecuteNonQuery(@"delete from SD_Department where dep_ID=@id",
                    new SqlParameter("@id", index));
            return i;
        }
    }

    /// <summary>
    /// 按照编号来修改学院信息
    /// </summary>
    /// <param name="index"></param>
    /// <param name="newindex"></param>
    /// <param name="newname"></param>
    /// <returns></returns>
    public  int UpDeByID(int index,int newindex,string newname)
    {
        if (String.IsNullOrEmpty(newname) && (index < 0) &&(newindex < 0))
        {
            return -1;
        }
        else
        {
            int i = SQLHelper.ExecuteNonQuery(@"update SD_Department set dep_ID=@newid ,dep_name=@newname where dep_ID=@d",
                    new SqlParameter("@newid", newindex),
                    new SqlParameter("@newname", newname),
                    new SqlParameter("@d", index));
            return i;
        }
    }

    /// <summary>
    /// 将获取到的学院信息转换为数据
    /// </summary>
    /// <param name="row">数据表</param>
    /// <returns>返回Department数据模型</returns>
    public Department ToDepModel(DataRow row)
    {
        Department dp = new Department();
        dp.dep_ID = (int)row["dep_ID"];
        dp.dep_name  = (string)row["dep_name"];
        return dp;
    }
        
    /// <summary>
    /// 获取Department表中所有数据，存放到List集合中
    /// </summary>
    /// <returns></returns>
    public  List<Department> ListAll_Dep()
    {
        List<Department> list = new List<Department>();
        //执行查询，获取datatable
        DataTable dt = SQLHelper.ExecuteDataTable(@"select * from [SD_Department]");
        foreach(DataRow row in dt.Rows)
        {
            //转换为Department数据结构
            Department tmp = ToDepModel(row);
            //添加内容项到list中
            list.Add(tmp);
        }
        return list;
    }
#endregion
        
    #region 系别操作

    /// <summary>
    /// 专业增加操作
    /// </summary>
    /// <param name="mj">major数据模型</param>
    public void AddMa(Major mj)
    {

        SQLHelper.ExecuteNonQuery(@"insert [SD_Major] (maj_depID,maj_ID,maj_name) values(@id,@mid,@mname)",
                new SqlParameter("@id", mj.maj_depID),
                new SqlParameter("@mid", mj.maj_ID),
                new SqlParameter("@mname", mj.maj_name));
    }

    /// <summary>
    /// 按编号删除专业操作
    /// </summary>
    /// <param name="index">编号</param>
    /// <returns>1成功，-1失败</returns>
    public int DelMaByID(int index)
    {
        if ((index < 0))
        {
            return -1;
        }
        else
        {
            int i = SQLHelper.ExecuteNonQuery(@"delete from [SD_Major] where maj_ID=@id",
                    new SqlParameter("@id", index));
            return i;
        }
    }

    /// <summary>
    /// 按照编号来修改专业信息
    /// </summary>
    /// <param name="index">需要修改项的ID</param>
    /// <param name="mj">需要修改的信息</param>
    /// <returns>-1失败，1成功</returns>
    public int UpMaByID(int index,Major mj)
    {
        if (index < 0)
        {
            return -1;
        }
        else
        {
            int i = SQLHelper.ExecuteNonQuery(@"update [SD_Major] set maj_ID=@Id,maj_depID=@DId,maj_name=@Name where maj_ID=@d",
                    new SqlParameter("@d",index),
                    new SqlParameter("@DId", mj.maj_depID),
                    new SqlParameter("@Id", mj.maj_ID),
                    new SqlParameter("@Name", mj.maj_name));
            return i;
        }
    }

    /// <summary>
    /// 将获取到的专业信息转换为数据模型
    /// </summary>
    /// <param name="row">数据表</param>
    /// <returns>返回Major数据模型</returns>
    public Major ToMajorModel(DataRow row)
    {
        Major mj = new Major();
        mj.maj_depID = (int)row["maj_depID"];
        mj.maj_ID = (int)row["maj_ID"];
        mj.maj_name = (string)row["maj_name"];
        return mj;
    }

    /// <summary>
    /// 获取Major表中所有数据，存放到List集合中
    /// </summary>
    /// <returns></returns>
    public List<Major> ListAll_Maj()
    {
        List<Major> list = new List<Major>();
        //执行查询，获取datatable
        DataTable dt = SQLHelper.ExecuteDataTable(@"select * from [SD_Major]");
        foreach (DataRow row in dt.Rows)
        {
            //转换为Major数据结构
            Major tmp = ToMajorModel(row);
            //添加内容项到list中
            list.Add(tmp);
        }
        return list;
    }
    #endregion
}
