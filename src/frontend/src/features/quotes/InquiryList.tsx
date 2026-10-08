import type { CustomerInquiryDto } from '../../shared/api/contracts'
const labels: Record<CustomerInquiryDto['status'], string> = { received: '접수', assigned: '담당자 배정', responding: '대응 중', quoting: '견적 작성', answered: '답변 완료', closed: '종료' }
export function InquiryList({ rows, selectedId, onSelect }: { rows: CustomerInquiryDto[]; selectedId?: string; onSelect: (id: string) => void }) {
  return <article className="quote-list-panel"><div className="panel-heading"><h2>고객문의</h2><span>{rows.length}건</span></div><div className="quote-row-list">{rows.map((row) => <button type="button" className={selectedId === row.id ? 'is-selected' : ''} key={row.id} onClick={() => onSelect(row.id)}><strong>{row.title}</strong><span>{row.inquiryNumber} · {row.customerName}</span><small className={row.responseDueAt < '2026-10-08' && !['answered', 'closed'].includes(row.status) ? 'is-overdue' : ''}>{labels[row.status]} · {row.responseDueAt}</small></button>)}</div></article>
}
