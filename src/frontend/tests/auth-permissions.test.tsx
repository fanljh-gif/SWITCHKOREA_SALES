import '@testing-library/jest-dom/vitest'
import { render, screen } from '@testing-library/react'

import { AuthSessionProvider } from '../src/app/AuthSession'
import { MockSalesApi } from '../src/shared/api/MockSalesApi'
import type { User } from '../src/shared/api/contracts'
import { PermissionGate } from '../src/shared/ui/PermissionGate'

const member: User = {
  id: 'u2',
  loginId: 'ptw',
  name: '박태원',
  email: 'ptw@switchkorea.com',
  role: 'member',
  status: 'active',
}

const manager: User = {
  id: 'u1',
  loginId: 'jhlee',
  name: '이재학',
  email: 'jhlee@switchkorea.com',
  role: 'manager',
  status: 'active',
}

function renderEditPermission(currentUser: User, ownerUserId: string) {
  render(
    <AuthSessionProvider api={new MockSalesApi({ currentUser })}>
      <p>거래처 A</p>
      <PermissionGate allow="owner" ownerUserId={ownerUserId}>
        <button type="button">수정</button>
      </PermissionGate>
    </AuthSessionProvider>,
  )
}

it('allows members to view all customers but hides edit for another owner', async () => {
  renderEditPermission(member, manager.id)

  expect(screen.getByText('거래처 A')).toBeVisible()
  expect(await screen.findByText('박태원')).toBeVisible()
  expect(screen.queryByRole('button', { name: '수정' })).not.toBeInTheDocument()
})

it('shows edit to the owning member', async () => {
  renderEditPermission(member, member.id)

  expect(await screen.findByRole('button', { name: '수정' })).toBeVisible()
})

it('shows edit to the manager regardless of ownership', async () => {
  renderEditPermission(manager, member.id)

  expect(await screen.findByRole('button', { name: '수정' })).toBeVisible()
})
