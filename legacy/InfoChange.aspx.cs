using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class InfoChange : System.Web.UI.Page
{
    //用户角色标记,0教师，1辅导员
    static int style = 0;
    static DataTable dtmp = new DataTable();
    static string pass = null;

    EducationalOperator edu = new EducationalOperator();

    //教师数据绑定
    public void bind(int index)
    {
        this.Label5.Text = "个人资料修改";
        dtmp = edu.SeaById_Tea(Session["Textname"].ToString());
        this.Label1.Text = dtmp.Rows[0][0].ToString();           //ID
        this.Label2.Text = dtmp.Rows[0][1].ToString();           //姓名
        this.TextBox1.Text = dtmp.Rows[0][2].ToString();         //密码
        this.TextBox2.Text = dtmp.Rows[0][2].ToString();         //确认密码
        this.Label3.Text = edu.SeaDepByName_Tea(int.Parse(dtmp.Rows[0][4].ToString()));          //院系

        this.Label7.Visible = false;
        this.Label8.Visible = false;

        this.TextBox3.Visible = false;
        this.TextBox4.Visible = false;

    }

    //辅导员绑定数据
    public void bind()
    {
        this.Label5.Text = "辅导员个人资料修改";
        dtmp = edu.SeaById_Cou(Session["Textname"].ToString());
        this.Label1.Text = dtmp.Rows[0][0].ToString();           //ID
        this.Label2.Text = dtmp.Rows[0][2].ToString();           //姓名
        this.TextBox1.Text = dtmp.Rows[0][1].ToString();         //密码
        this.TextBox2.Text = dtmp.Rows[0][1].ToString();         //确认密码
        this.TextBox4.Text = dtmp.Rows[0][3].ToString();        //联系方式

        dtmp.Clear();
        dtmp = edu.SeaclsById_Cou(Session["Textname"].ToString());

        //下属班级
        for (int i = 0; i < dtmp.Rows.Count;i++ )
        {
            this.TextBox3.Text+=dtmp.Rows[i][0].ToString()+"-"+dtmp.Rows[i][1].ToString()+"\n";
        }

        this.Label9.Visible = false;
        this.Label3.Visible = false;
    }
    protected void Page_Load(object sender, EventArgs e)
    {
        if(!IsPostBack)
        {
            //第一次加载
            //进行用户角色判断，决定要显示的控件和内容
            if (Session["style"].Equals("Teacher"))
            {
                style = 0;
                bind(1);

            }
            else if (Session["style"].Equals("Cou"))
            {
                style = 1;
                bind();
            }
        }
        else
        {
            //刷新

        }
    }
    
    //提交操作
    protected void Button1_Click(object sender, EventArgs e)
    {
        pass = this.TextBox1.Text;
        int i=edu.ChangePassword(style, Session["Textname"].ToString(),pass);
        if (i == 1)
        {
            Response.Write("<script>alert('修改成功！')</script>");
        }
        else
        {
            Response.Write("<script>alert('修改失败，请稍后再试')</script>");
        }
    }

    //取消，返回
    protected void Button2_Click(object sender, EventArgs e)
    {
        //收到一个锅，是我们33送的~，开森
        if(style == 0)
        {
            Response.Redirect("Teacher.aspx");
        }else if(style == 1)
        {
            Response.Redirect("Counselor.aspx");
        }
    }
}
