<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Detail.aspx.cs" Inherits="ApexCounter.OrderDetailPage" MasterPageFile="~/Site.Master" Title="Order" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="card">
        <h2><asp:Label ID="lblNumber" runat="server" Text="Order" /></h2>
        <asp:Label ID="lblMessage" runat="server" CssClass="msg" />
        <p class="note">Picking deducts stock. If any line is short, nothing is deducted and the order stays Open. Invoicing does not change stock. Cancelling a picked order puts the stock back. Cancelling an open order does not. Invoiced and Cancelled do not move again.</p>
        <div class="row">Customer <asp:Label ID="lblCustomer" runat="server" /></div>
        <div class="row">Status <asp:Label ID="lblStatus" runat="server" /></div>
        <div class="row">Opened <asp:Label ID="lblCreated" runat="server" /></div>
        <div class="row">Picked <asp:Label ID="lblPicked" runat="server" /></div>
        <div class="row">Invoiced <asp:Label ID="lblInvoiced" runat="server" /></div>
        <asp:GridView ID="gvLines" runat="server" CssClass="grid" AutoGenerateColumns="false" GridLines="None" EmptyDataText="No lines.">
            <AlternatingRowStyle CssClass="alt" />
            <Columns>
                <asp:BoundField DataField="Sku" HeaderText="SKU" />
                <asp:BoundField DataField="ProductName" HeaderText="Name" />
                <asp:BoundField DataField="Quantity" HeaderText="Qty" />
                <asp:BoundField DataField="UnitPrice" HeaderText="Price" DataFormatString="{0:N2}" HtmlEncode="false" />
                <asp:BoundField DataField="LineTotal" HeaderText="Line" DataFormatString="{0:N2}" HtmlEncode="false" />
            </Columns>
        </asp:GridView>
        <div class="totals">
            <div>Subtotal <asp:Label ID="lblSubtotal" runat="server" /></div>
            <div>Tax <asp:Label ID="lblTax" runat="server" /></div>
            <div>Total <asp:Label ID="lblTotal" runat="server" /></div>
        </div>
        <asp:Panel ID="pnlApply" runat="server">
            <div class="row">
                <label>Move to</label>
                <asp:DropDownList ID="ddlNext" runat="server" />
                <asp:Button ID="btnApply" runat="server" Text="Apply status" OnClick="btnApply_Click" />
            </div>
        </asp:Panel>
    </div>
    <div class="card">
        <h2>Stock ledger for this order</h2>
        <asp:GridView ID="gvLedger" runat="server" CssClass="grid" AutoGenerateColumns="false" GridLines="None" EmptyDataText="No stock has moved for this order.">
            <AlternatingRowStyle CssClass="alt" />
            <Columns>
                <asp:BoundField DataField="Sku" HeaderText="SKU" />
                <asp:BoundField DataField="QtyChange" HeaderText="Change" />
                <asp:BoundField DataField="Reason" HeaderText="Reason" />
                <asp:BoundField DataField="Note" HeaderText="Note" />
                <asp:BoundField DataField="CreatedAt" HeaderText="When" DataFormatString="{0:dd MMM yyyy HH:mm}" HtmlEncode="false" />
            </Columns>
        </asp:GridView>
    </div>
</asp:Content>
