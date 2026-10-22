<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="List.aspx.cs" Inherits="ApexCounter.CatalogListPage" MasterPageFile="~/Site.Master" Title="Catalog" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="card">
        <h2>Catalog</h2>
        <p class="note">A row is marked when on hand is at or under the reorder point. This list includes inactive products. The low-stock report does not.</p>
        <div class="row">
            <asp:TextBox ID="txtSearch" runat="server" />
            <asp:CheckBox ID="chkLow" runat="server" Text="Low stock only" />
            <asp:Button ID="btnSearch" runat="server" Text="Search" OnClick="btnSearch_Click" />
            <a href="Edit.aspx">New product</a>
        </div>
        <asp:GridView ID="gvProducts" runat="server" CssClass="grid" AutoGenerateColumns="false" GridLines="None"
            AllowPaging="true" PageSize="5" OnPageIndexChanging="gvProducts_PageIndexChanging"
            OnRowDataBound="gvProducts_RowDataBound" EmptyDataText="No products.">
            <AlternatingRowStyle CssClass="alt" />
            <PagerStyle CssClass="pager" />
            <Columns>
                <asp:BoundField DataField="Sku" HeaderText="SKU" />
                <asp:BoundField DataField="ProductName" HeaderText="Name" />
                <asp:BoundField DataField="UnitPrice" HeaderText="Price" DataFormatString="{0:N2}" HtmlEncode="false" />
                <asp:BoundField DataField="OnHand" HeaderText="On hand" />
                <asp:BoundField DataField="ReorderPoint" HeaderText="Reorder" />
                <asp:BoundField DataField="ActiveText" HeaderText="Active" />
                <asp:HyperLinkField Text="Open" DataNavigateUrlFields="ProductId" DataNavigateUrlFormatString="Edit.aspx?id={0}" />
            </Columns>
        </asp:GridView>
    </div>
</asp:Content>
