<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Edit.aspx.cs" Inherits="ApexCounter.CustomerEditPage" MasterPageFile="~/Site.Master" Title="Customer" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="card">
        <h2>Customer</h2>
        <asp:Label ID="lblMessage" runat="server" CssClass="msg" />
        <div class="row">
            <asp:Label runat="server" AssociatedControlID="txtAccount" Text="Account code" />
            <asp:TextBox ID="txtAccount" runat="server" MaxLength="20" />
            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtAccount" CssClass="err" ErrorMessage="Enter an account code." Display="Dynamic" />
        </div>
        <div class="row">
            <asp:Label runat="server" AssociatedControlID="txtCompany" Text="Company" />
            <asp:TextBox ID="txtCompany" runat="server" MaxLength="120" />
            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtCompany" CssClass="err" ErrorMessage="Enter a company name." Display="Dynamic" />
        </div>
        <div class="row">
            <asp:Label runat="server" AssociatedControlID="txtContact" Text="Contact" />
            <asp:TextBox ID="txtContact" runat="server" MaxLength="80" />
            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtContact" CssClass="err" ErrorMessage="Enter a contact name." Display="Dynamic" />
        </div>
        <div class="row">
            <asp:Label runat="server" AssociatedControlID="txtPhone" Text="Phone" />
            <asp:TextBox ID="txtPhone" runat="server" MaxLength="30" />
            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtPhone" CssClass="err" ErrorMessage="Enter a phone number." Display="Dynamic" />
        </div>
        <div class="row">
            <asp:Label runat="server" AssociatedControlID="txtCity" Text="City" />
            <asp:TextBox ID="txtCity" runat="server" MaxLength="60" />
            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtCity" CssClass="err" ErrorMessage="Enter a city." Display="Dynamic" />
        </div>
        <div class="row">
            <asp:Label runat="server" AssociatedControlID="chkActive" Text="Active" />
            <asp:CheckBox ID="chkActive" runat="server" Checked="true" />
        </div>
        <asp:Button ID="btnSave" runat="server" Text="Save" OnClick="btnSave_Click" />
        <asp:Button ID="btnCancel" runat="server" Text="Back" CssClass="btn quiet" CausesValidation="false" OnClick="btnCancel_Click" />
    </div>
</asp:Content>
