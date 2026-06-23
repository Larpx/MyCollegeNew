using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Data.Sql;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class Teacher : System.Web.UI.Page
{
    //写往数据库
    //List<Attend> att_list = new List<Attend>();
    //List<Students> stu_list = new List<Students>();
    //用于前台页面签到表GridView数据汇总展示
    static DataTable dtview = new DataTable();
    //临时存放，数据转换时使用
    DataTable dtmp = new DataTable();
    //指定时间
    static string dtime;

    StudentOperator stu = new StudentOperator();
    AttendOperator ato = new AttendOperator();
    EducationalOperator edu = new EducationalOperator();
   
    /// <summary>
    /// 刷新绑定数据
    /// </summary>
    private  void bind()
    {
        //对页面内标签的初始化
        this.Label12.Text = dtime;
        this.Lable2.Text = string.Format("当前第{0}页/总共{1}页", this.GridView2.PageIndex + 1, this.GridView2.PageCount);
        this.Label13.Text = Session["Textname"].ToString();

        dtmp.Clear();
        dtmp = edu.SeanameById_Tea(Session["Textname"].ToString());
        this.Label14.Text ="--"+dtmp.Rows[0][0].ToString();
        this.Label11.Text = dtmp.Rows[0][0].ToString();

        ///考勤记录____课程选择
        dtmp =SQLHelper.ExecuteDataTable(@"select les_ID, les_name from SD_Lession where les_teacherID=@w",
            new SqlParameter("@w",Session["Textname"].ToString()));
        Drop_selectLession.Items.Clear();
        for (int i = 0; i < dtmp.Rows.Count;i++ )
        {
            ListItem lt = new ListItem();
            lt.Text = dtmp.Rows[i][1].ToString();
            lt.Value =dtmp.Rows[i][0].ToString();
            Drop_selectLession.Items.Add(lt);
        }
        //设置默认选中项,取消，防止每次刷新页面的时候默认选中该项，导致查询记录无法改变
        //Drop_selectLession.SelectedIndex = 0;
        
         
        ///考勤记录____班级选择
        dtmp.Clear();
        dtmp = SQLHelper.ExecuteDataTable(@"select cla_name from SD_Classes");
        this.Drop_qClass.Items.Clear();
        for (int i = 0; i < dtmp.Rows.Count; i++)
        {
            ListItem lt = new ListItem();
            lt.Text = dtmp.Rows[i][0].ToString();
            lt.Value = dtmp.Rows[i][0].ToString();
            Drop_qClass.Items.Add(lt);
        }
        //this.Drop_qClass.SelectedIndex = 0;
        

        //考勤记录_年级选项
        dtmp.Clear();
        dtmp = ato.GetGrade();
        this.Drop_grade.Items.Clear();
        for (int i = 0; i < dtmp.Rows.Count;i++ )
        {
            ListItem lt = new ListItem();
            lt.Text = dtmp.Rows[i][0].ToString();
            lt.Value = dtmp.Rows[i][0].ToString();
            this.Drop_grade.Items.Add(lt);
        }
        //this.Drop_grade.SelectedIndex = 0;


        ///考勤记录____课程选择
        if(String.IsNullOrEmpty(this.Drop_qClass.Text)||String.IsNullOrEmpty(this.Drop_grade.Text))
        {
            //选项未全选
            Response.Write("<script>alert('请先选择年级和班级信息')</script>");
            return;
        }
        else
        {
            dtmp.Clear();
            dtmp = SQLHelper.ExecuteDataTable(@"select les_ID, les_name from SD_Lession where les_teacherID=@w",
            new SqlParameter("@w", Session["Textname"].ToString()));
            this.Drop_qLession.Items.Clear();
            for (int i = 0; i < dtmp.Rows.Count; i++)
            {
                ListItem lt = new ListItem();
                lt.Text = dtmp.Rows[i][1].ToString();
                lt.Value = dtmp.Rows[i][0].ToString();
                this.Drop_qLession.Items.Add(lt);
            }
            //设置默认选中项
            //this.Drop_qLession.SelectedIndex = 0;
        }
    }

    /// <summary>
    /// 初始化前台的DT
    /// </summary>
    private static void initDT()
    {
        //初始化前台绑定数据的DT
        dtview.Columns.Add("id",typeof(string));
        dtview.Columns.Add("name", typeof(string));
        dtview.Columns.Add("classes",typeof(string));
        dtview.Columns.Add("first",typeof(int));
        dtview.Columns.Add("second",typeof(int));

        //dtview.Columns.Add()
    }

    /// <summary>
    /// 初次加载绑定数据
    /// </summary>
    /// <param name="index"> 使用重载</param>
    private void bind(int index)
    {
        //对页面内标签的初始化
        this.Label12.Text = dtime;
        this.Lable2.Text = string.Format("当前第{0}页/总共{1}页", this.GridView2.PageIndex + 1, this.GridView2.PageCount);
        this.Label13.Text = Session["Textname"].ToString();

        dtmp.Clear();
        dtmp = edu.SeanameById_Tea(Session["Textname"].ToString());
        this.Label14.Text = "--" + dtmp.Rows[0][0].ToString();
        this.Label11.Text = dtmp.Rows[0][0].ToString();

        ///考勤记录____课程选择
        dtmp = SQLHelper.ExecuteDataTable(@"select les_ID, les_name from SD_Lession where les_teacherID=@w",
            new SqlParameter("@w", Session["Textname"].ToString()));
        this.Drop_selectLession.Items.Clear();
        for (int i = 0; i < dtmp.Rows.Count; i++)
        {
            ListItem lt = new ListItem();
            lt.Text = dtmp.Rows[i][1].ToString();
            lt.Value = dtmp.Rows[i][0].ToString();
            Drop_selectLession.Items.Add(lt);
        }
        //设置默认选中项
        this.Drop_selectLession.SelectedIndex = 0;


        ///考勤记录____班级选择
        dtmp.Clear();
        dtmp = SQLHelper.ExecuteDataTable(@"select cla_name from SD_Classes");
        this.Drop_qClass.Items.Clear();
        for (int i = 0; i < dtmp.Rows.Count; i++)
        {
            ListItem lt = new ListItem();
            lt.Text = dtmp.Rows[i][0].ToString();
            lt.Value = dtmp.Rows[i][0].ToString();
            Drop_qClass.Items.Add(lt);
        }
        this.Drop_qClass.SelectedIndex = 0;


        //考勤记录_年级选项
        dtmp.Clear();
        dtmp = ato.GetGrade();
        this.Drop_grade.Items.Clear();
        for (int i = 0; i < dtmp.Rows.Count; i++)
        {
            ListItem lt = new ListItem();
            lt.Text = dtmp.Rows[i][0].ToString();
            lt.Value = dtmp.Rows[i][0].ToString();
            this.Drop_grade.Items.Add(lt);
        }
        this.Drop_grade.SelectedIndex = 0;


        ///考勤记录____课程选择
        dtmp.Clear();
        dtmp = SQLHelper.ExecuteDataTable(@"select les_ID, les_name from SD_Lession where les_teacherID=@w",
        new SqlParameter("@w", Session["Textname"].ToString()));
        this.Drop_qLession.Items.Clear();
        for (int i = 0; i < dtmp.Rows.Count; i++)
        {
            ListItem lt = new ListItem();
            lt.Text = dtmp.Rows[i][1].ToString();
            lt.Value = dtmp.Rows[i][0].ToString();
            this.Drop_qLession.Items.Add(lt);
        }
        //设置默认选中项
        this.Drop_qLession.SelectedIndex = 0;
    }

    /// <summary>
    /// 页面加载
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            ///判断是否是第一次加载页面
            if (Session["IsLogin"] == null || !Session["Style"].Equals("Teacher"))
            {
                Server.Transfer("Welcome.aspx");
            }
            dtime = DateTime.Now.ToString();
            bind(1);
            initDT();
        }
        else
        {
            //刷新操作
        }
    }


    //首页，上一页，下一页，跳页，操作
    protected void Button1_Click(object sender, EventArgs e)
    {
        //空值判断

        if(!String.IsNullOrEmpty(TextBox3.Text))
        {
            dtmp.Clear();
            //不为空,执行查询
            dtmp = ato.SeachById(TextBox3.Text, Drop_selectLession.SelectedValue);
            this.GridView2.DataSource = dtmp;
            this.GridView2.DataBind();

        }
        else
        {
            //为空
            Response.Write("<script>alert('学生学号不能为空')</script>");
            return;
        }
    }
    protected void btn_first0_Click(object sender, EventArgs e)
    {
        this.GridView2.PageIndex = 0;
        bind();
    }
    protected void btn_end0_Click(object sender, EventArgs e)
    {
        this.GridView2.PageIndex = this.GridView2.PageCount - 1;
        bind();
    }
    protected void btn_up0_Click(object sender, EventArgs e)
    {
        if(this.GridView2.PageIndex >0)
        {
            this.GridView2.PageIndex--;
            bind();
        }
    }
    protected void btn_next0_Click(object sender, EventArgs e)
    {
        if(this.GridView2.PageIndex<this.GridView2.PageCount)
        {
            this.GridView2.PageIndex++;
            bind();
        }
    }
    protected void drop_page_SelectedIndexChanged(object sender, EventArgs e)
    {
        this.GridView2.PageIndex = this.drop_page.SelectedIndex;
        bind();
    }



    //提交签到表
    protected void btn_Tiaojiao_Click(object sender, EventArgs e)
    {
        int index = 0;
        for(int i=0;i<dtview.Rows.Count;i++)
        {
            /*
            dtview.Columns.Add("id",typeof(string));
            dtview.Columns.Add("name", typeof(string));
            dtview.Columns.Add("classes",typeof(string));
            dtview.Columns.Add("first",typeof(int));
            dtview.Columns.Add("second",typeof(int));
             */
            Attend at = new Attend();
            at.att_stuId = dtview.Rows[i][0].ToString();
            at.att_name = dtview.Rows[i][1].ToString();
            at.att_lesid =int.Parse(this.Drop_qLession.SelectedValue.ToString());
            at.att_time =DateTime.Parse(dtime);
            at.att_att1 = int.Parse(dtview.Rows[i][3].ToString());
            at.att_att1 = int.Parse(dtview.Rows[i][4].ToString());

            index += ato.AddAtt(at);
        }
        if(index != dtview.Rows.Count)
        {
            Response.Write("<script>alert('false')</script>");
            Response.Redirect(Request.Url.ToString());
        }
        else
        {
            Response.Write("<script>alert('sucess')</script>");
        }
    }

    /// <summary>
    /// 选择完成后填充下面的Gridview
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void btn_Liebiao_Click(object sender, EventArgs e)
    {
        
        //发生事件后，开始查询辅导员姓名
        if (String.IsNullOrEmpty(Drop_qClass.SelectedValue) || String.IsNullOrEmpty(Drop_grade.SelectedValue))
        {
            //选项未全选
            Response.Write("<script>alert('请先选择年级和班级信息')</script>");
            Response.Redirect(Request.Url.ToString());
        }
        else
        {
            dtmp.Clear();
            dtmp = edu.SeaBygarname_Cou(Drop_qClass.SelectedValue, int.Parse(Drop_grade.SelectedValue));
            this.Label15.Text = dtmp.Rows[0][0].ToString();
            this.Label16.Text = "--" + dtmp.Rows[0][2].ToString();
        }

        dtview.Clear();
        dtmp.Clear();
        dtmp = stu.SeaStuBycls_Stu(Drop_qClass.SelectedValue.ToString(), int.Parse(Drop_grade.SelectedValue));
        if(dtmp.Rows.Count == 0)
        {
            Response.Write("<script>alert('请先选择年级和班级信息')</script>");
            Response.Redirect(Request.Url.ToString());
        }
        else
        {
            for (int i = 0; i < dtmp.Rows.Count; i++)
            {
                DataRow row = dtview.NewRow();
                row[0] = dtmp.Rows[i][0].ToString();
                row[1] = dtmp.Rows[i][1].ToString();
                row[2] = Drop_qClass.Text;
                row[3] = 0;
                row[4] = 0;
                dtview.Rows.Add(row);
            }
            this.GridView1.DataSource = dtview;
            this.GridView1.DataBind();
        }
    }

    //第一次签到操作
    protected void LinkButton1_Command(object sender, CommandEventArgs e)
    {
        int first = 0;
        LinkButton lb = (LinkButton)sender;
        DataControlFieldCell dcf = (DataControlFieldCell)lb.Parent;
        GridViewRow gvr = (GridViewRow)dcf.Parent; //此得出的值是表示那行被选中的索引值
        first = gvr.RowIndex;
        LinkButton linb = (LinkButton)this.GridView1.Rows[first].FindControl("LinkButton1");

        if (dtview.Rows[first][4].Equals(1))
        {
            dtview.Rows[first][3] = 1;
            linb.Text = "0";
        }
        else
        {
            dtview.Rows[first][3] = 1;
            linb.Text = "1";
        }
    }


    //第二次签到操作
    protected void LinkButton2_Command(object sender, CommandEventArgs e)
    {
        int second = 0;
        LinkButton lb = (LinkButton)sender;
        DataControlFieldCell dcf = (DataControlFieldCell)lb.Parent;
        GridViewRow gvr = (GridViewRow)dcf.Parent;       //此得出的值是表示那行被选中的索引值
        second = gvr.RowIndex;      //获取控件所在行数
        LinkButton linb = (LinkButton)this.GridView1.Rows[second].FindControl("LinkButton2");

        if( dtview.Rows[second][4].Equals(1))
        {
            dtview.Rows[second][4] = 0;
            linb.Text = "0";
        }
        else
        {
            dtview.Rows[second][4] = 1;
            linb.Text = "1";
        }

    }

}
