using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;


/// <summary>
/// 教务管理操作，包含：
/// 对教师表的操作
/// 对辅导员表的操作
/// 对课程表的操作
/// </summary>
public class EducationalOperator
{
    /*****教师表操作封装****************/
    #region 教师管理
    /// <summary>
    /// 根据姓名查询教师信息
    /// </summary>
    /// <param name="name">姓名关键字</param>
    /// <returns>填充查询结果的DataTable</returns>
    public DataTable SeaByName_Tea(string name)
    {
        DataTable dt = new DataTable();
        dt = SQLHelper.ExecuteDataTable(@"select * from SD_Teacherinfo where tif_name like '%@name%'",
                    new SqlParameter("@name", name));
        return dt;
    }

    /// <summary>
    /// 根据ID查询教师院系，学院信息
    /// </summary>
    /// <param name="dep">院系编号</param>
    /// <returns>学院名字</returns>
    public string SeaDepByName_Tea(int dep)
    {
        DataTable dt = new DataTable();
        dt = SQLHelper.ExecuteDataTable(@"select dep_name from SD_Department where dep_ID=@name",
                    new SqlParameter("@name", dep));

        return dt.Rows[0][0].ToString();
    }

    /// <summary>
    /// 根据编号查询教师姓名
    /// </summary>
    /// <param name="id">教师编号</param>
    /// <returns>填充查询结果的DataTable</returns>
    public DataTable SeanameById_Tea(string id)
    {
        DataTable dt = new DataTable();
        dt = SQLHelper.ExecuteDataTable(@"select tif_name from SD_Teacherinfo where tea_ID='" + id + "'");
        return dt;
    }

    /// <summary>
    /// 根据编号查询教师信息
    /// </summary>
    /// <param name="id">教师编号</param>
    /// <returns>填充查询结果的DataTable</returns>
    public DataTable SeaById_Tea(string id)
    {
        DataTable dt = new DataTable();
        dt = SQLHelper.ExecuteDataTable(@"select * from SD_Teacherinfo where tea_ID ='"+id+"'");
        return dt;
    }

    /// <summary>
    /// 登录判断
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <param name="password">用户密码，可以改为MD5</param>
    /// <returns></returns>
    public static int Login_Tea(string id,string password)
    {
        //防止为null
        int result;
        if (DBNull.Equals(SQLHelper.ToDbValue(SQLHelper.ExecuteScalar(@"select tif_isDel from SD_Teacherinfo where tea_ID='" + id + "' and tif_password='" + password + "'")), DBNull.Value))
        {
            return 0;
        }
        else
        {   //判断当前是否为可用用户信息
            result = (int)SQLHelper.ExecuteScalar(@"select tif_isDel from SD_Teacherinfo where tea_ID='" + id + "' and tif_password='" + password + "'");
            if (result==1)
            {
                return 1;
            }
            else {
                return 0;
            }
        }
    }

    public int ChangePassword(int style,string id,string pass)
    {
        int i = 0;
        if(style == 0)
        {
            //T
            i = SQLHelper.ExecuteNonQuery(@"update SD_Teacherinfo set tif_password=@ps where tea_ID=@tid",
                new SqlParameter("@ps",pass),
                new SqlParameter("@tid",id));
        }
        else if(style == 1)
        {
            //C
            i = SQLHelper.ExecuteNonQuery(@"update SD_Counselor set cou_password=@ps where cou_ID=@cid",
                new SqlParameter("@ps", pass),
                new SqlParameter("@cid", id));
        }
        return i;
    }

    /// <summary>
    /// 将得到的数据行转换为Teacher数据模型
    /// </summary>
    /// <param name="row">得到的一行数据</param>
    /// <returns>Teacher数据模型</returns>
    public Teacher ToTeacherModel(DataRow row)
    {
        Teacher tea = new Teacher();

        tea.tea_ID = (string)row["tea_ID"];
        tea.tif_name = (string)row["tif_name"];
        tea.tif_password = (string)row["tif_password"];
        tea.tif_major = (int)row["tif_major"];
        tea.tif_department = (int)row["tif_department"];
        tea.tif_isDel = (int)row["tif_isDel"];		
        return tea;
    }

    /// <summary>
    /// 获取教师表中所有的信息，并存放到Teacher泛型集合中
    /// </summary>
    /// <returns>Teacher泛型集合</returns>
    public List<Teacher> ListAll_Att()
    {
        List<Teacher> list = new List<Teacher>();
        //执行查询，获取datatable
        DataTable dt = SQLHelper.ExecuteDataTable(@"select * from SD_Teacherinfo");
        foreach (DataRow row in dt.Rows)
        {
            //转换为Teacher数据结构
            Teacher tmp = ToTeacherModel(row);
            //添加内容项到list中
            list.Add(tmp);
        }
        return list;
    }

