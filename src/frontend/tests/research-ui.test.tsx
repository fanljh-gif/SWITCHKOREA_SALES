import '@testing-library/jest-dom/vitest'

import { SalesApiError } from '../src/shared/api/HttpSalesApi'
import { MockSalesApi } from '../src/shared/api/MockSalesApi'
import type { User } from '../src/shared/api/contracts'

const manager: User = {
  id: 'u1', loginId: 'jhlee', name: '이재학', email: 'jhlee@switchkorea.com', role: 'manager', status: 'active',
}

const member: User = {
  id: 'u2', loginId: 'ptw', name: '박태원', email: 'ptw0517@switchkorea.com', role: 'member', status: 'active',
}

it('rejects approval by a team member', async () => {
  const api = new MockSalesApi({ currentUser: member })

  await expect(api.reviewProspectCandidate('prospect-hanul', {
    decision: 'approve',
    expectedVersion: 1,
  })).rejects.toMatchObject<Partial<SalesApiError>>({ status: 403 })
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
  })).rejects.toMatchObject<Partial<SalesApiError>>({ status: 422 })
})
