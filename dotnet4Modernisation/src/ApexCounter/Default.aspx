<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="ApexCounter.DeskPage" MasterPageFile="~/Site.Master" Title="Desk" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="tiles">
        <div class="tile"><strong><asp:Label ID="lblOpen" runat="server" Text="0" /></strong><span>Open orders</span></div>
        <div class="tile"><strong><asp:Label ID="lblLow" runat="server" Text="0" /></strong><span>Active products at or under the reorder point</span></div>
        <div class="tile"><strong><asp:Label ID="lblSales" runat="server" Text="0.00" /></strong><span>Invoiced today</span></div>
    </div>
    <div class="card">
        <h2>Latest orders</h2>
        <asp:GridView ID="gvRecent" runat="server" CssClass="grid" AutoGenerateColumns="false" GridLines="None" EmptyDataText="No orders yet.">
            <AlternatingRowStyle CssClass="alt" />
            <Columns>
                <asp:BoundField DataField="OrderNumber" HeaderText="Order" />
                <asp:BoundField DataField="CompanyName" HeaderText="Customer" />
                <asp:BoundField DataField="Status" HeaderText="Status" />
                <asp:BoundField DataField="CreatedAt" HeaderText="Opened" DataFormatString="{0:dd MMM yyyy HH:mm}" HtmlEncode="false" />
                <asp:BoundField DataField="Total" HeaderText="Total" DataFormatString="{0:N2}" HtmlEncode="false" />
                <asp:HyperLinkField Text="Open" DataNavigateUrlFields="OrderId" DataNavigateUrlFormatString="Orders/Detail.aspx?id={0}" />
            </Columns>
        </asp:GridView>
    </div>
</asp:Content>
