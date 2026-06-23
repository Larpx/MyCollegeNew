using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class Welcome : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!Page.IsPostBack)
        {
            Session["style"] = null;
        }
    }

    /// <summary>
    /// 教师登录
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void Button1_Click1(object sender, EventArgs e)
    {
        //检查sessio是否为空
        if (Session["style"] == null)
        {
            Session["style"] = "Teacher";
            Response.Redirect("LoginPage.aspx");
        }
        else
        {
            //执行跳转
            Response.Redirect("LoginPage.aspx");
        }
    }

    /// <summary>
    /// 辅导员登录
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void Button2_Click(object sender, EventArgs e)
    {
        //检查sessio是否为空
        if (Session["style"] == null)
        {
            Session["style"] = "Cou";
            Response.Redirect("LoginPage.aspx");
        }
        else
        {
            //执行跳转
            Response.Redirect("Welcome.aspx");
        }
    }

    /// <summary>
    /// 学生登录
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void Button3_Click(object sender, EventArgs e)
    {
        //检查sessio是否为空
        if (Session["style"] == null)
        {
            Session["style"] = "Student";
            Response.Redirect("LoginPage.aspx");
        }
        else
        {
            //执行跳转
            Response.Redirect("LoginPage.aspx");
        }
        //检查sessio是否为空
    }
}