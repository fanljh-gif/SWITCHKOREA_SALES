import '@testing-library/jest-dom/vitest'
import { fireEvent, render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'

import { SalesApiProvider } from '../src/app/App'
import { ImportWizard } from '../src/features/imports/ImportWizard'
import { MockSalesApi } from '../src/shared/api/MockSalesApi'

it('keeps invalid import rows out of the commit request', async () => {
  const api = new MockSalesApi({
    importValidation: {
      token: 'import-1',
      newCount: 2,
      duplicateCount: 0,
      changeCandidateCount: 0,
      errorCount: 1,
      errors: [{ row: 4, field: '수량', message: '수량은 0보다 커야 합니다.' }],
      canCommit: false,
    },
  })

  render(
    <SalesApiProvider api={api}>
      <MemoryRouter>
        <ImportWizard />
      </MemoryRouter>
    </SalesApiProvider>,
  )

  const file = new File(['shipment data'], 'shipments.xlsx', {
    type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  })
  fireEvent.change(screen.getByLabelText('Excel 파일'), { target: { files: [file] } })
  fireEvent.click(screen.getByRole('button', { name: '검증 실행' }))

  expect(await screen.findByText('오류 1건')).toBeVisible()
  expect(screen.getByText('4행 · 수량 · 수량은 0보다 커야 합니다.')).toBeVisible()
  expect(screen.getByRole('button', { name: '최종 반영' })).toBeDisabled()
})

it('converts an evidence-backed research candidate into the shared network once', async () => {
  const api = new MockSalesApi()
  const before = await api.getProspectCandidate('prospect-hanul')
  const approved = await api.reviewProspectCandidate(before.id, { decision: 'approve', expectedVersion: before.version })
  const first = await api.convertProspectCandidate(approved.id, approved.version)
  const second = await api.convertProspectCandidate(before.id, first.version)
  expect(first.status).toBe('converted')
  expect(second.networkCompanyId).toBe(first.networkCompanyId)
  expect((await api.getNetworkCompany(first.networkCompanyId!)).lifecycleType).toBe('prospect')
})
