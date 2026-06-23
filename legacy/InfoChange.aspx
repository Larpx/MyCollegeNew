<%@ Page Language="C#" AutoEventWireup="true" CodeFile="InfoChange.aspx.cs" Inherits="InfoChange" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
<meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
    <link rel="stylesheet" href="css/bootstrap.min.css" />
    <link rel="stylesheet" href="css/flat-ui.css" />
    <title>个人资料修改</title>
</head>
<body>
    <form id="form1" runat="server">
        <div class="container-fluid">
            <div class="row-fluid">
                <div class="span12">
                    <h3 class="text-center">
                        <asp:Label ID="Label5" runat="server"></asp:Label>
                    </h3>
                    <table class="table table-bordered table-hover">
                        <tbody>
                            <tr>
                                <td class="auto-style1">编号：</td>
                                <td>
                                    <asp:Label ID="Label1" runat="server" Text="Label"></asp:Label>
                                </td>
                                <td>姓名：</td>
                                <td>
                                    <asp:Label ID="Label2" runat="server" Text="Label"></asp:Label>
                                </td>
                            </tr>
                            <tr>
                                <td class="auto-style1">密码：</td>
                                <td>
                                    <asp:TextBox ID="TextBox1" runat="server"></asp:TextBox>
                                    <br />
                                    <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" ControlToValidate="TextBox1" ErrorMessage="请输入密码"></asp:RequiredFieldValidator>
                                    <br />
                                </td>
                                <td>确认密码：</td>
                                <td>
                                    <asp:TextBox ID="TextBox2" runat="server"></asp:TextBox>
                                    <br />
                                    <asp:RequiredFieldValidator ID="RequiredFieldValidator2" runat="server" ControlToValidate="TextBox2" ErrorMessage="请输入密码"></asp:RequiredFieldValidator>
                                    <br />
                                    <asp:CompareValidator ID="CompareValidator1" runat="server" ControlToCompare="TextBox2" ControlToValidate="TextBox1" ErrorMessage="两次密码不相同"></asp:CompareValidator>
                                </td>
                            </tr>
                            <tr>
                                <td class="auto-style1">
                                    <asp:Label ID="Label9" runat="server" Text="所属院系："></asp:Label>
                                </td>
                                <td>
                                    <asp:Label ID="Label3" runat="server" Text="Label"></asp:Label>
                                </td>
                                <td>
                                    <asp:Label ID="Label7" runat="server" Text="下属班级："></asp:Label>
                                </td>
                                <td>
                                    <asp:TextBox ID="TextBox3" runat="server" Height="70%" TextMode="MultiLine" ReadOnly="True"></asp:TextBox>
                                </td>
                            </tr>
                            <tr>
                                <td class="auto-style1">
                                    <asp:Label ID="Label8" runat="server" Text="联系方式："></asp:Label>
                                </td>
                                <td>
                                    <asp:TextBox ID="TextBox4" runat="server" ReadOnly="True"></asp:TextBox>
                                </td>
                                <td>&nbsp;</td>
                                <td>
                                    &nbsp;</td>
                            </tr>
                        </tbody>
                    </table>
                    <asp:Button ID="Button1" runat="server" Text="确认修改" class="btn btn-success" OnClick="Button1_Click"  />
                    <asp:Button ID="Button2" runat="server" Text="返回" class="btn" OnClick="Button2_Click"/>
                </div>
            </div>
        </div>
    </form>
</body>
</html>
