import type { Currency, QuoteTaxType, QuoteTradeType } from '../../shared/api/contracts'
import Decimal from 'decimal.js'

type CalculationInput = { currency: Currency; taxType: QuoteTaxType; subtotalAmount: number; discountAmount: number; shippingAmount: number; otherAmount: number }
const round = (currency: Currency, value: Decimal.Value) => new Decimal(value).toDecimalPlaces(currency === 'KRW' ? 0 : 2, Decimal.ROUND_HALF_UP).toNumber()

export const roundQuoteAmount = (currency: Currency, value: number) => round(currency, value)
export const calculateQuoteLineAmount = (currency: Currency, quantity: number, unitPrice: number) => round(currency, new Decimal(quantity).times(unitPrice))
export const calculateQuoteSubtotal = (currency: Currency, lines: Array<{ quantity: number; unitPrice: number }>) =>
  lines.reduce((sum, line) => sum.plus(calculateQuoteLineAmount(currency, line.quantity, line.unitPrice)), new Decimal(0)).toNumber()

export function calculateQuote(input: CalculationInput) {
  const base = round(input.currency, Decimal.max(0, new Decimal(input.subtotalAmount).minus(input.discountAmount).plus(input.shippingAmount).plus(input.otherAmount)))
  if (input.taxType === 'exclusive') { const taxAmount = round(input.currency, new Decimal(base).times(0.1)); return { supplyAmount: base, taxAmount, totalAmount: round(input.currency, new Decimal(base).plus(taxAmount)) } }
  if (input.taxType === 'inclusive') { const supplyAmount = round(input.currency, new Decimal(base).div(1.1)); return { supplyAmount, taxAmount: round(input.currency, new Decimal(base).minus(supplyAmount)), totalAmount: base } }
  return { supplyAmount: base, taxAmount: 0, totalAmount: base }
}

export function validateQuoteDraft(input: { tradeType: QuoteTradeType; incotermCode: string | null; namedPlace: string | null }) {
  if (input.tradeType === 'export' && (!input.incotermCode?.trim() || !input.namedPlace?.trim())) return '수출 견적은 Incoterms와 지정 장소·항구가 필요합니다.'
  if (input.tradeType === 'domestic' && (input.incotermCode || input.namedPlace)) return '국내 견적에는 Incoterms를 입력할 수 없습니다.'
  return null
}
