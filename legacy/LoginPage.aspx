<%@ Page Language="C#" AutoEventWireup="true" CodeFile="LoginPage.aspx.cs" Inherits="LoginPage1" EnableEventValidation="false" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
    <meta http-equiv="X-UA-Compatible" content="IE=edge"/>
    <meta name="viewport" content="width=device-width, initial-scale=1"/>
    <meta name="description" content=""/>
    <meta name="author" content=""/>
    <!-- Bootstrap core CSS -->
    <link href="css/bootstrap.min.css" rel="stylesheet"/>
    <title>用户登录</title>
</head>
<body>
    <form  id="form1" runat="server">
        <div class="container">
            <form class="form-signin">
                <h2 class="form-signin-heading">用户登录</h2>
                <label class="sr-only" for="email">用户ID</label>
                <asp:TextBox ID="TextBox1" autofocus="autofocus" class="form-control" placeholder="用户ID" required="required" runat="server"></asp:TextBox>
                <label class="sr-only" for="inputPassword">密码</label>
                <asp:TextBox ID="TextBox2" class="form-control" placeholder="密码" required="required" type="password" runat="server"></asp:TextBox>
                <br />
                <asp:Button class="btn btn-lg btn-primary btn-block" ID="Button1" runat="server" Text="Sign in" type="submit" OnClick="Button1_Click" />
            </form>
        </div>
    </form>
</body>
</html>

