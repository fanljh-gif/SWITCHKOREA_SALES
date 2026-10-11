using SalesManagement.Core.Models;
namespace SalesManagement.Core.Quotations;
public static class QuoteCalculator
{
    public static QuoteAmounts Calculate(QuoteCalculationInput input)
    {
        if (input.Lines.Count == 0 || input.Lines.Any(x => x.Quantity <= 0 || x.UnitPrice < 0)) throw new InvalidDataException("유효한 견적 품목이 필요합니다.");
        var digits = input.Currency == Currency.KRW ? 0 : 2; decimal Round(decimal value) => Math.Round(value, digits, MidpointRounding.AwayFromZero);
        var lines = input.Lines.Select(x => Round(x.Quantity * x.UnitPrice)).ToArray(); var subtotal = Round(lines.Sum()); var payable = Round(subtotal - input.DiscountAmount + input.ShippingAmount + input.OtherAmount);
        if (input.DiscountAmount < 0 || input.ShippingAmount < 0 || input.OtherAmount < 0 || payable < 0) throw new InvalidDataException("할인과 비용은 0 이상이며 합계가 음수가 될 수 없습니다.");
        var supply = input.TaxType == QuoteTaxType.Inclusive ? Round(payable / 1.1m) : payable;
        var tax = input.TaxType switch { QuoteTaxType.Exclusive => Round(payable * .1m), QuoteTaxType.Inclusive => Round(payable - supply), _ => 0 };
        return new(subtotal, supply, tax, input.TaxType == QuoteTaxType.Exclusive ? Round(payable + tax) : payable, lines);
    }
    public static (string? IncotermCode, string? NamedPlace) NormalizeTrade(QuoteTradeType tradeType, string? incotermCode, string? namedPlace)
    {
        if (tradeType == QuoteTradeType.Domestic) return (null, null);
        if (string.IsNullOrWhiteSpace(incotermCode) || string.IsNullOrWhiteSpace(namedPlace)) throw new InvalidDataException("수출 견적에는 Incoterms와 지정 장소가 필요합니다.");
        return (incotermCode.Trim().ToUpperInvariant(), namedPlace.Trim());
    }
}
