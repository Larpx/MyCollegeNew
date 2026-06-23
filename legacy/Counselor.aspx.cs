using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Data;


public partial class Counselor : System.Web.UI.Page
{
    /// <summary>
    /// 查询当前是否有未处理的请假请求，如果有，则显示到前台中
    /// </summary>
    /// <param name="type">查询类型编号</param>
    private void selectall(int type)
    {
        AttendOperator at = new AttendOperator();
        DataTable dt;
        switch(type) 
        {
            case 1:
                //未处理信息
                dt = at.SeachByCouId_Lea(Session["Textname"].ToString(), 0);
                GridView1.DataSource = dt;
                GridView1.DataBind();
                break;

            case 2:
                //显示所有信息
                dt = at.SeachByCouId_Lea(Session["Textname"].ToString());
                GridView3.DataSource = dt;
                GridView3.DataBind();
                break;
        }
    }

    /// <suymmary>
    /// 获取未通过申请的请假信息
    /// </summary>
    private void bind()
    {
        //存放临时信息dt
        DataTable dtmp = new DataTable(); ;
        AttendOperator at = new AttendOperator();
        EducationalOperator edu = new EducationalOperator();

        dtmp = at.SeachByCouId_Lea(Session["Textname"].ToString());

        for(int i=0;i<dtmp.Rows.Count;i++)
        {
            //开始遍历，删除已处理的请假信息
            if(dtmp.Rows[i][8].ToString().Equals("1"))
            {
                dtmp.Rows.RemoveAt(i);
            }
        }
        this.Label11.Text = edu.SeaById_Cou(Session["Textname"].ToString()).Rows[0][2].ToString();
        this.GridView1.DataSource = dtmp;
        this.GridView1.DataBind();
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            ///判断是否是第一次加载页面
            if (Session["IsLogin"] == null || !Session["Style"].Equals("Cou"))
            {
                Server.Transfer("Welcome.aspx");
            }
            bind();
        }
        else
        {
            //刷新操作
            bind();
        }
    }
    /*************************考勤记录*********************/
    
    /// <summary>
    /// 查询指定学号学生的考勤信息，并进行修订
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void Button1_Click(object sender, EventArgs e)
    {
        if(!String.IsNullOrEmpty(TextBox3.Text))
        {
            DataTable dt = new DataTable();
            StudentOperator stu = new StudentOperator();
            dt = stu.SeaAttById_Stu(TextBox3.Text);
            if(dt.Rows.Count!=0)
            {
                this.GridView2.DataSource = dt;
                this.GridView2.DataBind();
            }
            else
            {
                Response.Write("<script>alert('未查询到任何数据，请检查学生学号是否正确')</script>");
                return;
            }
        }
        else
        {
            Response.Write("<script>alert('请输入学号')</script>");
            return;
        }
    }

    /// <summary>
    /// 查询考勤信息编号，获取要修订的考勤信息
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void Button2_Click(object sender, EventArgs e)
    {
        if(!String.IsNullOrEmpty(TextBox4.Text))
        {
            DataTable dt = new DataTable();
            AttendOperator at = new AttendOperator();
            dt = at.SeachByAttId(TextBox4.Text);
            TextBox4.Text = dt.Rows[0][0].ToString();
            Label6.Text = dt.Rows[0][1].ToString();
            Label3.Text = dt.Rows[0][2].ToString();
            Label12.Text = dt.Rows[0][3].ToString();
            Label10.Text = dt.Rows[0][4].ToString();
            TextBox1.Text = dt.Rows[0][5].ToString();
            TextBox2.Text = dt.Rows[0][6].ToString();
        }
        else
        {
            Response.Write("<script>alert('请输入编号')</script>");
            return;
        }
    }

    /// <summary>
    /// 提交修改信息
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void LinkButton2_Click(object sender, EventArgs e)
    {
        if (!(String.IsNullOrEmpty(TextBox1.Text)) && !(String.IsNullOrEmpty(TextBox2.Text)) && !(String.IsNullOrEmpty(TextBox4.Text)))
        {
            AttendOperator at = new AttendOperator();
            int index = at.UpAttByID(int.Parse(TextBox4.Text), int.Parse(TextBox1.Text), int.Parse(TextBox2.Text));
            if(index == 1)
            {
                Response.Write("<script>alert('修改成功')</script>");
            }
        }
        else {
            Response.Write("<script>alert('请填写学号和考勤信息')</script>");
            return;
        }
    }

    //按学号查询指定学生考勤信息
    protected void Button3_Click(object sender, EventArgs e)
    {
        if(!String.IsNullOrEmpty(TextBox5.Text))
        {
            AttendOperator at = new AttendOperator();
            DataTable dt;
            dt = at.SeachByStuId_Lea(TextBox5.Text);
            GridView3.DataSource = dt;
            GridView3.DataBind();
        }
        
    }

    /// <summary>
    /// 查询下属所有班级学生的请假信息
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void Button4_Click(object sender, EventArgs e)
    {
        selectall(2);
    }
}
