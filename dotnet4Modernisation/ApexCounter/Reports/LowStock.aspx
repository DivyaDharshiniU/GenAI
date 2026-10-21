<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="LowStock.aspx.cs" Inherits="ApexCounter.LowStockPage" MasterPageFile="~/Site.Master" Title="Low stock" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="card">
        <h2>Low stock</h2>
        <p class="note">Active products only, where on hand is at or under the reorder point. Inactive products stay off this report even when they are low. The catalog filter is the list that includes them.</p>
        <p><asp:Label ID="lblCount" runat="server" /> active products</p>
        <asp:GridView ID="gvLow" runat="server" CssClass="grid" AutoGenerateColumns="false" GridLines="None"
            AllowPaging="true" PageSize="4" OnPageIndexChanging="gvLow_PageIndexChanging"
            OnRowDataBound="gvLow_RowDataBound" EmptyDataText="Nothing is low.">
            <AlternatingRowStyle CssClass="alt" />
            <PagerStyle CssClass="pager" />
            <Columns>
                <asp:BoundField DataField="Sku" HeaderText="SKU" />
                <asp:BoundField DataField="ProductName" HeaderText="Name" />
                <asp:BoundField DataField="UnitPrice" HeaderText="Price" DataFormatString="{0:N2}" HtmlEncode="false" />
                <asp:BoundField DataField="OnHand" HeaderText="On hand" />
                <asp:BoundField DataField="ReorderPoint" HeaderText="Reorder" />
                <asp:HyperLinkField Text="Open" DataNavigateUrlFields="ProductId" DataNavigateUrlFormatString="../Catalog/Edit.aspx?id={0}" />
            </Columns>
        </asp:GridView>
    </div>
</asp:Content>
