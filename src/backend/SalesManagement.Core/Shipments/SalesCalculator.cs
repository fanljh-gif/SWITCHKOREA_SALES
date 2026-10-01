using SalesManagement.Core.Models;

namespace SalesManagement.Core.Shipments;

public readonly record struct SalesAmounts(decimal SupplyAmount, decimal KrwAmount);

public static class SalesCalculator
{
    public static SalesAmounts Calculate(decimal quantity, decimal unitPrice, Currency currency, decimal monthlyRate)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice));
        if (currency == Currency.KRW && monthlyRate != 1m) throw new ArgumentException("KRW 환율은 1이어야 합니다.", nameof(monthlyRate));
        if (currency != Currency.KRW && monthlyRate <= 0) throw new ArgumentOutOfRangeException(nameof(monthlyRate));
        var supplyAmount = quantity * unitPrice;
        return new SalesAmounts(supplyAmount, supplyAmount * monthlyRate);
    }
}