    #endregion
    /*****导员表操作封装****************/
    #region 辅导员管理

    /// <summary>
    /// 辅导员登录验证
    /// </summary>
    /// <param name="id">辅导员编号</param>
    /// <param name="passwprd">辅导员密码</param>
    /// <returns></returns>
    public static int Login_Cou(string id, string password)
    {
        //防止为null
        int result;
        if (DBNull.Equals(SQLHelper.ToDbValue(SQLHelper.ExecuteScalar(@"select cou_isDel from SD_Counselor where cou_ID='" + id + "' and cou_password='" + password + "'")), DBNull.Value))
        {
            return 0;
        }
        else
        {   //判断当前是否为可用用户信息,检查IsDel
            result = (int)SQLHelper.ExecuteScalar(@"select cou_isDel from SD_Counselor where cou_ID='" + id + "' and cou_password='" + password + "'");
            if (result == 1)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
    }

    /// <summary>
    /// 通过班级名称和年级查询辅导员信息
    /// </summary>
    /// <param name="name">班级名称</param>
    /// <param name="grade">年级</param>
    /// <returns></returns>
    public DataTable SeaBygarname_Cou(string name,int grade)
    {
        DataTable dt = new DataTable();
        dt = SQLHelper.ExecuteDataTable(@"select * from SD_Counselor where cou_ID=(select cla_insID from SD_Classes where cla_name=@nm and cla_grade=@gra ) and cou_isDel=1",
           new SqlParameter("@nm", name),
           new SqlParameter("@gra", grade));
        return dt;
    }

    /// <summary>
    /// 根据姓名查询辅导员信息
    /// </summary>
    /// <param name="name">姓名关键字</param>
    /// <returns>填充查询结果的DataTable</returns>
    public DataTable SeaByName_Cou(string name)
    {
        DataTable dt = new DataTable();
        dt = SQLHelper.ExecuteDataTable(@"select * from SD_Counselor where cou_name like '%@name%'",
                    new SqlParameter("@name", name));
        return dt;
    }

    /// <summary>
    /// 根据编号查询辅导员信息
    /// </summary>
    /// <param name="id">辅导员编号</param>
    /// <returns>填充查询结果的DataTable</returns>
    public DataTable SeaById_Cou(string id)
    {
        DataTable dt = new DataTable();
        dt = SQLHelper.ExecuteDataTable(@"select * from SD_Counselor where cou_ID='"+ id +"'");
        return dt;
    }

    /// <summary>
    /// 根据辅导员编号查询辅导员下属班级信息
    /// </summary>
    /// <param name="id">辅导员编号</param>
    /// <returns>年级，班级名字的DataTable</returns>
    public DataTable SeaclsById_Cou(string id)
    {
        DataTable dt = new DataTable();
        dt = SQLHelper.ExecuteDataTable(@"select cla_grade,cla_name from [SD_Classes] where cla_insID =" + id + "");
        return dt;
    }


    /// <summary>
    /// 将得到的数据行转换为Counselor数据模型
    /// </summary>
    /// <param name="row">得到的一行数据</param>
    /// <returns>Counselor数据模型</returns>
    public Counselor ToCounselorModel(DataRow row)
    {
        Counselor cou = new Counselor();
        cou.cou_ID = (string)row["cou_ID"];
        cou.cou_password = (string)row["cou_password"];
        cou.cou_name = (string)row["cou_name"];
        cou.cou_contactinfo = (string)row["cou_contactinfo"];
        cou.cou_competence = (int)row["cou_competence"];
        cou.cou_isDel = (int)row["cou_isDel"];			
        return cou;
    }

    /// <summary>
    /// 获取辅导员表中所有的信息，并存放到Counselor泛型集合中
    /// </summary>
    /// <returns>Counselor泛型集合</returns>
    public List<Counselor> ListAll_Cou()
    {
        List<Counselor> list = new List<Counselor>();
        //执行查询，获取datatable
        DataTable dt = SQLHelper.ExecuteDataTable(@"select * from SD_Counselor");
        foreach (DataRow row in dt.Rows)
        {
            //转换为Counselor数据结构
            Counselor tmp = ToCounselorModel(row);
            //添加内容项到list中
            list.Add(tmp);
        }
        return list;
    }

    #endregion
    /*****课程操作封装*****************/
    #region 课程表管理
    /// <summary>
    /// 增加课程信息
    /// </summary>
    /// <param name="tt">Lession数据模型</param>
    public void AddLes(Lession les)
    {
        SQLHelper.ExecuteNonQuery(@"insert [SD_Lession] (les_ID, les_name, les_teacherID, les_other) values(@Id, @Name, @tid, @other)",
                new SqlParameter("@Id", les.les_ID),
                new SqlParameter("@Name", les.les_name),
                new SqlParameter("@tid", les.les_teacherID),
                new SqlParameter("@other", SQLHelper.ToDbValue(les.les_other)));
    }

    /// <summary>
    /// 删除指定编号课程信息
    /// ---判断是否存在编号
    /// </summary>
    /// <param name="index">课程的编号</param>
    /// <returns>-1失败，1成功</returns>
    public int DelLesByID(int index)
    {
        if ((index < 0))
        {
            return -1;
        }
        else
        {
            //软删除
            int i = SQLHelper.ExecuteNonQuery(@"delete from [SD_Lession] where les_ID=@id",
                    new SqlParameter("@id", index));
            return i;
        }
    }

    /// <summary>
    /// 根据课程名查询课程信息
    /// </summary>
    /// <param name="name">课程关键字</param>
    /// <returns>填充查询结果的DataTable</returns>
    public DataTable SeaByName_Les(string name)
    {
        DataTable dt = new DataTable();
        dt = SQLHelper.ExecuteDataTable(@"select * from [SD_Lession] where les_name like '%@name%'",
                    new SqlParameter("@name", name));
        return dt;
    }

    /// <summary>
    /// 根据编号查询课程信息
    /// </summary>
    /// <param name="id">课程编号</param>
    /// <returns>填充查询结果的DataTable</returns>
    public DataTable SeaById_Les(string idr)
    {
        DataTable dt = new DataTable();
        dt = SQLHelper.ExecuteDataTable(@"select * from SD_Lession where les_ID like '" + idr + "%'");
        return dt;
    }

    /// <summary>
    /// 根据教师编号，查询该教师的所有任课
    /// </summary>
    /// <param name="id">教师编号</param>
    /// <returns>填充查询结果的DataTable</returns>
    public DataTable SeaLesById_Les(string id)
    {
        DataTable dt = new DataTable();
        dt = SQLHelper.ExecuteDataTable(@"select * from [SD_Lession] where les_teacherID like '@id%'",
                    new SqlParameter("@id", id));
        return dt;
    }

    /// <summary>
    /// 更新对应编号的课程信息资料
    /// </summary>
    /// <param name="index">需要更新的编号</param>
    /// <param name="cou">Lession数据模型</param>
    /// <returns>-1失败，1成功</returns>
    public int UpLesByID(int index, Lession les)
    {
        if (index <= 0)
        {
            return -1;
        }
        else
        {
            int i = SQLHelper.ExecuteNonQuery(@"update [SD_Lession] set les_name=@Name, les_teacherID=@tid, les_other=@other where les_ID=@d",
                    new SqlParameter("@d", index),
                        
                    new SqlParameter("@Name", les.les_name),
                    new SqlParameter("@tid", les.les_teacherID),
                    new SqlParameter("@other",SQLHelper.ToDbValue(les.les_other)));
            return i;
        }
    }

    /// <summary>
    /// 将得到的数据行转换为Lession数据模型
    /// </summary>
    /// <param name="row">得到的一行数据</param>
    /// <returns>Lession数据模型</returns>
    public Lession ToLessionModel(DataRow row)
    {
        Lession les = new Lession();
        les.les_ID = (int)row["les_ID"];
        les.les_name = (string)row["les_name"];
        les.les_teacherID = (string)row["les_teacherID"];
        les.les_other = (string)SQLHelper.FromDbValue(row["les_other"]);   //可为空，须校验传来的东西，防止NullRef
        return les;
    }

    /// <summary>
    /// 获取课程表中所有的信息，并存放到Lession泛型集合中
    /// </summary>
    /// <returns>Lession泛型集合</returns>
    public List<Lession> ListAll_Les()
    {
        List<Lession> list = new List<Lession>();
        //执行查询，获取datatable
        DataTable dt = SQLHelper.ExecuteDataTable(@"select * from SD_Lession");
        foreach (DataRow row in dt.Rows)
        {
            //转换为Lession数据结构
            Lession tmp = ToLessionModel(row);
            //添加内容项到list中
            list.Add(tmp);
        }
        return list;
    }
    #endregion
}
