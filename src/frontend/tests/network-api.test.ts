import { describe, expect, it } from 'vitest'

import { SalesApiError } from '../src/shared/api/HttpSalesApi'
import { MockSalesApi } from '../src/shared/api/MockSalesApi'
import type { NetworkTreeNode } from '../src/shared/api/contracts'

function flatten(nodes: Awaited<ReturnType<MockSalesApi['getNetworkTree']>>): NetworkTreeNode[] {
  return nodes.flatMap((node) => [node, ...flatten(node.children)])
}

describe('MockSalesApi sales network', () => {
  it('lists_one_company_under_two_oem_roots_without_duplicating_identity', async () => {
    const api = new MockSalesApi()

    const samsung = flatten(await api.getNetworkTree({ rootCompanyId: 'nc-samsung' }))
    const lg = flatten(await api.getNetworkTree({ rootCompanyId: 'nc-lg' }))
    const samsungShared = samsung.find((node) => node.company.id === 'nc-shared')
    const lgShared = lg.find((node) => node.company.id === 'nc-shared')

    expect(samsungShared?.company).toBe(lgShared?.company)
    expect(samsungShared?.depth).toBe(1)
    expect(lgShared?.depth).toBe(1)
  })

  it('converts_a_prospect_without_losing_contacts_or_relationships', async () => {
    const api = new MockSalesApi()
    const contactsBefore = await api.listCompanyContacts('nc-prospect')
    const relationshipsBefore = await api.listCompanyRelationships('nc-prospect')

    const converted = await api.convertNetworkCompany('nc-prospect', {
      customerCode: 'CUS-900',
      defaultCurrency: 'KRW',
      expectedVersion: 1,
    })

    expect(converted).toMatchObject({ id: 'nc-prospect', lifecycleType: 'customer', version: 2 })
    expect(converted.customerId).toBeTruthy()
    expect((await api.listCompanyContacts('nc-prospect')).map((row) => row.id)).toEqual(contactsBefore.map((row) => row.id))
    expect((await api.listCompanyRelationships('nc-prospect')).map((row) => row.id)).toEqual(relationshipsBefore.map((row) => row.id))
  })

  it('rejects_member_update_for_another_owners_company', async () => {
    const member = { id: 'u3', loginId: 'mcc', name: '최명철', email: 'mcc@switchkorea.com', role: 'member' as const, status: 'active' as const }
    const api = new MockSalesApi({ currentUser: member })

    await expect(api.updateNetworkCompany('nc-shared', {
      companyName: '공통 협력사 변경',
      country: '대한민국',
      industry: '전자부품',
      lifecycleType: 'customer',
      isOem: false,
      status: 'active',
      expectedVersion: 1,
    })).rejects.toMatchObject({ status: 403 } satisfies Partial<SalesApiError>)
  })

  it('prunes_tree_by_lifecycle_owner_status_and_search_while_preserving_ancestors', async () => {
    const api = new MockSalesApi()
    const nodes = flatten(await api.getNetworkTree({ rootCompanyId: 'nc-samsung', lifecycleType: 'prospect', ownerId: 'u3', status: 'active', search: '새빛' }))

    expect(nodes.map((node) => node.company.id)).toEqual(['nc-samsung', 'nc-shared', 'nc-prospect'])
    expect(nodes.at(-1)?.depth).toBe(2)
  })

  it('blocks_member_classification_changes_and_allows_downstream_relationship_owner', async () => {
    const sharedOwner = { id: 'u2', loginId: 'ptw', name: '박태원', email: 'ptw@switchkorea.com', role: 'member' as const, status: 'active' as const }
    const sharedApi = new MockSalesApi({ currentUser: sharedOwner })
    await expect(sharedApi.updateNetworkCompany('nc-shared', {
      companyName: '대한전자', country: '대한민국', industry: '전자부품', lifecycleType: 'prospect', isOem: true, status: 'active', expectedVersion: 1,
    })).rejects.toMatchObject({ status: 403 } satisfies Partial<SalesApiError>)

    const prospectOwner = { id: 'u3', loginId: 'mcc', name: '최명철', email: 'mcc@switchkorea.com', role: 'member' as const, status: 'active' as const }
    const prospectApi = new MockSalesApi({ currentUser: prospectOwner })
    const updated = await prospectApi.updateNetworkRelationship('nr-shared-prospect', {
      upstreamCompanyId: 'nc-shared', downstreamCompanyId: 'nc-prospect', validFrom: '2026-02-01', status: 'active', notes: '하위 담당자 수정', supplies: [], expectedVersion: 1,
    })
    expect(updated.notes).toBe('하위 담당자 수정')
    await expect(prospectApi.updateNetworkRelationship('nr-shared-prospect', {
      upstreamCompanyId: 'nc-samsung', downstreamCompanyId: 'nc-lg', validFrom: '2026-02-01', status: 'active', supplies: [], expectedVersion: 2,
    })).rejects.toMatchObject({ status: 403 } satisfies Partial<SalesApiError>)
  })

  it('closes_previous_assignment_the_day_before_and_rejects_stale_versions', async () => {
    const api = new MockSalesApi()
    await api.changeCompanyAssignment('nc-shared', { userId: 'u3', effectiveDate: '2026-10-10', expectedVersion: 1 })
    const assignments = await api.listCompanyAssignments('nc-shared')
    expect(assignments.find((row) => !row.isCurrent)?.validTo).toBe('2026-10-09')
    expect(assignments.filter((row) => row.isCurrent)).toHaveLength(1)

    await expect(api.updateNetworkCompany('nc-shared', {
      companyName: '대한전자', country: '대한민국', industry: '전자부품', lifecycleType: 'customer', isOem: false, status: 'active', expectedVersion: 1,
    })).rejects.toMatchObject({ status: 409 } satisfies Partial<SalesApiError>)
  })

  it('preserves_company_classification_during_ordinary_updates', async () => {
    const api = new MockSalesApi()

    await expect(api.updateNetworkCompany('nc-shared', {
      companyName: '대한전자', country: '대한민국', industry: '전자부품', lifecycleType: 'prospect', isOem: false, status: 'active', expectedVersion: 1,
    })).rejects.toMatchObject({ status: 422 } satisfies Partial<SalesApiError>)

    await expect(api.updateNetworkCompany('nc-shared', {
      companyName: '대한전자', country: '대한민국', industry: '전자부품', lifecycleType: 'customer', isOem: true, status: 'active', expectedVersion: 1,
    })).rejects.toMatchObject({ status: 422 } satisfies Partial<SalesApiError>)
  })

  it('rejects_invalid_company_references_and_unlinked_customer_creation', async () => {
    const api = new MockSalesApi()

    await expect(api.updateNetworkRelationship('nr-shared-prospect', {
      upstreamCompanyId: 'nc-missing', downstreamCompanyId: 'nc-prospect', validFrom: '2026-02-01', status: 'active', supplies: [], expectedVersion: 1,
    })).rejects.toMatchObject({ status: 404 } satisfies Partial<SalesApiError>)

    await expect(api.createNetworkCompany({
      companyName: '미연결 거래처', country: '대한민국', industry: '전자부품', lifecycleType: 'customer', isOem: false, status: 'active',
    })).rejects.toMatchObject({ status: 422 } satisfies Partial<SalesApiError>)
  })
})
