<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Edit.aspx.cs" Inherits="ApexCounter.CatalogEditPage" MasterPageFile="~/Site.Master" Title="Product" %>
<asp:Content ID="Main" ContentPlaceHolderID="MainContent" runat="server">
    <div class="card">
        <h2>Product</h2>
        <p class="note">Saving the price does not change lines already on an order. On hand changes only through Adjust stock. A new product starts at 0.</p>
        <asp:Label ID="lblMessage" runat="server" CssClass="msg" />
        <div class="row">
            <label>SKU</label>
            <asp:TextBox ID="txtSku" runat="server" MaxLength="20" />
            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtSku" CssClass="err" ErrorMessage="Enter a SKU." Display="Dynamic" />
        </div>
        <div class="row">
            <label>Name</label>
            <asp:TextBox ID="txtName" runat="server" MaxLength="120" />
            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtName" CssClass="err" ErrorMessage="Enter a name." Display="Dynamic" />
        </div>
        <div class="row">
            <label>Price</label>
            <asp:TextBox ID="txtPrice" runat="server" />
            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtPrice" CssClass="err" ErrorMessage="Enter a price." Display="Dynamic" />
        </div>
        <div class="row">
            <label>Reorder point</label>
            <asp:TextBox ID="txtReorder" runat="server" />
            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtReorder" CssClass="err" ErrorMessage="Enter a reorder point." Display="Dynamic" />
        </div>
        <div class="row">
            <label>On hand</label>
            <asp:Label ID="lblOnHand" runat="server" Text="0" />
        </div>
        <div class="row">
            <label>Active</label>
            <asp:CheckBox ID="chkActive" runat="server" Checked="true" />
        </div>
        <asp:Button ID="btnSave" runat="server" Text="Save" OnClick="btnSave_Click" />
        <asp:Button ID="btnCancel" runat="server" Text="Back" CssClass="btn quiet" CausesValidation="false" OnClick="btnCancel_Click" />
    </div>
    <asp:Panel ID="pnlAdjust" runat="server" CssClass="card" Visible="false">
        <h2>Adjust stock</h2>
        <p class="note">Use a negative number to remove stock. Zero is refused. The reason is written on the ledger.</p>
        <div class="row">
            <label>Quantity change</label>
            <asp:TextBox ID="txtAdjustQty" runat="server" />
        </div>
        <div class="row">
            <label>Reason</label>
            <asp:TextBox ID="txtAdjustNote" runat="server" MaxLength="200" />
        </div>
        <asp:Button ID="btnAdjust" runat="server" Text="Adjust" CausesValidation="false" OnClick="btnAdjust_Click" />
    </asp:Panel>
</asp:Content>
