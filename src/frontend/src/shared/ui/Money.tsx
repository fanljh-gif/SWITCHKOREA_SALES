import type { Currency } from '../api/contracts'

export function Money({ amount, currency = 'KRW' }: { amount: number; currency?: Currency }) {
  const formatted = new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency,
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  }).format(amount)

  return <span>{formatted}</span>
}
