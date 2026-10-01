export function ConcurrencyNotice({ kind = 'record' }: { kind?: 'record' | 'price' }) {
  return (
    <p role="alert">
      {kind === 'price' ? '단가 적용기간이 겹칩니다.' : '다른 사용자가 먼저 수정했습니다. 최신 내용을 다시 확인해 주세요.'}
    </p>
  )
}
