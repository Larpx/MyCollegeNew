using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Data;

public partial class Student : System.Web.UI.Page
{
    
    /// <suymmary>
    /// 数据绑定
    /// </summary>
    private void bind()
    {
        DataTable dt;
        StudentOperator stu= new StudentOperator();
        //显示个人考勤记录
        dt = stu.SeaAttById_Stu(Session["Textname"].ToString());
        this.GridView2.DataSource = dt;
        GridView2.DataBind();
        this.Lable2.Text = string.Format("当前第{0}页/总共{1}页", this.GridView2.PageIndex + 1, this.GridView2.PageCount);

        dt.Clear();
        dt = stu.SeaLeaById_Stu(Session["Textname"].ToString());
        GridView1.DataSource = dt;
        GridView1.DataBind();
        this.Lable1.Text = string.Format("当前第{0}页/总共{1}页", this.GridView1.PageIndex + 1, this.GridView1.PageCount);


        this.Label5.Text = Session["Textname"].ToString();
        dt = stu.SeaCouById_Stu(this.Label5.Text.ToString());
        this.Label6.Text = dt.Rows[0][0].ToString();
        //获取名字
        this.Label11.Text = Session["username"].ToString();
     }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            ///判断是否是第一次加载页面
            if ( Session["IsLogin"] == null || !Session["Style"].Equals("Student"))
            {
                Server.Transfer("Welcome.aspx");
            }
            bind();
        }
        else {
            
        }
    }

    #region 考勤记录
    protected void btn_first0_Click(object sender, EventArgs e)
    {
        //考勤记录首页
        this.GridView2.PageIndex = 0;
        bind();
    }
    protected void btn_up0_Click(object sender, EventArgs e)
    {
        //考勤记录上页
        if(this.GridView2.PageIndex > 0)
        {
            this.GridView2.PageIndex -= 1;
            bind();
        }

    }
    protected void btn_next0_Click(object sender, EventArgs e)
    {
        //考勤记录下页
        if (this.GridView2.PageIndex < this.GridView2.PageCount -1)
        {
            this.GridView2.PageIndex += 1;
            bind();
        }
    }
    protected void btn_end0_Click(object sender, EventArgs e)
    {
        //考勤记录尾页
        this.GridView2.PageIndex = this.GridView2.PageCount - 1;
        bind();
    }
    protected void GridView2_PageIndexChanging(object sender, GridViewPageEventArgs e)
    {
        //本页索引发生改变的时候
        this.GridView2.PageIndex = e.NewPageIndex;
        bind();
    }
    protected void drop_list0_SelectedIndexChanged(object sender, EventArgs e)
    {
        //combo选定索引值改变时
        this.GridView2.PageIndex = this.drop_list0.SelectedIndex;
        bind();
    }
    #endregion

    #region 请假记录
    protected void btn_first_Click(object sender, EventArgs e)
    {
        this.GridView1.PageIndex = 0;
        bind();
    }
    protected void btn_up_Click(object sender, EventArgs e)
    {
        if (this.GridView1.PageIndex > 0)
        {
            this.GridView1.PageIndex -= 1;
            bind();
        }
    }
    protected void btn_next_Click(object sender, EventArgs e)
    {
        if (this.GridView1.PageIndex < this.GridView1.PageCount - 1)
        {
            this.GridView1.PageIndex += 1;
            bind();
        }
    }
    protected void btn_end_Click(object sender, EventArgs e)
    {
        this.GridView1.PageIndex = this.GridView1.PageCount - 1;
        bind();
    }

    protected void drop_list_SelectedIndexChanged(object sender, EventArgs e)
    {
        this.GridView1.PageIndex = this.drop_list.SelectedIndex;
        bind();
    }

    protected void GridView1_PageIndexChanging(object sender, GridViewPageEventArgs e)
    {
        this.GridView1.PageIndex = e.NewPageIndex;
        bind();
    }
    protected void LinkButton1_Click(object sender, EventArgs e)
    {
        TextBox1.Text = "";
    }
    protected void LinkButton2_Click(object sender, EventArgs e)
    {
        StudentOperator su = new StudentOperator();
        
        int index = su.GetLeave(Session["Textname"].ToString(), Calendar1.SelectedDate, Calendar2.SelectedDate,
            0, DropDownList1.SelectedValue, TextBox1.Text);
        if(index==1)
        {
            Response.Write("<script>alert('申请成功！')</script>");
            //执行成功后进行数据更新
            bind();
        }
    }
    #endregion
}