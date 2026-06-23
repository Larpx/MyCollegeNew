using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Data;


/// <summary>
/// 学生操作类
/// </summary>
public class StudentOperator : System.Web.UI.Page
{
    public static int Login_Stu(string id, string password)
    {
        //直接检查能否查询到指定数据，查询不到说明不存在，返回0
        if (DBNull.Equals(SQLHelper.ToDbValue(SQLHelper.ExecuteScalar(@"select * from SD_Students where stu_stuID='" + id + "' and stu_password='" + password + "'")), DBNull.Value))
        {
            return 0;
        }
        else
        {  
            return 1;
        }
    }


    /// <summary>
    /// 根据学号查询学生信息
    /// </summary>
    /// <param name="id">学号</param>
    /// <returns>填充查询结果的DataTable</returns>
    public DataTable SeaById_Stu(string id)
    {
        DataTable dt = new DataTable();
        dt = SQLHelper.ExecuteDataTable(@"select * from SD_Students where stu_stuID='" + id + "'"
                    );
        return dt;
    }

    /// <summary>
    /// 根据学号查询该学生的辅导员信息
    /// </summary>
    /// <param name="id">学号</param>
    /// <returns>填充查询结果的DataTable</returns>
    public DataTable SeaCouById_Stu(string id)
    {
        DataTable dt = new DataTable();
        dt = SQLHelper.ExecuteDataTable(@"select * from SD_Counselor where cou_ID=(select c.cla_insID from SD_Students s left join SD_Classes c on s.stu_major=c.cla_majID and s.stu_classes=c.cla_ID and s.stu_grade=c.cla_grade where s.stu_stuID=@id)",
                    new SqlParameter("@id", id));
        return dt;
    }

    /// <summary>
    /// 查询编号对应的学院名
    /// </summary>
    /// <param name="dep"></param>
    /// <returns></returns>
    public string SeaDepById_Stu(int dep)
    {
        DataTable dt = new DataTable();
        dt = SQLHelper.ExecuteDataTable(@"select dep_name from SD_Department where dep_ID=" + dep);
        return dt.Rows[0][0].ToString();

    }

    /// <summary>
    /// 通过专业编号，编号，和年级查询班级名
    /// </summary>
    /// <param name="id"></param>
    /// <param name="maj"></param>
    /// <param name="gra"></param>
    /// <returns></returns>
    public string SeaClassBy_Stu(int id,int maj,int gra)
    {
        DataTable dt = new DataTable();
        dt = SQLHelper.ExecuteDataTable(@"select cla_name from SD_Classes where cla_ID=@i and cla_majID=@m and cla_grade=@g",
            new SqlParameter("@i",id),
            new SqlParameter("@m",maj),
            new SqlParameter("@g",gra));

        return dt.Rows[0][0].ToString();
    }

    public int ChangePassword(string id,string pass)
    {
        //T
       int i = SQLHelper.ExecuteNonQuery(@"update SD_Students set stu_password=@ps where stu_stuID=@id",
            new SqlParameter("@ps", pass),
            new SqlParameter("@id", id));
        return i;
    }

    /// <summary>
    /// 根据班级名称,年级，查询该班级所有学生的名字和学号
    /// </summary>
    /// <param name="classname">班级名称</param>
    /// <param name="grade">年级</param>
    /// <returns>填充查询结果的DataTable</returns>
    public DataTable SeaStuBycls_Stu(string classname,int grade)
    {
        DataTable dt;
        dt = SQLHelper.ExecuteDataTable(@"select * from SD_Students where stu_classes=(select cla_ID from SD_Classes where cla_name=@nn) and stu_grade=@gg",
                    new SqlParameter("@nn", classname),
                    new SqlParameter("@gg", grade));
        return dt;
    }

    /// <summary>
    /// 根据学号查询学生的考勤记录
    /// </summary>
    /// <param name="id">学号</param>
    /// <returns></returns>
    public DataTable SeaAttById_Stu(string id)
    {
        DataTable dt = new DataTable();
        dt = SQLHelper.ExecuteDataTable(@"select att_ID, att_stuId, att_name, att_lesid, att_time, att_att1, att_att2 from SD_Attendinfo where att_stuId=@id",
                    new SqlParameter("@id", id));
        return dt;
    }

    /// <summary>
    /// 根据学号查询学生的请假记录
    /// </summary>
    /// <param name="id">学号</param>
    /// <returns></returns>
    public DataTable SeaLeaById_Stu(string id)
    {
        DataTable dt = new DataTable();
        dt = SQLHelper.ExecuteDataTable(@"select lea_ID, lea_stuID, lea_conID, lea_time1, lea_time2, lea_times, lea_info, lea_other, lea_stat from SD_Leaveinfo where lea_stuID=@id",
                    new SqlParameter("@id", id));
        return dt;
    }

    //public DataTable SeaAttOf

    /// <summary>
    /// 学生请假操作，，info表0
    /// </summary>
    /// <param name="id">个人学号</param>
    /// <param name="t1">起始时间</param>
    /// <param name="t2">终止时间</param>
    /// <param name="t">课程数</param>
    /// <param name="inf">类型</param>
    /// <param name="oth">备注</param>
    /// <returns>1成功</returns>
    public int GetLeave(string id,DateTime t1,DateTime t2,int t,string inf,string oth)
    {
        DataTable dt = SeaCouById_Stu(id);
        int index = SQLHelper.ExecuteNonQuery(@"insert SD_Leaveinfo ( lea_stuID, lea_conID, lea_time1, lea_time2, lea_times, lea_info, lea_other) values(@stuid, @con, @tim1, @tim2, @tms, @inf, @ot)",
            new SqlParameter("@stuid",id),
            new SqlParameter("@con",dt.Rows[0][0].ToString()),
            new SqlParameter("@tim1",t1),
            new SqlParameter("@tim2",t2),
            new SqlParameter("@tms",t),
            new SqlParameter("@inf",inf),
            new SqlParameter("@ot",oth));
        if(index == 1)
        {
            return 1;
        }
        return 0;
    }

    /// <summary>
    /// 将获取的数据表转换为Students数据模型类
    /// </summary>
    /// <param name="row"></param>
    /// <returns></returns>
    public Students ToStudentsModel(DataRow row)
    {
        Students stu = new Students();

        stu.stu_stuID = (string)row["stu_stuID"];
        stu.stu_name = (string)row["stu_name"];
        stu.stu_password = (string)row["stu_password"];
        stu.stu_sex = (string)SQLHelper.FromDbValue(row["stu_sex"]);
        stu.stu_department = (int)row["stu_department"];
        stu.stu_major = (int)row["stu_major"];
        stu.stu_classes = (int)row["stu_classes"];
        stu.stu_static = (int)row["stu_static"];
        stu.stu_grade = (int)row["stu_grade"];
        stu.stu_other = (string)SQLHelper.FromDbValue(row["stu_other"]);
        return stu;
    }

    /// <summary>
    /// 获取SD_Student表中所有数据
    /// </summary>
    /// <returns>Students类型的泛型集合</returns>
    public List<Students> ListAll_Stu()
    {
        List<Students> list = new List<Students>();
        //执行查询，获取datatable
        DataTable dt = SQLHelper.ExecuteDataTable(@"select * from SD_Students");
        foreach (DataRow row in dt.Rows)
        {
            //转换为Students数据结构
            Students tmp = ToStudentsModel(row);
            //添加内容项到list中
            list.Add(tmp);
        }
        return list;
    }
}
