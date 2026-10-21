<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="ApexCounter.LoginPage" %>
<!DOCTYPE html>
<html>
<head runat="server">
    <title>Sign in · Apex Counter</title>
    <link rel="stylesheet" runat="server" href="~/Content/site.css" />
</head>
<body class="login-body">
    <form id="form1" runat="server" defaultbutton="btnLogin">
        <div class="login-card">
            <p class="stamp">Legacy · .NET Framework 4.0</p>
            <h1>Apex Counter</h1>
            <p>Parts desk for Apex Industrial Supply. Stock changes only when an order is picked.</p>
            <div class="row">
                <asp:Label ID="lblUserCaption" runat="server" AssociatedControlID="txtUser" Text="Username" />
                <asp:TextBox ID="txtUser" runat="server" />
                <asp:RequiredFieldValidator ID="valUser" runat="server" ControlToValidate="txtUser" CssClass="err" ErrorMessage="Enter a username." Display="Dynamic" />
            </div>
            <div class="row">
                <asp:Label ID="lblPasswordCaption" runat="server" AssociatedControlID="txtPassword" Text="Password" />
                <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" />
                <asp:RequiredFieldValidator ID="valPassword" runat="server" ControlToValidate="txtPassword" CssClass="err" ErrorMessage="Enter a password." Display="Dynamic" />
            </div>
            <asp:Button ID="btnLogin" runat="server" Text="Sign in" OnClick="btnLogin_Click" />
            <p class="err"><asp:Label ID="lblError" runat="server" /></p>
            <p class="note">Classroom sign-in: clerk or supervisor. Password counter2011.</p>
        </div>
    </form>
</body>
</html>
