import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'

import type { SalesApi } from '../shared/api/SalesApi'
import type { AuthResult, LoginRequest, User } from '../shared/api/contracts'

export interface AuthSessionState {
  currentUser: User | null
  status: 'loading' | 'anonymous' | 'authenticated'
  error: string | null
  login(input: LoginRequest): Promise<AuthResult>
  logout(): Promise<void>
  changePassword(currentPassword: string, newPassword: string): Promise<void>
}

const AuthSessionContext = createContext<AuthSessionState | null>(null)

export function AuthSessionProvider({ api, children }: { api: SalesApi; children: ReactNode }) {
  const [currentUser, setCurrentUser] = useState<User | null>(null)
  const [status, setStatus] = useState<AuthSessionState['status']>('loading')
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let active = true
    void api.getCurrentUser().then((user) => {
      if (!active) return
      setCurrentUser(user)
      setStatus(user ? 'authenticated' : 'anonymous')
    }).catch((cause: unknown) => {
      if (!active) return
      setError(cause instanceof Error ? cause.message : '세션을 확인할 수 없습니다.')
      setStatus('anonymous')
    })
    return () => { active = false }
  }, [api])

  const value = useMemo<AuthSessionState>(() => ({
    currentUser,
    status,
    error,
    async login(input) {
      setError(null)
      try {
        const result = await api.login(input)
        setCurrentUser(result.user)
        setStatus('authenticated')
        return result
      } catch (cause) {
        setError(cause instanceof Error ? cause.message : '로그인에 실패했습니다.')
        throw cause
      }
    },
    async logout() {
      await api.logout()
      setCurrentUser(null)
      setStatus('anonymous')
    },
    async changePassword(currentPassword, newPassword) {
      await api.changePassword(currentPassword, newPassword)
      setCurrentUser((user) => user ? { ...user, passwordChangeRequired: false } : user)
    },
  }), [api, currentUser, error, status])

  return (
    <AuthSessionContext.Provider value={value}>
      {currentUser && <span aria-label="현재 사용자">{currentUser.name}</span>}
      {children}
    </AuthSessionContext.Provider>
  )
}

export function useAuth(): AuthSessionState {
  const session = useContext(AuthSessionContext)
  if (!session) throw new Error('AuthSession provider is missing')
  return session
}
