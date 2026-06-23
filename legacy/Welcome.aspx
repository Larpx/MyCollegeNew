<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Welcome.aspx.cs" Inherits="Welcome" %>
<!DOCTYPE html>
<html>
<head runat="server"> 
<meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
<meta name="viewprot" content="width=device-width,initial-scale=1"/>

    <link href="css/welcome_style.css" rel="stylesheet"/>
    <link href="css/flat-ui.css" rel="stylesheet">
    <title>Welcome To...</title>
</head>
<body>
    <!--start-pricing-tablel-->
    <script src="@Url.Content(js/jquery.magnific-popup.js)" type="text/javascript"></script>
    <script src="@Url.Content(js/modernizr.custom.53451.js)" type="text/javascript"></script> 

    <form id="form1" runat="server">
        <div class="pricing-plans">
			<div class="wrap">
			    <div class="price-head">
				    <h1>欢迎使用学生考勤管理系统</h1>
			    </div>
                <div class="pricing-grids">
                <!--教师登录按钮-->
                <div class="pricing-grid1">
                    <div class="price-value">
                        <h2><a href="#">我是教师</a></h2>
                        <h5><span>____________________</span></h5>
                        <div class="sale-box one">
                            <span class="on_sale title_shop">★</span>
                        </div>
                    </div>
                    <div class="price-bg">
                        <ul>
                            <li class="whyt"><a>1.在线签到</a></li>
                            <li><a>2.个人资料修改</a></li>
                            <li class="whyt"><a>3.每日励志</a></li>
                            <li><a>4.other</a></li>
                            <li class="whyt"><a>5.ohter</a></li>
                        </ul>
                        <div class="cart2">
                            <asp:Button class="btn btn-large btn btn-block btn-primary" ID="Button1" runat="server" Text="登  录" OnClick="Button1_Click1" />
                        </div>
                    </div>
                </div>

                <!--辅导员登录按钮-->
                <div class="pricing-grid2">
                    <div class="price-value two">
                        <h3><a href="#">我是辅导员</a></h3>
                        <h5><span>____________________</span></h5>
                        <div class="sale-box two">
                            <span class="on_sale title_shop">★</span>
                        </div>
                    </div>
                    <div class="price-bg">
                        <ul>
                            <li class="whyt"><a>1.学生考勤查询 </a></li>
                            <li><a>2.个人资料修改</a></li>
                            <li class="whyt"><a>3.在线请假批复</a></li>
                            <li><a>4.每日励志</a></li>
                            <li class="whyt"><a>5.ohter</a></li>
                        </ul>
                        <div class="cart2">
                            <asp:Button class="btn btn-large btn btn-block btn-primary" ID="Button2" runat="server" Text="登  录" OnClick="Button2_Click" />
                        </div>
                    </div>
                </div>

                <!--学生登录按钮-->
                <div class="pricing-grid3">
                    <div class="price-value three">
                        <h4><a href="#">我是学生</a></h4>
                        <h5><span>____________________</span></h5>
							<div class="sale-box three">
								<span class="on_sale title_shop">★</span>
							</div>
                    </div>
                    <div class="price-bg">
                        <ul>
                            <li class="whyt"><a>1.个人考勤查询 </a></li>
                            <li><a>2.个人资料修改</a></li>
                            <li class="whyt"><a>3.在线请假申请</a></li>
                            <li><a>4.每日励志</a></li>
                            <li class="whyt"><a>5.ohter</a></li>
                        </ul>
                        <div class="cart3">
                            <asp:Button class="btn btn-large btn btn-block btn-primary" ID="Button3" runat="server" Text="登  录" OnClick="Button3_Click" />
                        </div>
                    </div>
                </div>

                <div class="clear">
                </div>
                <div id="small-dialog" class="mfp-hide">
                    <div class="pop_up">
                    </div>
                </div>
            </div>
                <div class="clear">
				</div>
            </div>
	    </div>
        <div class="footer">
			<div class="wrap">
					<p>Copyright &copy; 2015.Company name All rights reserved.<a target="_blank">天津中德职业技术学院</a></p>
			</div>
		</div>
    </form>
</body>
</html>
