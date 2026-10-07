import '@testing-library/jest-dom/vitest'
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'

import { App } from '../src/app/App'
import { SalesApiError } from '../src/shared/api/HttpSalesApi'
import { MockSalesApi } from '../src/shared/api/MockSalesApi'
import type { User } from '../src/shared/api/contracts'

const manager: User = {
  id: 'u1', loginId: 'jhlee', name: '이재학', email: 'jhlee@switchkorea.com', role: 'manager', status: 'active',
}

const member: User = {
  id: 'u2', loginId: 'ptw', name: '박태원', email: 'ptw0517@switchkorea.com', role: 'member', status: 'active',
}

function openResearch(api = new MockSalesApi()) {
  window.history.pushState({}, '', '/research')
  return render(<App api={api} />)
}

it('rejects approval by a team member', async () => {
  const api = new MockSalesApi({ currentUser: member })

  await expect(api.reviewProspectCandidate('prospect-hanul', {
    decision: 'approve',
    expectedVersion: 1,
  })).rejects.toMatchObject({ status: 403 } satisfies Partial<SalesApiError>)
})

it('allows a manager to approve evidence-backed candidate and convert it once', async () => {
  const api = new MockSalesApi({ currentUser: manager })

  const approved = await api.reviewProspectCandidate('prospect-hanul', {
    decision: 'approve',
    expectedVersion: 1,
  })
  expect(approved.status).toBe('approved')

  const converted = await api.convertProspectCandidate('prospect-hanul', approved.version)
  const repeated = await api.convertProspectCandidate('prospect-hanul', converted.version)
  expect(converted.status).toBe('converted')
  expect(converted.networkCompanyId).toMatch(/^nc-research-/)
  expect(repeated.networkCompanyId).toBe(converted.networkCompanyId)

  const networkCompanies = await api.listNetworkCompanies()
  expect(networkCompanies).toContainEqual(expect.objectContaining({
    id: converted.networkCompanyId,
    companyName: 'Hanul Mobility Systems',
    lifecycleType: 'prospect',
  }))
})

it('blocks a duplicate-suspected candidate until the duplicate decision is recorded', async () => {
  const api = new MockSalesApi({ currentUser: manager })
  const candidate = await api.getProspectCandidate('prospect-nordlicht')

  expect(candidate.duplicateMatches).toHaveLength(1)
  await expect(api.reviewProspectCandidate(candidate.id, {
    decision: 'approve',
    expectedVersion: candidate.version,
  })).rejects.toMatchObject({ status: 422 } satisfies Partial<SalesApiError>)
})

it('renders research filters, score, evidence, owner assignment, and partial result warning', async () => {
  openResearch()

  expect(await screen.findByRole('heading', { name: '잠재고객 리서치', level: 1 })).toBeVisible()
  expect(screen.getByRole('link', { name: '잠재고객 리서치' })).toBeVisible()
  expect(await screen.findByLabelText('지역')).toHaveValue('')
  expect(screen.getByRole('option', { name: '유럽' })).toBeInTheDocument()
  expect(screen.getByLabelText('업종')).toHaveValue('')
  expect(await screen.findByRole('heading', { name: 'Hanul Mobility Systems' })).toBeVisible()
  expect(screen.getByText('86점')).toBeVisible()
  expect(screen.getByRole('link', { name: 'Automotive console product overview' })).toHaveAttribute('href', 'https://hanul-mobility.example/products/console')
  expect(screen.getByRole('alert')).toHaveTextContent('일부 공개 페이지')

  fireEvent.change(screen.getByLabelText('후보 담당자'), { target: { value: 'u4' } })
  fireEvent.click(screen.getByRole('button', { name: '담당자 저장' }))
  await waitFor(() => expect(screen.getByLabelText('후보 담당자')).toHaveValue('u4'))
})

it('requires duplicate resolution before the manager approval action becomes available', async () => {
  openResearch()

  fireEvent.click(await screen.findByRole('button', { name: /Nordlicht Hausgeräte GmbH/ }))
  const duplicateReview = await screen.findByRole('region', { name: '중복 후보 비교' })
  expect(within(duplicateReview).getByText('Horizon Controls Europe')).toBeVisible()
  expect(screen.queryByRole('button', { name: '승인' })).not.toBeInTheDocument()

  fireEvent.click(within(duplicateReview).getByRole('button', { name: '별도 신규 후보' }))
  expect(await screen.findByRole('button', { name: '승인' })).toBeVisible()
})

it('hides manager-only research controls from a team member', async () => {
  openResearch(new MockSalesApi({ currentUser: member }))

  expect(await screen.findByRole('heading', { name: '잠재고객 리서치', level: 1 })).toBeVisible()
  expect(screen.queryByRole('button', { name: '조건 편집' })).not.toBeInTheDocument()
  expect(screen.queryByRole('button', { name: '즉시 조사' })).not.toBeInTheDocument()
  expect(screen.queryByRole('button', { name: '승인' })).not.toBeInTheDocument()
})

it('shows an empty inbox without losing the research summary', async () => {
  const api = new MockSalesApi()
  api.listProspectCandidates = async () => []
  openResearch(api)

  expect(await screen.findByText('조건에 맞는 후보가 없습니다.')).toBeVisible()
  expect(screen.getByText('월 API 사용량')).toBeVisible()
})
