using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class InfoChange_Stu : System.Web.UI.Page
{
    StudentOperator sto = new StudentOperator();
    static DataTable dt = new DataTable();
    
    protected void Page_Load(object sender, EventArgs e)
    {
        dt = sto.SeaById_Stu(Session["Textname"].ToString());
        
        this.Label1.Text = dt.Rows[0][0].ToString();    //id
        this.Label2.Text = dt.Rows[0][1].ToString();    //name
        this.TextBox1.Text = dt.Rows[0][2].ToString();  //pass
        this.TextBox2.Text = dt.Rows[0][2].ToString();  //pass
        this.Label3.Text =sto.SeaDepById_Stu(int.Parse( dt.Rows[0][4].ToString())); //dep
        this.Label10.Text = dt.Rows[0][8].ToString()+"-"+sto.SeaClassBy_Stu(
            int.Parse(dt.Rows[0][6].ToString()),
            int.Parse(dt.Rows[0][5].ToString()),
            int.Parse(dt.Rows[0][8].ToString()));       //class
    }

    //提交
    protected void Button1_Click(object sender, EventArgs e)
    {
        string pass = this.TextBox1.Text;
        int i = sto.ChangePassword(Session["Textname"].ToString(), pass);
        if(i==1)
        {
            Response.Write("<script>alert('修改成功！')</script>");

        }else{
            Response.Write("<script>alert('修改失败，请稍后再试')</script>");
        }
    }
    //返回
    protected void Button2_Click(object sender, EventArgs e)
    {
        Response.Redirect("Student.aspx");

    }
}