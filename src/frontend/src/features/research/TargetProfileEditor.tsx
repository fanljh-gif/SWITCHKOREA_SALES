import type { ResearchTargetProfile } from '../../shared/api/contracts'
import { PermissionGate } from '../../shared/ui/PermissionGate'

export function TargetProfileEditor({ profile }: { profile: ResearchTargetProfile }) {
  return <article className="target-profile"><div className="panel-title"><div><h2>타깃 프로필</h2><p>{profile.name} · 매주 월요일 09:00</p></div><PermissionGate allow="manager"><button type="button">조건 편집</button></PermissionGate></div>
    <dl><div><dt>대상 지역</dt><dd>{profile.regions.join(' · ')}</dd></div><div><dt>대상 업종</dt><dd>{profile.industries.join(' · ')}</dd></div><div><dt>핵심 키워드</dt><dd>{profile.includeKeywords.join(' · ')}</dd></div><div><dt>실행 한도</dt><dd>검색 {profile.maxSearchResults}건 · AI 분석 {profile.maxAiAnalyses}건</dd></div></dl>
  </article>
}
