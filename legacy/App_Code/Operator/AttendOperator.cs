using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;


/// <summary>
/// 考勤操作类，包含：
/// 对考勤记录表的操作
/// 对请假记录表的操作
/// </summary>
public class AttendOperator
{
    #region 考勤操作类

    /// <summary>
    /// 通过ID和课程编号查询学生考勤信息
    /// </summary>
    /// <param name="index">学生编号</param>
    /// <returns>Attend数据模型</returns>
    public DataTable SeachById(string index,string lessonid)
    {
        DataTable dt = SQLHelper.ExecuteDataTable(@"select att_ID,att_stuId, att_name, att_lesid, att_time, att_att1, att_att2 from SD_Attendinfo where att_stuId=@Id and att_lesid=@le"
            ,new SqlParameter("@Id", index),
            new SqlParameter("@le", lessonid));
        return dt;
    }

    /// <summary>
    /// 通过考勤ID查询考勤信息
    /// </summary>
    /// <param name="index">考勤编号</param>
    /// <returns>Attend数据模型</returns>
    public DataTable SeachByAttId(string index)
    {
        string sql = @"select * from SD_Attendinfo where att_ID=@Id";
        DataTable dt = SQLHelper.ExecuteDataTable(sql, new SqlParameter("@Id", index));
        return dt;
    }


    /// <summary>
    /// 更新对应编号的考勤记录
    /// </summary>
    /// <param name="index">需要更新的编号</param>
    /// <param name="at">Attend数据模型</param>
    /// <returns>-1失败，1成功</returns>
    public int UpAttByID(int index, int t1,int t2)
    {
        if (index < 0)
        {
            return -1;
        }
        else
        {
            int i = SQLHelper.ExecuteNonQuery(@"update SD_Attendinfo set att_att1=@at1, att_att2=@at2 where att_ID=@d",
                    new SqlParameter("@d", index),
                    new SqlParameter("@at1", t1),
                    new SqlParameter("@at2", t2));
            return i;
        }
    }




    /// <summary>
    /// 获取Attend表中所有数据，并且存放到泛型集合中
    /// </summary>
    /// <returns>Attend泛型集合</returns>
    //public List<Attend> ListAll_Att()
    //{
    //    List<Attend> list = new List<Attend>();
    //    //执行查询，获取datatable
    //    DataTable dt = SQLHelper.ExecuteDataTable(@"select * from SD_Attendinfo");
    //    foreach (DataRow row in dt.Rows)
    //    {
    //        //转换为Attend数据结构
    //        Attend tmp = ToAttendModel(row);
    //        //添加内容项到list中
    //        list.Add(tmp);
    //    }
    //    return list;
    //}

    /// <summary>
    /// 获取当前年级数，13，14等
    /// </summary>
    /// <returns></returns>
    public DataTable GetGrade()
    {
        DataTable dt = new DataTable();
        dt = SQLHelper.ExecuteDataTable(@"select DISTINCT stu_grade from SD_Students");
        return dt;
    }

    #endregion

    #region 请假表操作类

    /// <summary>
    /// 通过辅导员查询尚未处理的学生请假信息
    /// </summary>
    /// <param name="index">辅导员</param>
    /// <returns>返回的DataTable</returns>
    public DataTable SeachByCouId_Lea(string id,int t)
    {
        DataTable dt = SQLHelper.ExecuteDataTable(@"select * from [SD_Leaveinfo] where lea_conID=@d and lea_stat=@tat", 
            new SqlParameter("@d", id),
            new SqlParameter("@tat", t)
        );
        return dt;
    }

    /// <summary>
    /// 通过辅导员查询所管理的所有学生请假信息
    /// </summary>
    /// <param name="index">辅导员</param>
    /// <returns>返回的DataTable</returns>
    public DataTable SeachByCouId_Lea(string id)
    {
        string sql = @"select * from [SD_Leaveinfo] where lea_conID=@d";
        DataTable dt = SQLHelper.ExecuteDataTable(sql, new SqlParameter("@d", id));
        return dt;
    }

    /// <summary>
    /// 通过请假单编号查询学生请假信息
    /// </summary>
    /// <param name="index">编号</param>
    /// <returns>返回的DataTable</returns>
    public DataTable SeachById_Lea(int index)
    {
        string sql = @"select * from [SD_Leaveinfo] where lea_ID=@Id";
        DataTable dt = SQLHelper.ExecuteDataTable(sql, new SqlParameter("@Id", index));
        return dt;
    }

    /// <summary>
    /// 通过学号查询学生请假信息
    /// </summary>
    /// <param name="index">学生学号</param>
    /// <returns>返回的DataTable</returns>
    public DataTable SeachByStuId_Lea(string index)
    {
        string sql = @"select * from SD_Leaveinfo where lea_stuID=@d";
        DataTable dt = SQLHelper.ExecuteDataTable(sql, new SqlParameter("@d", index));
        return dt;
    }


