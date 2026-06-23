<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Student.aspx.cs" Inherits="Student" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type X-UA-Compatible" content="text/html; charset=utf-8 IE=edge,chrome=1"/>
    <!-- BASICS -->
    <title>欢迎使用学生考勤管理系统</title>
    <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
    <link rel="stylesheet" href="css/isotope.css" type="text/css" media="screen" />
    <link rel="stylesheet" href="js/fancybox/jquery.fancybox.css" type="text/css" media="screen" />
    <link rel="stylesheet" href="css/bootstrap.min.css"/>
    <link rel="stylesheet" href="css/style.css"/>
    <!-- skin -->
    <link rel="stylesheet" href="skin/default.css"/>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <!--回到顶部-->
        <section id="header" class="appear"></section>
        <!--菜单-->
        <div class="navbar navbar-fixed-top" role="navigation" data-0="line-height:100px; height:100px; background-color:rgba(0,0,0,0.3);" data-300="line-height:60px; height:60px; background-color:rgba(0,0,0,1);">
            <div class="container">
                <div class="navbar-header">
                    <button type="button" class="navbar-toggle" data-toggle="collapse" data-target=".navbar-collapse">
                        <span class="fa fa-bars color-white"></span>
                    </button>
                    <h1>
                        <a class="navbar-brand" href="index.html" data-0="line-height:90px;" data-300="line-height:70px;">
                           考勤管理系统
                        </a>
                    </h1>
                </div>
                <div class="navbar-collapse collapse">
                    <ul class="nav navbar-nav" data-0="margin-top:20px;" data-300="margin-top:5px;">
                        <li class="active"><a href="#header">主页</a></li>
                        <li><a href="InfoChange_Stu.aspx">信息修改</a></li>
                        <li><a href="#Leaveinfog">请假申请</a></li>
                        <li><a href="#Attend">考勤统计</a></li>
                        <li><a href="#testimonials">每日励志</a></li>
                        <li><a href="#about">关于本系统</a></li>
                    </ul>
                </div><!--/.navbar-collapse -->
            </div>
        </div>
        <!--正文-->
        <section class="featured">
            <div class="container">
                <div class="row mar-bot40">
                    <div class="col-md-6 col-md-offset-3">

                        <div class="align-center">
                            <i class="fa fa-flask fa-5x mar-bot20"></i>
                            <h2 class="slogan">欢迎使用考勤管理系统</h2>
                            <p>
                                欢迎你，<asp:Label ID="Label11" runat="server" Text="Label"></asp:Label>
                                同学，</p>
                            <p>
                                使用本系统，你可以查询个人考勤记录和向辅导员在线申请请假，</p>
                            <p>
                                祝你使用愉快
                                。</p>
                        </div>
                    </div>
                </div>
            </div>
        </section>

        <!-- 校训栅格 -->
        <section id="section-services" class="section pad-bot30 bg-white">
            <div class="container">
                <div class="row mar-bot40">
                    <div class="col-lg-12">
                        <div class="align-left">
                            <p>中德校训</p>
                        </div>
                    </div>
                    
                    <div class="col-lg-4">
                        <div class="align-center">
                            <i class="fa fa-code fa-5x mar-bot20"></i>
                            <h4 class="text-bold"><span style="color: rgb(0, 0, 0); font-family: 华文仿宋; font-size: 19px; font-style: normal; font-variant: normal; font-weight: normal; letter-spacing: normal; line-height: 33px; orphans: auto; text-align: start; text-indent: 0px; text-transform: none; white-space: normal; widows: 1; word-spacing: 0px; -webkit-text-stroke-width: 0px; display: inline !important; float: none;">崇实</span></h4>
                            <p>
                                &nbsp;<span style="color: rgb(0, 0, 0); font-family: 华文仿宋; font-size: 19px; font-style: normal; font-variant: normal; font-weight: normal; letter-spacing: normal; line-height: 33px; orphans: auto; text-align: start; text-indent: 0px; text-transform: none; white-space: normal; widows: 1; word-spacing: 0px; -webkit-text-stroke-width: 0px; display: inline !important; float: none;">崇尚实际，崇尚朴实”，语出东汉哲学家王充《论衡•定贤》。既强调做人要实、做事要实，脚踏实地，一切从实际出发，又强调“强实技、兴实业”，以实实在在的技术技能积累，实现实业兴国的理想抱负。</span></p>
                        </div>
                    </div>

                    <div class="col-lg-4">
                        <div class="align-center">
                            <i class="fa fa-terminal fa-5x mar-bot20"></i>
                            <h4 class="text-bold"><span style="color: rgb(0, 0, 0); font-family: 华文仿宋; font-size: 19px; font-style: normal; font-variant: normal; font-weight: normal; letter-spacing: normal; line-height: 33px; orphans: auto; text-align: justify; text-indent: 0px; text-transform: none; white-space: normal; widows: 1; word-spacing: 0px; -webkit-text-stroke-width: 0px; display: inline !important; float: none;">求精</span></h4>
                            <p>
                                <span style="color: rgb(0, 0, 0); font-family: 华文仿宋; font-size: 19px; font-style: normal; font-variant: normal; font-weight: normal; letter-spacing: normal; line-height: 33px; orphans: auto; text-align: justify; text-indent: 0px; text-transform: none; white-space: normal; widows: 1; word-spacing: 0px; -webkit-text-stroke-width: 0px; display: inline !important; float: none;">“追求完美”，语出《论语•学而》及宋代朱熹的注解。既强调做每件事情都要精益求精，又倡导善思善行、寓创于精。</span></p>
                        </div>
                    </div>

                    <div class="col-lg-4">
                        <div class="align-center">
                            <i class="fa fa-bolt fa-5x mar-bot20"></i>
                            <h4 class="text-bold"><span style="color: rgb(0, 0, 0); font-family: 华文仿宋; font-size: 19px; font-style: normal; font-variant: normal; font-weight: normal; letter-spacing: normal; line-height: 33px; orphans: auto; text-align: justify; text-indent: 28px; text-transform: none; white-space: normal; widows: 1; word-spacing: 0px; -webkit-text-stroke-width: 0px; display: inline !important; float: none;">致良知</span></h4>
                            <p>
                                <span style="color: rgb(0, 0, 0); font-family: 华文仿宋; font-size: 19px; font-style: normal; font-variant: normal; font-weight: normal; letter-spacing: normal; line-height: 33px; orphans: auto; text-align: justify; text-indent: 28px; text-transform: none; white-space: normal; widows: 1; word-spacing: 0px; -webkit-text-stroke-width: 0px; display: inline !important; float: none;">明朝思想家王阳明心学思想的结晶</span><span lang="EN-US" style="font-size: 19px; color: rgb(0, 0, 0); font-family: 华文仿宋; font-style: normal; font-variant: normal; font-weight: normal; letter-spacing: normal; line-height: 33px; orphans: auto; text-align: justify; text-indent: 28px; text-transform: none; white-space: normal; widows: 1; word-spacing: 0px; -webkit-text-stroke-width: 0px;">,</span><span style="color: rgb(0, 0, 0); font-family: 华文仿宋; font-size: 19px; font-style: normal; font-variant: normal; font-weight: normal; letter-spacing: normal; line-height: 33px; orphans: auto; text-align: justify; text-indent: 28px; text-transform: none; white-space: normal; widows: 1; word-spacing: 0px; -webkit-text-stroke-width: 0px; display: inline !important; float: none;">主要包含“心即理、知行合一、致良知”三个理论，而“致良知”全部包括了这三层意思。“良知”语出《孟子•尽心上》：“人之所不学而能者，其良能也，所不虑而知者，其良知也”。“良知”既是道德层面的善恶之心，也是认识层面的是非之心。“致良知”就是要求在实践中磨练明辨善恶是非的能力，做到知行合一、德能兼备。</span>
                            </p>
                        </div>
                    </div>
                </div>
            </div>
        </section>

        <!--请假数据数据区-->
        <section id="Leaveinfog" class="section pad-bot30 bg-white">
            <div class="align-center">
                <h4 class="text-left text-center text-bold">个人请假记录</h4>
            </div>
            <div class="container">
                <div class="row mar-bot40">
                    <div class="col-lg-12">
                        <div class="align-center">
                            <asp:GridView ID="GridView1" runat="server" Width="110%" AutoGenerateColumns="False" OnPageIndexChanging="GridView1_PageIndexChanging" BackColor="White" BorderColor="#999999" BorderStyle="Solid" BorderWidth="1px" CellPadding="3" ForeColor="Black" GridLines="Vertical">
                                <AlternatingRowStyle BackColor="#CCCCCC" />
                                <Columns>
                                    <asp:BoundField DataField="lea_ID" HeaderText="请假记录编号" />
                                    <asp:BoundField DataField="lea_stuID" HeaderText="学号" />
                                    <asp:BoundField DataField="lea_conID" HeaderText="辅导员编号" />
                                    <asp:BoundField DataField="lea_time1" HeaderText="请假起始时间" />
                                    <asp:BoundField DataField="lea_time2" HeaderText="请假终止时间" />
                                    <asp:BoundField DataField="lea_times" HeaderText="课程数" />
                                    <asp:BoundField DataField="lea_info" HeaderText="请假类别" />
                                    <asp:BoundField DataField="lea_other" HeaderText="备注" />
                                    <asp:BoundField DataField="lea_stat" HeaderText="状态" />
                                </Columns>
                                <FooterStyle BackColor="#CCCCCC" />
                                <HeaderStyle Width="8%" BackColor="Black" Font-Bold="True" ForeColor="White" />
                                <PagerStyle BackColor="#999999" ForeColor="Black" HorizontalAlign="Center" />
                                <SelectedRowStyle BackColor="#000099" Font-Bold="True" ForeColor="White" />
                                <SortedAscendingCellStyle BackColor="#F1F1F1" />
                                <SortedAscendingHeaderStyle BackColor="#808080" />
                                <SortedDescendingCellStyle BackColor="#CAC9C9" />
                                <SortedDescendingHeaderStyle BackColor="#383838" />
                            </asp:GridView>
                            <br />
                            <asp:LinkButton ID="btn_first" runat="server" OnClick="btn_first_Click">首页</asp:LinkButton>
                            <asp:LinkButton ID="btn_up" runat="server" OnClick="btn_up_Click">上一页</asp:LinkButton>
                            <asp:Label ID="Lable1" runat="server"></asp:Label>
                            <asp:LinkButton ID="btn_next" runat="server" OnClick="btn_next_Click">下一页</asp:LinkButton>
                            <asp:LinkButton ID="btn_end" runat="server" OnClick="btn_end_Click">尾页</asp:LinkButton>
                            跳转到第<asp:DropDownList ID="drop_list" runat="server" AutoPostBack="True" OnSelectedIndexChanged="drop_list_SelectedIndexChanged">
                            </asp:DropDownList>页
                        </div>
                        <br />
                        <br />
                        <div class="align-center">
                            <h5 class="text-left text-center">申请请假</h5>
                        </div>
                        <div class="container">
                            <div class="align-center">
                                <div class="row mar-bot40">
                                    <table class="table table-bordered table-hover table-responsive">
                                        <tbody>
                                            <tr class="alert">
                                                <td>
                                                    <asp:Label ID="Label4" runat="server" Text="学号"></asp:Label>
                                                </td>
                                                <td>
                                                    <asp:Label ID="Label5" runat="server"></asp:Label>
                                                </td>
                                                <td>
                                                    <asp:Label ID="Label2" runat="server" Text="辅导员编号"></asp:Label>
                                                </td>
                                                <td>
                                                    <asp:Label ID="Label6" runat="server"></asp:Label>
                                                </td>
                                            </tr>
                                            <tr class="alert">
                                                <td>
                                                    <asp:Label ID="Label7" runat="server" Text="请假起始时间："></asp:Label>
                                                </td>
                                                <td>
                                                    <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePageMethods="True">
                                                    </asp:ScriptManager>
                                                    <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                                                        <ContentTemplate>
                                                            <asp:Calendar ID="Calendar1" runat="server" Height="200px" Width="280px"></asp:Calendar>
                                                        </ContentTemplate>
                                                    </asp:UpdatePanel>
                                                </td>
                                                <td>
                                                    <asp:Label ID="Label8" runat="server" Text="请假终止时间："></asp:Label>
                                                </td>
                                                <td>
                                                    <asp:UpdatePanel ID="UpdatePanel2" runat="server">
                                                        <ContentTemplate>
                                                            <asp:Calendar ID="Calendar2" runat="server" Height="220px" Width="280px"></asp:Calendar>
                                                        </ContentTemplate>
                                                    </asp:UpdatePanel>
                                                </td>
                                            </tr>
                                            <tr class="alert">
                                                <td>
                                                    <asp:Label ID="Label9" runat="server" Text="请假类型"></asp:Label>
                                                </td>
                                                <td>
                                                    <asp:DropDownList ID="DropDownList1" runat="server">
                                                        <asp:ListItem Value="1">病假</asp:ListItem>
                                                        <asp:ListItem Value="2">事假</asp:ListItem>
                                                        <asp:ListItem Value="3">其他</asp:ListItem>
                                                    </asp:DropDownList>
                                                </td>
                                                <td>
                                                    <asp:Label ID="Label10" runat="server" Text="请假原因"></asp:Label>
                                                </td>
                                                <td>
                                                    <asp:TextBox ID="TextBox1" runat="server"></asp:TextBox>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td colspan="4">
                                                    <asp:LinkButton ID="LinkButton1" runat="server" OnClick="LinkButton1_Click">重填</asp:LinkButton>
                                                    &nbsp;&nbsp;
                                                        <asp:LinkButton ID="LinkButton2" runat="server" OnClick="LinkButton2_Click">提交</asp:LinkButton>
                                                </td>
                                            </tr>
                                        </tbody>
                                    </table>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </section>
        <!--勤记录数据区-->
        <section id="Attend" class="section pad-bot30 bg-white">
            <div class="align-center">
                <h4 class="text-left text-center text-bold">个人考勤记录</h4>
            </div>
            <div class="container">
                <div class="row mar-bot40">
                    <div class="col-lg-12">
                       <div class="align-center">
                            <asp:GridView ID="GridView2" runat="server" Width="1228px" AutoGenerateColumns="False" OnPageIndexChanging="GridView2_PageIndexChanging" CellPadding="4" ForeColor="#333333" GridLines="None" >
                                <AlternatingRowStyle BackColor="White" />
                                <Columns>
                                    <asp:BoundField DataField="att_ID" HeaderText="考勤记录编号" />
                                    <asp:BoundField DataField="att_stuId" HeaderText="学号" />
                                    <asp:BoundField DataField="att_name" HeaderText="姓名" />
                                    <asp:BoundField DataField="att_lesid" HeaderText="课程编号" />
                                    <asp:BoundField DataField="att_time" HeaderText="上课时间" />
                                    <asp:BoundField DataField="att_att1" HeaderText="第一次签到记录" />
                                    <asp:BoundField DataField="att_att2" HeaderText="第二次签到记录" />
                                </Columns>
                                <EditRowStyle BackColor="#2461BF" />
                                <FooterStyle BackColor="#507CD1" Font-Bold="True" ForeColor="White" />
                                <HeaderStyle BackColor="#507CD1" Font-Bold="True" ForeColor="White" Width="8%" />
                                <PagerStyle BackColor="#2461BF" ForeColor="White" HorizontalAlign="Center" />
                                <RowStyle BackColor="#EFF3FB" />
                                <SelectedRowStyle BackColor="#D1DDF1" Font-Bold="True" ForeColor="#333333" />
                                <SortedAscendingCellStyle BackColor="#F5F7FB" />
                                <SortedAscendingHeaderStyle BackColor="#6D95E1" />
                                <SortedDescendingCellStyle BackColor="#E9EBEF" />
                                <SortedDescendingHeaderStyle BackColor="#4870BE" />
                                </asp:GridView>
                            <asp:LinkButton ID="btn_first0" runat="server" OnClick="btn_first0_Click">首页</asp:LinkButton> 
                            <asp:LinkButton ID="btn_up0" runat="server" OnClick="btn_up0_Click">上一页</asp:LinkButton> 
                            <asp:Label ID="Lable2" runat="server"></asp:Label> 
                            <asp:LinkButton ID="btn_next0" runat="server" OnClick="btn_next0_Click">下一页</asp:LinkButton> 
                            <asp:LinkButton ID="btn_end0" runat="server" OnClick="btn_end0_Click">尾页</asp:LinkButton> 
                            跳转到第<asp:DropDownList ID="drop_list0" runat="server" AutoPostBack="True" OnSelectedIndexChanged="drop_list0_SelectedIndexChanged"> 
                            </asp:DropDownList>页
                                <br />
                           <asp:Label ID="Label1" runat="server" Text="Label">
                           </asp:Label>
                       </div>
                    </div>
                    <br />
                    
                </div>
            </div>
        </section>

        <!-- 动态图片 section:testimonial -->
        <section id="testimonials" class="section" data-stellar-background-ratio="0.5">
            <div class="container">
                <div class="row">
                    <div class="col-lg-12">
                        <div class="align-center">
                            <div class="testimonial pad-top40 pad-bot40 clearfix">
                                <h5>
                                    人最宝贵的东西是生命.生命对人来说只有一次.因此,人的一生应当这样度过:当一个人回首往事时,不因虚度年华而悔恨,也不因碌碌无为而羞愧
                                </h5>
                                <h5>
                                    Life is the most precious thing. Life to people only once. Therefore, people's life should pass like this: when a person look back on the past, not wasted the mood for love and remorse, not because of mediocrity and shame
                                </h5>
                                <br />
                                <span class="author">&mdash; Nicola Alexeyevich Ostrovsky</span>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </section>

        <!-- 动态图片 section:stats -->
        <section id="parallax1" class="section pad-top40 pad-bot40" data-stellar-background-ratio="0.5">
            <div class="container">
                <div class="align-center pad-top40 pad-bot40">
                    <blockquote class="bigquote color-white">一寸光阴一寸金<br />
                        寸金难买寸光阴</blockquote>
                    <p class="color-white">-惜时如金</p>
                </div>
            </div>
        </section>
    
        <!--页脚-->
        <section id="about" class="section footer">
            <div class="container">
                <div class="row animated opacity mar-bot20" data-andown="fadeIn" data-animation="animation">
                    <div class="col-sm-12 align-center">
                        <ul class="social-network social-circle">
                            <li><a href="#" class="icoRss" title="Rss"><i class="fa fa-rss"></i></a></li>
                            <li><a href="#" class="icoVimeo" title="Vimeo"><i class="fa fa-vimeo-square"></i></a></li>
                        </ul>
                    </div>
                </div>
                <div class="row align-center copyright">
                    <div class="col-sm-12"><p>Copyright &copy; 2015 Amoeba - by <a href="http://bootstraptaste.com">Bootstraptaste</a></p></div>
                </div>
            </div>
        </section>

    </div>
    </form>

    <!--脚本-->
    <a href="#header" class="scrollup"><i class="fa fa-chevron-up"></i></a>
	<!--js脚本-->
	<script src="js/modernizr-2.6.2-respond-1.1.0.min.js"></script>
	<script src="js/jquery.js"></script>
	<script src="js/jquery.easing.1.3.js"></script>
    <script src="js/bootstrap.min.js"></script>
	<script src="js/jquery.isotope.min.js"></script>
	<script src="js/jquery.nicescroll.min.js"></script>
	<script src="js/fancybox/jquery.fancybox.pack.js"></script>
	<script src="js/skrollr.min.js"></script>		
	<script src="js/jquery.scrollTo-1.4.3.1-min.js"></script>
	<script src="js/jquery.localscroll-1.2.7-min.js"></script>
	<script src="js/stellar.js"></script>
	<script src="js/jquery.appear.js"></script>
	<script src="js/validate.js"></script>
    <script src="js/main.js"></script>
</body>
</html>
