import type { QuoteDto } from '../../shared/api/contracts'
export function QuoteList({ rows, selectedId, onSelect }: { rows: QuoteDto[]; selectedId?: string; onSelect: (id: string) => void }) {
  return <article className="quote-list-panel"><div className="panel-heading"><h2>견적서</h2><span>{rows.length}건</span></div><div className="quote-row-list">{rows.map((row) => <button type="button" className={selectedId === row.id ? 'is-selected' : ''} key={row.id} onClick={() => onSelect(row.id)}><strong>{row.quoteNumber} · {row.currentRevision.revisionLabel}</strong><span>{row.customerName} · {row.currentRevision.currency} {row.currentRevision.totalAmount.toLocaleString()}</span><small>{row.currentRevision.status}</small></button>)}</div></article>
}
