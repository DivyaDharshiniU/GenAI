using System;

namespace ApexCounter
{
    /* Every sentence the desk shows for a rule. The repair agents compare these words. */
    public static class Messages
    {
        public const string LoginFailed = "That username and password do not match an active account.";
        public const string DbDown = "The counter database is not reachable. Run database/reset.ps1 against LocalDB, then refresh.";
        public const string NoStatusChange = "No status change.";
        public const string StatusApplied = "Order {0} is now {1}.";
        public const string StockShort = "Not enough stock for {0}. On hand is {1}, this order needs {2}.";
        public const string IllegalStatus = "An order that is {0} cannot move to {1}.";
        public const string NeedCustomer = "Choose a customer.";
        public const string CustomerInactive = "That customer is not active.";
        public const string NeedLines = "Add at least one line.";
        public const string NeedProduct = "Choose a product.";
        public const string ProductInactive = "That product is not active.";
        public const string OrderSaved = "Opened {0}.";
        public const string LineAdded = "Added {0}.";
        public const string LineMerged = "Added {0} more to {1}. On this order the quantity is now {2}.";
        public const string CustomerSaved = "Saved {0}.";
        public const string ProductSaved = "Saved {0}.";
        public const string AdjustOk = "On hand for {0} is now {1}.";
        public const string AdjustZero = "Enter a quantity that is not zero.";
        public const string AdjustBelow = "That adjustment would take {0} below zero. On hand is {1}.";
        public const string AdjustNote = "Enter a reason for the stock change.";
        public const string PasswordOk = "Password updated.";
        public const string PasswordWrong = "The current password does not match.";
        public const string PasswordShort = "The new password must be at least 6 characters.";
        public const string PasswordMismatch = "The new password and the confirmation do not match.";
        public const string DuplicateAccount = "Account code {0} is already used.";
        public const string DuplicateSku = "SKU {0} is already used.";
        public const string NotFound = "That record is not on file.";
        public const string QtyRequired = "Enter a whole number of 1 or more.";
        public const string BadPrice = "Enter a price of 0 or more.";
        public const string BadReorder = "Enter a reorder point of 0 or more.";
        public const string BadAdjust = "Enter a whole number for the adjustment, other than zero.";
    }

    /* Half away from zero, two decimal places. Matches SQL Server ROUND on decimal. */
    public static class Money
    {
        public const decimal TaxRate = 0.05m;

        public static decimal Round(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        public static decimal LineTotal(int quantity, decimal unitPrice)
        {
            return Round(quantity * unitPrice);
        }

        public static decimal Tax(decimal subtotal)
        {
            return Round(subtotal * TaxRate);
        }
    }

    public sealed class ActionResult
    {
        public bool Ok { get; private set; }
        public string Message { get; private set; }
        public int Id { get; private set; }

        public static ActionResult Success(string message, int id)
        {
            ActionResult result = new ActionResult();
            result.Ok = true;
            result.Message = message;
            result.Id = id;
            return result;
        }

        public static ActionResult Fail(string message)
        {
            ActionResult result = new ActionResult();
            result.Ok = false;
            result.Message = message;
            result.Id = 0;
            return result;
        }
    }
}