    /// <summary>
    /// 更新对应编号的考勤记录
    /// </summary>
    /// <param name="index">需要更新的编号</param>
    /// <param name="lea">Leaveinfo数据模型</param>
    /// <returns>-1失败，1成功</returns>
    public int UpLeaByID(int index, Leaveinfo lea)
    {
        if (index < 0)
        {
            return -1;
        }
        else
        {   
            int i = SQLHelper.ExecuteNonQuery(@"update SD_Leaveinfo set lea_stuID=@Id, lea_conID=@cId, lea_time1=@t1, lea_time2=@t2, lea_times=@ts, lea_info=@info,lea_other=@other where lea_ID=@d",
                    new SqlParameter("@d", index),
                    new SqlParameter("@Id", lea.lea_stuID),
                    new SqlParameter("@cId", lea.lea_conID),
                    new SqlParameter("@t1", lea.lea_time1),
                    new SqlParameter("@t2", lea.lea_time2),
                    new SqlParameter("@ts", lea.lea_times),
                    new SqlParameter("@info", lea.lea_info),
                    new SqlParameter("@other", lea.lea_other));
            return i;
        }
    }

    /// <summary>
    /// 将得到的数据行转换为Leaveinfo数据模型
    /// </summary>
    /// <param name="row">得到的一行数据</param>
    /// <returns>Leaveinfo数据模型</returns>
    public Leaveinfo ToLeaveinfoModel(DataRow row)
    {
        Leaveinfo lea = new Leaveinfo();
        lea.lea_ID = (int)row["lea_ID"];
        lea.lea_stuID = (string)row["lea_stuID"];
        lea.lea_conID = (string)row["lea_conID"];
        lea.lea_time1 = (DateTime)row["lea_time1"];
        lea.lea_time2 = (DateTime)row["lea_time2"];
        lea.lea_times = (int)row["lea_times"];
        lea.lea_info = (int)row["lea_info"];
        lea.lea_other = (string)row["lea_other"];
        return lea;
    }


    /// <summary>
    /// 获取Leaveinfo表中所有数据，并且存放到泛型集合中
    /// </summary>
    /// <returns>Leaveinfo泛型集合</returns>
    public List<Leaveinfo> ListAll_Lea()
    {
        List<Leaveinfo> list = new List<Leaveinfo>();
        //执行查询，获取datatable
        DataTable dt = SQLHelper.ExecuteDataTable(@"select * from SD_Leaveinfo");
        foreach (DataRow row in dt.Rows)
        {
            //转换为Leaveinfo数据结构
            Leaveinfo tmp = ToLeaveinfoModel(row);
            //添加内容项到list中
            list.Add(tmp);
        }
        return list;
    }

    #endregion

    #region 签到操作
    /// <summary>
    /// 建立临时DataTable，用来存放签到数据
    /// 然后逐条写入数据库中
    /// 思路流程;
    /// 1.将数据整合到DataTable中，DataTable行列顺序按照页面中的表单顺序填写
    /// 2.将数据整合到Attend数据模型中
    /// 3.逐条插入
    /// </summary>

    /// <summary>
    /// 考勤记录增加
    /// 可做签到
    /// </summary>
    /// <param name="at">Attend数据模型</param>
    public int AddAtt(DataRow row )
    {
        //格式化数据
        Attend att = new Attend();
        int i = 0;  //返回成功结果，用于进行统计

        att.att_stuId = (string)row["stuId"];
        att.att_name = (string)row["name"];
        att.att_lesid = (int)row["lesid"];
        att.att_time = (DateTime)row["time"];
        att.att_att1 = (int)row["att1"];
        att.att_att2 = (int)row["att2"];

        //插入数据
       i = SQLHelper.ExecuteNonQuery(@"insert [SD_Attendinfo] ( att_stuId, att_name, att_lesid, att_time, att_att1, att_att2)values(@Id, @Name, @lesid, @t, @At1, @At2)",
                new SqlParameter("@Id", att.att_stuId),
                new SqlParameter("@Name", att.att_name),
                new SqlParameter("@lesid", att.att_lesid),
                new SqlParameter("@t", att.att_time),
                new SqlParameter("@At1", att.att_att1),
                new SqlParameter("@At2", att.att_att2));
        
        return i;
    }

    /// <summary>
    /// 考勤记录增加
    /// 可做签到
    /// </summary>
    /// <param name="at">Attend数据模型</param>
    public int AddAtt(Attend att)
    {
        int i = 0;  //返回成功结果，用于进行统计
        //插入数据
        i = SQLHelper.ExecuteNonQuery(@"insert [SD_Attendinfo] ( att_stuId, att_name, att_lesid, att_time, att_att1, att_att2)values(@Id, @Name, @lesid, @t, @At1, @At2)",
                 new SqlParameter("@Id", att.att_stuId),
                 new SqlParameter("@Name", att.att_name),
                 new SqlParameter("@lesid", att.att_lesid),
                 new SqlParameter("@t", att.att_time),
                 new SqlParameter("@At1", att.att_att1),
                 new SqlParameter("@At2", att.att_att2));
        return i;
    }

    #endregion
}
