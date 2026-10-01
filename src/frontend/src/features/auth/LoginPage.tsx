import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'

import { useAuth } from '../../app/AuthSession'

export function LoginPage() {
  const auth = useAuth()
  const navigate = useNavigate()
  const [loginId, setLoginId] = useState('')
  const [password, setPassword] = useState('')

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    try {
      const result = await auth.login({ loginId, password })
      navigate(result.passwordChangeRequired ? '/change-password' : '/dashboard')
    } catch {
      // AuthSession exposes a user-safe error message.
    }
  }

  return (
    <section aria-labelledby="login-title">
      <h1 id="login-title">로그인</h1>
      <form onSubmit={submit}>
        <label>아이디<input value={loginId} onChange={(event) => setLoginId(event.target.value)} autoComplete="username" /></label>
        <label>비밀번호<input type="password" value={password} onChange={(event) => setPassword(event.target.value)} autoComplete="current-password" /></label>
        {auth.error && <p role="alert">{auth.error}</p>}
        <button type="submit">로그인</button>
      </form>
    </section>
  )
}
