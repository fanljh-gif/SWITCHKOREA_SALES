import { useState } from 'react'

import { useSalesApi } from '../../app/App'
import type { ImportValidation } from '../../shared/api/contracts'

const steps = ['파일 선택', '열 연결', '검증', '오류·중복 확인', '최종 반영']

export function ImportWizard() {
  const api = useSalesApi()
  const [file, setFile] = useState<File | null>(null)
  const [validation, setValidation] = useState<ImportValidation | null>(null)
  const [committed, setCommitted] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function validate() {
    if (!file) return
    setError(null)
    try {
      setValidation(await api.validateImport({ kind: 'shipment', file, columnMap: {} }))
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : '파일을 검증하지 못했습니다.')
    }
  }

  async function commit() {
    if (!validation?.canCommit) return
    await api.commitImport(validation.token)
    setCommitted(true)
  }

  return (
    <section>
      <h1>Excel 가져오기</h1>
      <ol aria-label="가져오기 단계">{steps.map((step) => <li key={step}>{step}</li>)}</ol>
      <label>Excel 파일<input type="file" accept=".xlsx" onChange={(event) => setFile(event.target.files?.[0] ?? null)} /></label>
      <fieldset>
        <legend>열 연결</legend>
        <p>출고일, 거래처, 품목, 수량 열을 자동 연결합니다.</p>
      </fieldset>
      <button type="button" disabled={!file} onClick={() => void validate()}>검증 실행</button>
      {error && <p role="alert">{error}</p>}
      {validation && (
        <section aria-label="검증 결과">
          <p>신규 {validation.newCount}건</p>
          <p>중복 {validation.duplicateCount}건</p>
          <p>변경 후보 {validation.changeCandidateCount}건</p>
          <p>오류 {validation.errorCount}건</p>
          <ul>{validation.errors.map((row) => <li key={`${row.row}-${row.field}`}>{row.row}행 · {row.field ?? '전체'} · {row.message}</li>)}</ul>
        </section>
      )}
      <button type="button" disabled={!validation?.canCommit || committed} onClick={() => void commit()}>최종 반영</button>
      {committed && <p role="status">가져오기를 완료했습니다.</p>}
    </section>
  )
}
