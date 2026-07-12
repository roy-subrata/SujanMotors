using AutoPartShop.Domain.Entities;
using Xunit;

namespace AutoPartShop.Api.Tests;

public class CustomerEntitySmokeTests
{
    [Fact]
    public void Customer_Create_Valid_ShouldSetProperties()
    {
        var c = Customer.Create("CUST-001", "John", "Doe", "john@test.com",
            "+8801712345678", "ABC Corp", "123 Main St", "123 Main St",
            "Dhaka", "Dhaka", "1205", "Bangladesh");

        Assert.Equal("CUST-001", c.CustomerCode);
        Assert.Equal("John", c.FirstName);
        Assert.Equal("Doe", c.LastName);
        Assert.Equal("john@test.com", c.Email);
        Assert.Equal("+8801712345678", c.Phone);
        Assert.Equal("ABC Corp", c.CompanyName);
        Assert.Equal("RETAIL", c.CustomerType);
        Assert.Equal("ACTIVE", c.Status);
        Assert.Equal(0, c.CurrentBalance);
    }

    [Fact]
    public void Customer_Create_EmptyCustomerCode_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Customer.Create("", "John", "Doe", "e", "p", "c", "a", "a", "c", "s", "z", "co"));
    }

    [Fact]
    public void Customer_Create_EmptyFirstName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Customer.Create("C001", "", "Doe", "e", "p", "c", "a", "a", "c", "s", "z", "co"));
    }

    [Fact]
    public void Customer_Create_EmptyLastName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Customer.Create("C001", "John", "", "e", "p", "c", "a", "a", "c", "s", "z", "co"));
    }

    [Fact]
    public void Customer_Create_EmptyPhone_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Customer.Create("C001", "John", "Doe", "e", "", "c", "a", "a", "c", "s", "z", "co"));
    }

    [Fact]
    public void Customer_Create_InvalidCustomerType_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Customer.Create("C001", "John", "Doe", "e", "p", "c", "a", "a", "c", "s", "z", "co",
                customerType: "INVALID"));
    }

    [Fact]
    public void Customer_Create_AllCustomerTypes_ShouldSet()
    {
        foreach (var t in new[] { "RETAIL", "WHOLESALE", "CORPORATE", "DISTRIBUTOR" })
        {
            var c = Customer.Create("C-" + t, "John", "Doe", "e", "p", "c", "a", "a", "c", "s", "z", "co",
                customerType: t);
            Assert.Equal(t.ToUpper(), c.CustomerType);
        }
    }

    [Fact]
    public void Customer_GetFullName_ShouldCombine()
    {
        var c = Customer.Create("C001", "John", "Doe", "e", "p", "c", "a", "a", "c", "s", "z", "co");
        Assert.Equal("John Doe", c.GetFullName());
    }

    [Fact]
    public void Customer_RecordPurchase_ShouldIncreaseTotal()
    {
        var c = Customer.Create("C001", "John", "Doe", "e", "p", "c", "a", "a", "c", "s", "z", "co");
        c.RecordPurchase(500m);
        Assert.Equal(500m, c.TotalPurchaseAmount);
        Assert.NotNull(c.LastPurchaseDate);
    }

    [Fact]
    public void Customer_RecordPurchase_NonPositive_Throws()
    {
        var c = Customer.Create("C001", "John", "Doe", "e", "p", "c", "a", "a", "c", "s", "z", "co");
        Assert.Throws<ArgumentException>(() => c.RecordPurchase(0));
        Assert.Throws<ArgumentException>(() => c.RecordPurchase(-1));
    }

    [Fact]
    public void Customer_ReverseRecordPurchase_ShouldDecrease()
    {
        var c = Customer.Create("C001", "John", "Doe", "e", "p", "c", "a", "a", "c", "s", "z", "co");
        c.RecordPurchase(1000m);
        c.ReverseRecordPurchase(300m);
        Assert.Equal(700m, c.TotalPurchaseAmount);
    }

    [Fact]
    public void Customer_ReverseRecordPurchase_FloorsAtZero()
    {
        var c = Customer.Create("C001", "John", "Doe", "e", "p", "c", "a", "a", "c", "s", "z", "co");
        c.RecordPurchase(100m);
        c.ReverseRecordPurchase(500m);
        Assert.Equal(0, c.TotalPurchaseAmount);
    }

    [Fact]
    public void Customer_ReverseRecordPurchase_NonPositive_Throws()
    {
        var c = Customer.Create("C001", "John", "Doe", "e", "p", "c", "a", "a", "c", "s", "z", "co");
        Assert.Throws<ArgumentException>(() => c.ReverseRecordPurchase(0));
        Assert.Throws<ArgumentException>(() => c.ReverseRecordPurchase(-1));
    }

    [Fact]
    public void Customer_CanPlaceOrder_Active_ReturnsTrue()
    {
        var c = Customer.Create("C001", "John", "Doe", "e", "p", "c", "a", "a", "c", "s", "z", "co");
        Assert.True(c.CanPlaceOrder());
    }

    [Fact]
    public void Customer_StatusTransitions_ShouldWork()
    {
        var c = Customer.Create("C001", "John", "Doe", "e", "p", "c", "a", "a", "c", "s", "z", "co");
        Assert.Equal("ACTIVE", c.Status);

        c.Deactivate();
        Assert.Equal("INACTIVE", c.Status);

        c.Activate();
        Assert.Equal("ACTIVE", c.Status);

        c.Suspend();
        Assert.Equal("SUSPENDED", c.Status);

        c.Blacklist();
        Assert.Equal("BLACKLISTED", c.Status);
    }

    [Fact]
    public void Customer_ActivateFromBlacklisted_Throws()
    {
        var c = Customer.Create("C001", "John", "Doe", "e", "p", "c", "a", "a", "c", "s", "z", "co");
        c.Blacklist();
        Assert.Throws<InvalidOperationException>(() => c.Activate());
    }

    [Fact]
    public void Customer_UpdateContactInfo_ValidatesPhone()
    {
        var c = Customer.Create("C001", "John", "Doe", "e", "p", "c", "a", "a", "c", "s", "z", "co");
        Assert.Throws<ArgumentException>(() => c.UpdateContactInfo("e", ""));
    }

    [Fact]
    public void Customer_UpdateContactInfo_ShouldSet()
    {
        var c = Customer.Create("C001", "John", "Doe", "old@test.com", "old", "c", "a", "a", "c", "s", "z", "co");
        c.UpdateContactInfo("new@test.com", "+8801711111111", "+8801722222222");
        Assert.Equal("new@test.com", c.Email);
        Assert.Equal("+8801711111111", c.Phone);
        Assert.Equal("+8801722222222", c.AlternatePhone);
    }

    [Fact]
    public void Customer_UpdateContactInfo_WithCustomerType_ShouldSet()
    {
        var c = Customer.Create("C001", "John", "Doe", "e", "p", "c", "a", "a", "c", "s", "z", "co");
        c.UpdateContactInfo("e", "p", customerType: "WHOLESALE");
        Assert.Equal("WHOLESALE", c.CustomerType);
    }

    [Fact]
    public void Customer_UpdateContactInfo_InvalidCustomerType_Throws()
    {
        var c = Customer.Create("C001", "John", "Doe", "e", "p", "c", "a", "a", "c", "s", "z", "co");
        Assert.Throws<ArgumentException>(() => c.UpdateContactInfo("e", "p", customerType: "INVALID"));
    }

    [Fact]
    public void Customer_UpdateAddress_ShouldSet()
    {
        var c = Customer.Create("C001", "John", "Doe", "e", "p", "c", "a", "a", "c", "s", "z", "co");
        c.UpdateAddress("Billing 1", "Shipping 1", "NYC", "NY", "10001", "USA");
        Assert.Equal("Billing 1", c.BillingAddress);
        Assert.Equal("Shipping 1", c.ShippingAddress);
        Assert.Equal("NYC", c.City);
        Assert.Equal("NY", c.State);
        Assert.Equal("10001", c.PostalCode);
        Assert.Equal("USA", c.Country);
    }

    [Fact]
    public void Customer_UpdateNotes_ShouldSet()
    {
        var c = Customer.Create("C001", "John", "Doe", "e", "p", "c", "a", "a", "c", "s", "z", "co");
        c.UpdateNotes("VIP customer");
        Assert.Equal("VIP customer", c.Notes);
    }

    [Fact]
    public void Customer_SetPrimaryContactPerson_ShouldSet()
    {
        var c = Customer.Create("C001", "John", "Doe", "e", "p", "c", "a", "a", "c", "s", "z", "co");
        c.SetPrimaryContactPerson("Jane Doe");
        Assert.Equal("Jane Doe", c.PrimaryContactPerson);
    }

    [Fact]
    public void Customer_UpdateBalance_ShouldAdd()
    {
        var c = Customer.Create("C001", "John", "Doe", "e", "p", "c", "a", "a", "c", "s", "z", "co");
        c.UpdateBalance(500m);
        Assert.Equal(500m, c.CurrentBalance);
        c.UpdateBalance(-200m);
        Assert.Equal(300m, c.CurrentBalance);
    }

    [Fact]
    public void Customer_ComputedProperties_DefaultToZero()
    {
        var c = Customer.Create("C001", "John", "Doe", "e", "p", "c", "a", "a", "c", "s", "z", "co");
        Assert.Equal(0, c.TotalPaid);
        Assert.Equal(0, c.AccountBalance);
        Assert.Equal(0, c.AdvanceAmount);
        Assert.Equal(0, c.PendingPaymentsCount);
    }

    [Fact]
    public void Customer_ComputedProperties_WithPayments_ShouldCalculate()
    {
        var customerId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var c = Customer.Create("C001", "John", "Doe", "e", "p", "c", "a", "a", "c", "s", "z", "co");

        var field = typeof(Customer).GetField("<CustomerPayments>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var payments = new List<CustomerPayment>
        {
            CustomerPayment.Create(customerId, providerId, 1000m, "CASH"),
            CustomerPayment.Create(customerId, providerId, 500m, "CASH"),
            CustomerPayment.Create(customerId, providerId, 200m, "CASH")
        };

        payments[1].MarkAsAdvance();
        payments[1].ReduceRemainingAmount(200m);
        payments[0].MarkAsCompleted();
        payments[1].MarkAsCompleted();

        field!.SetValue(c, payments);

        // Payment[2] is still PENDING
        Assert.Equal(1, c.PendingPaymentsCount);
    }

    [Fact]
    public void Customer_CustomerCode_Uppercased()
    {
        var c = Customer.Create("cust-001", "John", "Doe", "e", "p", "c", "a", "a", "c", "s", "z", "co");
        Assert.Equal("CUST-001", c.CustomerCode);
    }
}
