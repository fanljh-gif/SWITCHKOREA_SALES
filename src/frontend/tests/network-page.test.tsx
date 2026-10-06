import '@testing-library/jest-dom/vitest'
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'

import { App } from '../src/app/App'
import { MockSalesApi } from '../src/shared/api/MockSalesApi'
import type { User } from '../src/shared/api/contracts'

const member: User = {
  id: 'u3', loginId: 'mcc', name: '최명철', email: 'cmc@switchkorea.com', role: 'member', status: 'active',
}

function openNetwork(api = new MockSalesApi()) {
  window.history.pushState({}, '', '/network')
  return render(<App api={api} />)
}

it('renders the network route, repeated identity, selection, and five detail tabs', async () => {
  const view = openNetwork()

  expect(await screen.findByRole('heading', { name: '영업망 관리' })).toBeVisible()
  expect(screen.getByRole('link', { name: '영업망 관리' })).toBeVisible()
  expect(await screen.findByRole('button', { name: /^대한전자 정식 거래처/ })).toBeVisible()
  await waitFor(() => expect(window.location.search).toContain('company=nc-samsung'))
  await waitFor(() => expect(view.container.querySelector('[data-company-id="nc-shared"]')).toBeInTheDocument())

  fireEvent.change(screen.getByLabelText('원청 선택'), { target: { value: 'nc-lg' } })
  await waitFor(() => expect(window.location.search).toContain('root=nc-lg'))
  await waitFor(() => expect(window.location.search).toContain('company=nc-lg'))
  expect(await screen.findByRole('heading', { name: 'LG전자' })).toBeVisible()
  expect(view.container.querySelector('[data-company-id="nc-shared"]')).toBeInTheDocument()

  fireEvent.click(screen.getByRole('button', { name: /^새빛정밀 잠재 협력사/ }))
  expect(await screen.findByRole('heading', { name: '새빛정밀' })).toBeVisible()
  expect(window.location.search).toContain('root=nc-lg')
  expect(window.location.search).toContain('company=nc-prospect')
  const tabs = screen.getByRole('tablist', { name: '회사 상세 메뉴' })
  for (const label of ['기본정보', '관계·납품', '거래처 담당자', '당사 담당자 이력', '변경 이력']) {
    expect(within(tabs).getByRole('tab', { name: label })).toBeVisible()
  }
  expect(screen.getByRole('button', { name: '정식 거래처 전환' })).toBeVisible()
  expect(screen.getByRole('button', { name: '당사 담당자 변경' })).toBeVisible()

  fireEvent.click(within(tabs).getByRole('tab', { name: '관계·납품' }))
  expect(await screen.findByLabelText('선택 회사 역할')).toBeVisible()
  expect(screen.getByRole('option', { name: '하위 회사' })).toBeInTheDocument()
  fireEvent.click(within(tabs).getByRole('tab', { name: '거래처 담당자' }))
  expect((await screen.findAllByRole('button', { name: '수정' })).length).toBeGreaterThan(0)
  fireEvent.click(within(tabs).getByRole('tab', { name: '변경 이력' }))
  expect(await screen.findByRole('columnheader', { name: '작업자' })).toBeVisible()
})

it('locks member company registration to a prospect owned by the current user', async () => {
  openNetwork(new MockSalesApi({ currentUser: member }))
  await waitFor(() => expect(window.location.search).toContain('company=nc-samsung'))
  await waitFor(() => expect(screen.queryByRole('status')).not.toBeInTheDocument())
  fireEvent.click(await screen.findByRole('button', { name: '회사 등록' }))

  expect(screen.getByRole('form', { name: '영업망 회사 등록' })).toBeVisible()
  expect(screen.getByText('잠재 협력사로 등록되며 현재 사용자가 담당자가 됩니다.')).toBeVisible()
  expect(screen.queryByLabelText('등록 구분')).not.toBeInTheDocument()
})

it('hides edit actions from a member viewing another owners company', async () => {
  openNetwork(new MockSalesApi({ currentUser: member }))
  await waitFor(() => expect(window.location.search).toContain('company=nc-samsung'))
  await waitFor(() => expect(screen.queryByRole('status')).not.toBeInTheDocument())

  fireEvent.click(await screen.findByRole('button', { name: /^대한전자 정식 거래처/ }))
  await waitFor(() => expect(window.location.search).toContain('company=nc-shared'))
  expect(await screen.findByRole('heading', { name: '대한전자' })).toBeVisible()
  expect(screen.queryByRole('button', { name: '회사 수정' })).not.toBeInTheDocument()
  expect(screen.queryByRole('button', { name: '관계 연결' })).not.toBeInTheDocument()
  expect(screen.queryByRole('button', { name: '당사 담당자 변경' })).not.toBeInTheDocument()
})
