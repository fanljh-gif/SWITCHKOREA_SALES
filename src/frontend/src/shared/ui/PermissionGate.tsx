import type { ReactNode } from 'react'

import { useAuth } from '../../app/AuthSession'

export function PermissionGate({
  allow,
  ownerUserId,
  children,
}: {
  allow: 'manager' | 'owner'
  ownerUserId?: string
  children: ReactNode
}) {
  const { currentUser, status } = useAuth()
  if (status !== 'authenticated' || !currentUser) return null

  const allowed = currentUser.role === 'manager'
    || (allow === 'owner' && ownerUserId === currentUser.id)

  return allowed ? <>{children}</> : null
}
