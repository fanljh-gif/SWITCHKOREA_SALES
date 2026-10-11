import '@testing-library/jest-dom/vitest'
import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'

import { QuoteEditor } from '../src/features/quotes/QuoteEditor'
import { App } from '../src/app/App'
import { MockSalesApi } from '../src/shared/api/MockSalesApi'

async function editorFixture(approved = false) {
  const api = new MockSalesApi()
  let quote = await api.getQuote('quote-seed-2')
  if (approved) {
    const submitted = await api.submitQuoteRevision(quote.id, quote.currentRevision.id, quote.currentRevision.version)
    await api.approveQuoteRevision(quote.id, submitted.id, submitted.version)
    quote = await api.getQuote(quote.id)
  }
  const onSave = vi.fn()
  render(<QuoteEditor
    quote={quote}
    user={(await api.getCurrentUser())!}
    incoterms={await api.listIncoterms()}
    products={await api.listProducts()}
    busy={false}
    onSave={onSave}
    onSubmit={() => {}}
    onApprove={() => {}}
    onReject={() => {}}
    onNewRevision={() => {}}
    onPdf={() => {}}
    onDispatch={() => {}}
    onResult={() => {}}
  />)
  return { onSave, quote }
}

describe('quote commercial terms editor', () => {
  it.each([['결제조건', 501, 500], ['납기조건', 501, 500], ['비고', 4001, 4000]] as const)(
    'shows an API validation error for overlong %s', async (field, length, maximum) => {
      window.history.pushState({}, '', '/quotes')
      render(<App api={new MockSalesApi()} />)
      fireEvent.click(await screen.findByRole('button', { name: /SWK-202610-002/ }))
      // Bypass native textarea limits to exercise the API-error display path.
      fireEvent.change(screen.getByRole('textbox', { name: field }), { target: { value: 'x'.repeat(length) } })
      fireEvent.click(screen.getByRole('button', { name: '견적 저장' }))
      expect(await screen.findByRole('alert')).toHaveTextContent(String(maximum))
    },
  )

  it('includes edited payment, delivery, and customer notes in the save payload', async () => {
    const { onSave, quote } = await editorFixture()
    fireEvent.change(screen.getByRole('textbox', { name: '결제조건' }), { target: { value: 'T/T 30% 선금' } })
    fireEvent.change(screen.getByRole('textbox', { name: '납기조건' }), { target: { value: '발주 후 4주' } })
    fireEvent.change(screen.getByRole('textbox', { name: '비고' }), { target: { value: '표준 수출 포장\n납기 재확인' } })
    expect(screen.getByRole('button', { name: '승인 요청' })).toBeDisabled()
    fireEvent.click(screen.getByRole('button', { name: '견적 저장' }))
    expect(onSave).toHaveBeenCalledWith(expect.objectContaining({
      paymentTerms: 'T/T 30% 선금',
      deliveryTerms: '발주 후 4주',
      notes: '표준 수출 포장\n납기 재확인',
      expectedVersion: quote.currentRevision.version,
    }))
  })

  it('locks commercial terms on approved revisions', async () => {
    await editorFixture(true)
    expect(screen.getByRole('textbox', { name: '결제조건' })).toBeDisabled()
    expect(screen.getByRole('textbox', { name: '납기조건' })).toBeDisabled()
    expect(screen.getByRole('textbox', { name: '비고' })).toBeDisabled()
    expect(screen.queryByRole('button', { name: '견적 저장' })).not.toBeInTheDocument()
  })

  it('loads legacy quotes with empty terms and keeps saved values after changing selection', async () => {
    window.history.pushState({}, '', '/quotes')
    render(<App api={new MockSalesApi()} />)
    fireEvent.click(await screen.findByRole('button', { name: /SWK-202610-002/ }))
    expect(screen.getByRole('textbox', { name: '결제조건' })).toHaveValue('')
    expect(screen.getByRole('textbox', { name: '납기조건' })).toHaveValue('')
    expect(screen.getByRole('textbox', { name: '비고' })).toHaveValue('')
    fireEvent.change(screen.getByRole('textbox', { name: '결제조건' }), { target: { value: '  출고 전 결제  ' } })
    fireEvent.change(screen.getByRole('textbox', { name: '납기조건' }), { target: { value: '  발주 후 4주  ' } })
    fireEvent.change(screen.getByRole('textbox', { name: '비고' }), { target: { value: '  표준 포장\n분할 출고 협의  ' } })
    fireEvent.click(screen.getByRole('button', { name: '견적 저장' }))
    await screen.findByRole('textbox', { name: '결제조건' })
    fireEvent.click(screen.getByRole('button', { name: /SWK-202610-001/ }))
    fireEvent.click(screen.getByRole('button', { name: /SWK-202610-002/ }))
    expect(screen.getByRole('textbox', { name: '결제조건' })).toHaveValue('출고 전 결제')
    expect(screen.getByRole('textbox', { name: '납기조건' })).toHaveValue('발주 후 4주')
    expect(screen.getByRole('textbox', { name: '비고' })).toHaveValue('표준 포장\n분할 출고 협의')
  })
})
