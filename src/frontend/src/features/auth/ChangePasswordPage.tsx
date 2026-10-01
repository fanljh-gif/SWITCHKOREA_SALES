import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'

import { useAuth } from '../../app/AuthSession'

export function ChangePasswordPage() {
  const auth = useAuth()
  const navigate = useNavigate()
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [error, setError] = useState<string | null>(null)

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    try {
      await auth.changePassword(currentPassword, newPassword)
      navigate('/dashboard')
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : '비밀번호를 변경하지 못했습니다.')
    }
  }

  return (
    <section aria-labelledby="password-title">
      <h1 id="password-title">최초 비밀번호 변경</h1>
      <form onSubmit={submit}>
        <label>현재 비밀번호<input type="password" value={currentPassword} onChange={(event) => setCurrentPassword(event.target.value)} autoComplete="current-password" /></label>
        <label>새 비밀번호<input type="password" value={newPassword} onChange={(event) => setNewPassword(event.target.value)} autoComplete="new-password" /></label>
        {error && <p role="alert">{error}</p>}
        <button type="submit">비밀번호 변경</button>
      </form>
    </section>
  )
}
