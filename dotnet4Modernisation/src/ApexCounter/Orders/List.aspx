<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="List.aspx.cs" Inherits="ApexCounter.OrderListPage" MasterPageFile="~/Site.Master" Title="Orders" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="card">
        <h2>Orders</h2>
        <div class="row">
            <asp:DropDownList ID="ddlStatus" runat="server">
                <asp:ListItem Value="all" Text="All statuses" />
                <asp:ListItem Value="Open" Text="Open" />
                <asp:ListItem Value="Picked" Text="Picked" />
                <asp:ListItem Value="Invoiced" Text="Invoiced" />
                <asp:ListItem Value="Cancelled" Text="Cancelled" />
            </asp:DropDownList>
            <asp:TextBox ID="txtCustomer" runat="server" />
            <asp:Button ID="btnFilter" runat="server" Text="Filter" OnClick="btnFilter_Click" />
            <a href="New.aspx">New order</a>
        </div>
        <asp:GridView ID="gvOrders" runat="server" CssClass="grid" AutoGenerateColumns="false" GridLines="None"
            AllowPaging="true" PageSize="5" OnPageIndexChanging="gvOrders_PageIndexChanging" EmptyDataText="No orders.">
            <AlternatingRowStyle CssClass="alt" />
            <PagerStyle CssClass="pager" />
            <Columns>
                <asp:BoundField DataField="OrderNumber" HeaderText="Order" />
                <asp:BoundField DataField="CompanyName" HeaderText="Customer" />
                <asp:BoundField DataField="Status" HeaderText="Status" />
                <asp:BoundField DataField="CreatedAt" HeaderText="Opened" DataFormatString="{0:dd MMM yyyy HH:mm}" HtmlEncode="false" />
                <asp:BoundField DataField="Total" HeaderText="Total" DataFormatString="{0:N2}" HtmlEncode="false" />
                <asp:HyperLinkField Text="Open" DataNavigateUrlFields="OrderId" DataNavigateUrlFormatString="Detail.aspx?id={0}" />
            </Columns>
        </asp:GridView>
    </div>
</asp:Content>
