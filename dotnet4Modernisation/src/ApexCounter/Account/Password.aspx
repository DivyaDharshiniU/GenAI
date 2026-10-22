<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Password.aspx.cs" Inherits="ApexCounter.PasswordPage" MasterPageFile="~/Site.Master" Title="Password" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="card">
        <h2>Password</h2>
        <p class="note">The new password must be at least 6 characters. Changing clerk leaves supervisor on counter2011, and the other way around.</p>
        <asp:Label ID="lblMessage" runat="server" CssClass="msg" />
        <div class="row">
            <label>Current</label>
            <asp:TextBox ID="txtCurrent" runat="server" TextMode="Password" />
            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtCurrent" CssClass="err" ErrorMessage="Enter the current password." Display="Dynamic" />
        </div>
        <div class="row">
            <label>New</label>
            <asp:TextBox ID="txtNew" runat="server" TextMode="Password" />
            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtNew" CssClass="err" ErrorMessage="Enter a new password." Display="Dynamic" />
        </div>
        <div class="row">
            <label>Confirm</label>
            <asp:TextBox ID="txtConfirm" runat="server" TextMode="Password" />
            <asp:CompareValidator runat="server" ControlToValidate="txtConfirm" ControlToCompare="txtNew" CssClass="err" ErrorMessage="The new password and the confirmation do not match." Display="Dynamic" />
        </div>
        <asp:Button ID="btnSave" runat="server" Text="Update password" OnClick="btnSave_Click" />
    </div>
</asp:Content>
