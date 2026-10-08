import type { Currency, QuoteTaxType, QuoteTradeType } from '../../shared/api/contracts'

type CalculationInput = { currency: Currency; taxType: QuoteTaxType; subtotalAmount: number; discountAmount: number; shippingAmount: number; otherAmount: number }
const round = (currency: Currency, value: number) => currency === 'KRW' ? Math.round(value) : Math.round(value * 100) / 100

export function calculateQuote(input: CalculationInput) {
  const base = round(input.currency, Math.max(0, input.subtotalAmount - input.discountAmount + input.shippingAmount + input.otherAmount))
  if (input.taxType === 'exclusive') { const taxAmount = round(input.currency, base * 0.1); return { supplyAmount: base, taxAmount, totalAmount: round(input.currency, base + taxAmount) } }
  if (input.taxType === 'inclusive') { const supplyAmount = round(input.currency, base / 1.1); return { supplyAmount, taxAmount: round(input.currency, base - supplyAmount), totalAmount: base } }
  return { supplyAmount: base, taxAmount: 0, totalAmount: base }
}

export function validateQuoteDraft(input: { tradeType: QuoteTradeType; incotermCode: string | null; namedPlace: string | null }) {
  if (input.tradeType === 'export' && (!input.incotermCode?.trim() || !input.namedPlace?.trim())) return '수출 견적은 Incoterms와 지정 장소·항구가 필요합니다.'
  if (input.tradeType === 'domestic' && (input.incotermCode || input.namedPlace)) return '국내 견적에는 Incoterms를 입력할 수 없습니다.'
  return null
}
