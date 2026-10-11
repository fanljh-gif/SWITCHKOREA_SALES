import type { QuoteRevisionInput } from '../../shared/api/contracts'

type Terms = Pick<QuoteRevisionInput, 'paymentTerms' | 'deliveryTerms' | 'notes'>

export function QuoteCommercialTerms({ values, disabled, onChange }: {
  values: Terms
  disabled: boolean
  onChange: (field: keyof Terms, value: string) => void
}) {
  return <section className="quote-commercial-terms" aria-label="거래 조건">
    <h3>거래 조건</h3>
    <div className="commercial-terms-grid">
      <label>결제조건 <small>선택 · 최대 500자</small>
        <textarea aria-label="결제조건" disabled={disabled} maxLength={500} rows={3}
          placeholder={disabled ? '미입력' : '예: T/T 30% 선금, 출고 전 잔금 70%'}
          value={values.paymentTerms ?? ''} onChange={(event) => onChange('paymentTerms', event.target.value)} />
      </label>
      <label>납기조건 <small>선택 · 최대 500자</small>
        <textarea aria-label="납기조건" disabled={disabled} maxLength={500} rows={3}
          placeholder={disabled ? '미입력' : '예: 발주 및 선금 확인 후 4주'}
          value={values.deliveryTerms ?? ''} onChange={(event) => onChange('deliveryTerms', event.target.value)} />
      </label>
      <label className="commercial-notes">비고 <small>선택 · 최대 4,000자</small>
        <textarea aria-label="비고" disabled={disabled} maxLength={4000} rows={4}
          placeholder={disabled ? '미입력' : '고객에게 전달할 추가 조건'}
          value={values.notes ?? ''} onChange={(event) => onChange('notes', event.target.value)} />
      </label>
    </div>
    <p className="commercial-help">비고는 고객 전달용입니다. 내부용 메모를 입력하지 마세요. Incoterms·지정 장소는 기존 별도 항목에서 관리합니다.</p>
  </section>
}
