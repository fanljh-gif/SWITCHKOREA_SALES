using SalesManagement.Core.Models; using SalesManagement.Core.Quotations;
namespace SalesManagement.Tests;
public sealed class QuoteCalculationTests
{
    [Theory] [InlineData(QuoteTaxType.Exclusive, 31000, 3100, 34100)] [InlineData(QuoteTaxType.Inclusive, 28182, 2818, 31000)] [InlineData(QuoteTaxType.Exempt, 31000, 0, 31000)]
    public void Calculates_tax_modes(QuoteTaxType type, decimal supply, decimal tax, decimal total) { var value = QuoteCalculator.Calculate(new(Currency.KRW, type, 2000, 1000, 0, [new(Guid.NewGuid(), "Switch", 10, 3200)])); Assert.Equal((32000m, supply, tax, total), (value.SubtotalAmount, value.SupplyAmount, value.TaxAmount, value.TotalAmount)); }
    [Fact] public void Rounds_foreign_currency_and_validates_export_terms() { var value = QuoteCalculator.Calculate(new(Currency.USD, QuoteTaxType.Exempt, 0, 0, 0, [new(Guid.NewGuid(), "Switch", 3, 1.005m)])); Assert.Equal(3.02m, value.TotalAmount); Assert.Throws<InvalidDataException>(() => QuoteCalculator.NormalizeTrade(QuoteTradeType.Export, null, null)); Assert.Equal((null, null), QuoteCalculator.NormalizeTrade(QuoteTradeType.Domestic, "CIF", "Busan")); }
}
