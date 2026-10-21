<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="New.aspx.cs" Inherits="ApexCounter.OrderNewPage" MasterPageFile="~/Site.Master" Title="New order" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="card">
        <h2>New order</h2>
        <p class="note">Tax is 5 percent, rounded half away from zero to two decimals. The price is copied onto the line. A later catalog price change does not rewrite this order. Saving leaves the order Open and does not touch stock. The same SKU on a second add increases the quantity and keeps the first price.</p>
        <asp:Label ID="lblMessage" runat="server" CssClass="msg" />
        <div class="row">
            <label>Customer</label>
            <asp:DropDownList ID="ddlCustomer" runat="server" />
        </div>
        <div class="row">
            <label>Product</label>
            <asp:DropDownList ID="ddlProduct" runat="server" />
            <asp:TextBox ID="txtQty" runat="server" Text="1" Width="60" />
            <asp:Button ID="btnAdd" runat="server" Text="Add line" OnClick="btnAdd_Click" />
        </div>
        <asp:GridView ID="gvLines" runat="server" CssClass="grid" AutoGenerateColumns="false" GridLines="None"
            OnRowCommand="gvLines_RowCommand" EmptyDataText="No lines yet.">
            <AlternatingRowStyle CssClass="alt" />
            <Columns>
                <asp:BoundField DataField="Sku" HeaderText="SKU" />
                <asp:BoundField DataField="ProductName" HeaderText="Name" />
                <asp:BoundField DataField="Quantity" HeaderText="Qty" />
                <asp:BoundField DataField="UnitPrice" HeaderText="Price" DataFormatString="{0:N2}" HtmlEncode="false" />
                <asp:BoundField DataField="LineTotal" HeaderText="Line" DataFormatString="{0:N2}" HtmlEncode="false" />
                <asp:TemplateField>
                    <ItemTemplate>
                        <asp:LinkButton ID="btnRemove" runat="server" CommandName="remove" CommandArgument='<%# Container.DataItemIndex %>' Text="Remove" CausesValidation="false" />
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
        <div class="totals">
            <div>Subtotal <asp:Label ID="lblSubtotal" runat="server" Text="0.00" /></div>
            <div>Tax <asp:Label ID="lblTax" runat="server" Text="0.00" /></div>
            <div>Total <asp:Label ID="lblTotal" runat="server" Text="0.00" /></div>
        </div>
        <asp:Button ID="btnSave" runat="server" Text="Open order" OnClick="btnSave_Click" />
        <asp:Button ID="btnClear" runat="server" Text="Clear draft" CssClass="btn quiet" CausesValidation="false" OnClick="btnClear_Click" />
    </div>
</asp:Content>
