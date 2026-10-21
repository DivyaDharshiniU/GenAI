<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="List.aspx.cs" Inherits="ApexCounter.CustomerListPage" MasterPageFile="~/Site.Master" Title="Customers" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="card">
        <h2>Customers</h2>
        <p class="note">Opens on active accounts. Retired Yard Co. is inactive and stays off this list until you choose All or Inactive.</p>
        <div class="row">
            <asp:TextBox ID="txtSearch" runat="server" />
            <asp:DropDownList ID="ddlActive" runat="server">
                <asp:ListItem Value="all" Text="All" />
                <asp:ListItem Value="yes" Text="Active" Selected="True" />
                <asp:ListItem Value="no" Text="Inactive" />
            </asp:DropDownList>
            <asp:Button ID="btnSearch" runat="server" Text="Search" OnClick="btnSearch_Click" />
            <a href="Edit.aspx">New customer</a>
        </div>
        <asp:GridView ID="gvCustomers" runat="server" CssClass="grid" AutoGenerateColumns="false" GridLines="None"
            AllowPaging="true" PageSize="5" OnPageIndexChanging="gvCustomers_PageIndexChanging" EmptyDataText="No customers.">
            <AlternatingRowStyle CssClass="alt" />
            <PagerStyle CssClass="pager" />
            <Columns>
                <asp:BoundField DataField="AccountCode" HeaderText="Account" />
                <asp:BoundField DataField="CompanyName" HeaderText="Company" />
                <asp:BoundField DataField="ContactName" HeaderText="Contact" />
                <asp:BoundField DataField="Phone" HeaderText="Phone" />
                <asp:BoundField DataField="City" HeaderText="City" />
                <asp:BoundField DataField="ActiveText" HeaderText="Active" />
                <asp:HyperLinkField Text="Open" DataNavigateUrlFields="CustomerId" DataNavigateUrlFormatString="Edit.aspx?id={0}" />
            </Columns>
        </asp:GridView>
    </div>
</asp:Content>
