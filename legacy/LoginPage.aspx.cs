using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;

public partial class LoginPage1 : System.Web.UI.Page
{
    int type = 0;//记录对应不同Session的值
    EducationalOperator edu = new EducationalOperator();
    StudentOperator stu = new StudentOperator();
    /// <summary>
    /// 防止用户直接跳转到登录，引发异常
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void Page_Load(object sender, EventArgs e)
    {
        if(!Page.IsPostBack)
        {
            ///第一次加载页面
            if(Session["style"]==null)
            {
                Server.Transfer("Welcome.aspx");
            }
        }
    }


    /// <summary>
    /// 登录按钮
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void Button1_Click(object sender, EventArgs e)
    {
        
        if(String.IsNullOrEmpty(TextBox1.Text)||String.IsNullOrEmpty(TextBox2.Text))
        {
            Response.Write("<script>alert('请填写用户名和密码')</script>");
            return;
        }
        else
        {
            type = changeSession();
            switch(type)
            {
                case 1:
                    //教师登陆
                    if(EducationalOperator.Login_Tea(TextBox1.Text,TextBox2.Text)==1)
                    {
                        EducationalOperator edu = new EducationalOperator();
                        DataTable dt = edu.SeaById_Tea(TextBox1.Text);
                        Response.Write("<script>alert('登录成功')</script>");
                        //设定Session，用来标记当前用户是否登录
                        Session["IsLogin"] = 1;
                        Session["username"] = dt.Rows[0][1].ToString();
                        Session["Textname"] = TextBox1.Text;
                        //执行跳转
                        Response.Redirect("Teacher.aspx");
                    }
                    else
                    {
                        TextBox1.Text = "";
                        TextBox2.Text = "";
                        Response.Write("<script>alert('登录失败，请检查您的用户ID和密码是否正确')</script>");
                        return;
                    }
                    break;
                case 2:
                    //辅导员登录
                    if (EducationalOperator.Login_Cou(TextBox1.Text, TextBox2.Text) == 1)
                    {
                        EducationalOperator edu = new EducationalOperator();
                        DataTable dt =  edu.SeaById_Cou(TextBox1.Text);
                        Response.Write("<script>alert('登录成功')</script>");
                        //设定Session，用来标记当前用户是否登录
                        Session["IsLogin"] = 1;
                        Session["username"] = dt.Rows[0][1].ToString();
                        Session["Textname"] = TextBox1.Text;
                        //执行跳转
                        Response.Redirect("Counselor.aspx");
                    }
                    else
                    {
                        TextBox1.Text = "";
                        TextBox2.Text = "";
                        Response.Write("<script>alert('登录失败，请检查您的用户ID和密码是否正确')</script>");
                        return;
                    }
                    break;
                case 3:
                    //学生登录
                    if (StudentOperator.Login_Stu(TextBox1.Text, TextBox2.Text) ==1 )
                    {
                        StudentOperator so = new StudentOperator();
                        DataTable dt = so.SeaById_Stu(TextBox1.Text);
                        Response.Write("<script>alert('登录成功')</script>");
                        //设定Session，用来标记当前用户是否登录
                        Session["IsLogin"] = 1;
                        Session["username"] = dt.Rows[0][1].ToString();
                        Session["Textname"] = TextBox1.Text;
                        //执行跳转
                        Response.Redirect("Student.aspx");
                    }
                    else
                    {
                        TextBox1.Text = "";
                        TextBox2.Text = "";
                        Response.Write("<script>alert('登录失败，请检查您的用户ID和密码是否正确')</script>");
                        return;
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// 根据不同的Session值来获取不同的权限值
    /// </summary>
    /// <returns></returns>
    int changeSession()
    {
        int index = 0;
        //判断session的值是什么，来确定执行那个表的登录验证操作
        //1教师权限，2辅导员权限，3学生权限
        if (Session["Style"].Equals("Teacher"))
        {
            index = 1;
        }
        else if (Session["Style"].Equals("Cou"))
        {
            index = 2;
        }
        else if (Session["Style"].Equals("Student"))
        {
            index = 3;
        }
        return index;
    }

    //判断当前状态，确定是否将用户信息写入cookies
    protected void CheckBox1_CheckedChanged(object sender, EventArgs e)
    {
        //if(CheckBox1.Checked)
        //{
        //    //选中

        //}
        //else
        //{
        //    //未选中

        //}
    }
}