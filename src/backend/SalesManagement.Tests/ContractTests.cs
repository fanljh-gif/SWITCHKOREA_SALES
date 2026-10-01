using SalesManagement.Core.Models;

namespace SalesManagement.Tests;

public sealed class ContractTests
{
    [Fact]
    public void Currency_AllowsOnlyApprovedValues()
    {
        Assert.Equal(new[] { "KRW", "USD", "CNY" }, Enum.GetNames<Currency>());
    }

    [Fact]
    public void ItemType_AllowsOnlyProductAndGoods()
    {
        Assert.Equal(new[] { "Product", "Goods" }, Enum.GetNames<ItemType>());
    }
}
