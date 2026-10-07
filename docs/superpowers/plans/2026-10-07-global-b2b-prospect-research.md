# Global B2B Prospect Research Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 공개 웹 검색, Excel 업로드, 규칙 점수와 AI 요약을 이용해 글로벌 잠재기업을 발굴·검토하고 승인된 후보를 기존 영업망 잠재기업으로 전환하는 모듈을 구축한다.

**Architecture:** 기존 React + ASP.NET Core + PostgreSQL 모듈형 단일 애플리케이션에 `Prospect Research` 영역을 추가한다. 수집·정규화·중복판정·평가·검토·전환을 독립 서비스로 분리하고, 외부 검색·AI 공급자는 교체 가능한 어댑터와 작업 대기열 뒤에 둔다.

**Tech Stack:** HTML/CSS/JavaScript mockup, React 18, TypeScript 5.7, Vitest, ASP.NET Core/.NET 10, ClosedXML, EF Core 10, PostgreSQL, xUnit, Render Docker

**Spec:** `docs/superpowers/specs/2026-10-07-global-b2b-prospect-research-design.md`

## Global Constraints

- 구현 순서는 UI 목업 → Front-end → Back-end → Database이며 각 단계 완료 후 사용자 승인을 받는다.
- 테스트 명령은 실행 직전에 사용자 허락을 받는다.
- 대상 지역 기본값은 한국, 유럽, 미국, 중국, 인도, 베트남, 기타 동남아다.
- 대상 업종 기본값은 전자, 가전, 자동차 전장, 산업제어, IoT, 의료기기다.
- AI는 추천·요약만 제공하며 승인이나 거래처 전환을 자동 실행하지 않는다.
- 출처 URL과 조사일이 없는 AI 결과는 승인할 수 없다.
- 공개 기업·업무 정보만 수집하고 고객 개인정보, 내부 매출, 단가 및 인증정보를 외부 API에 전송하지 않는다.
- 외부 API 키는 서버 비밀 환경변수로만 관리하며 저장소와 데이터베이스에 저장하지 않는다.
- 외부 API 장애는 기존 후보 검토와 다른 영업관리 기능을 중단시키지 않는다.
- 기존 미추적 파일 `deploy/installation-report.md`를 수정하거나 커밋하지 않는다.

## Review Focus

- 같은 기업이 국가별 법인명과 본사명으로 들어올 때 자동 병합하지 않고 `DuplicateSuspected`로 분리되는지 확인한다. Task 5의 중복 테스트가 보장한다.
- 출처가 하나도 없거나 조사일이 누락된 후보를 팀장이 승인하려 할 때 422로 거절되는지 확인한다. Task 7의 승인 테스트가 보장한다.
- 월 API 한도 100%에서 예약 실행은 중지되지만 기존 후보 검토와 팀장 수동 실행은 가능한지 확인한다. Task 8의 예산 테스트가 보장한다.
- 검색 공급자가 일부 결과를 반환한 뒤 실패해도 정상 후보가 보존되고 실패 항목만 재처리되는지 확인한다. Task 6의 부분 실패 테스트가 보장한다.
- 동일 Excel 파일이나 동일 공급자 결과를 반복 반영해도 후보가 중복 생성되지 않는지 확인한다. Task 6과 Task 10의 멱등 테스트가 보장한다.

---

## File Structure

### UI mockup

- `src/mockup/index.html`: 잠재고객 리서치 화면 구조와 메뉴
- `src/mockup/styles.css`: 현황 카드, 후보 표, 상세 패널, 실행 및 중복 비교 스타일
- `src/mockup/mock-data.js`: 프로필, 실행, 후보, 근거, 평가 가상 데이터
- `src/mockup/app.js`: 필터, 상태 변경, 담당자 배정, 실행 및 전환 상호작용
- `src/mockup/research-mockup-check.mjs`: 필수 화면·흐름 정적 검사

### Front-end

- `src/frontend/src/features/research/ResearchPage.tsx`: 화면 상태와 하위 구성요소 조합
- `src/frontend/src/features/research/ResearchDashboard.tsx`: KPI, 사용량, 최근 실행
- `src/frontend/src/features/research/TargetProfileEditor.tsx`: 타깃 조건과 실행 한도
- `src/frontend/src/features/research/CandidateInbox.tsx`: 후보 필터·목록·일괄 작업
- `src/frontend/src/features/research/CandidateDetail.tsx`: 점수, AI 요약, 근거, 활동, 승인
- `src/frontend/src/features/research/DuplicateReview.tsx`: 중복 후보 비교와 판정
- `src/frontend/src/features/research/ResearchRunPanel.tsx`: 수동·예약 실행과 이력
- `src/frontend/src/features/research/ProspectImportPanel.tsx`: Excel 검증·반영 흐름
- `src/frontend/src/features/research/research.css`: 리서치 전용 반응형 스타일
- `src/frontend/src/shared/api/contracts.ts`: 리서치 DTO와 상태 타입
- `src/frontend/src/shared/api/SalesApi.ts`: 리서치 API 인터페이스
- `src/frontend/src/shared/api/MockSalesApi.ts`: 단계 승인용 메모리 구현
- `src/frontend/src/shared/api/HttpSalesApi.ts`: HTTP 요청 매핑
- `src/frontend/src/app/App.tsx`: `/research` 메뉴와 라우트
- `src/frontend/tests/research-ui.test.tsx`: 역할·상태별 UI 흐름
- `src/frontend/tests/research-http-api.test.ts`: URL·메서드·JSON 계약

### Back-end core and API

- `src/backend/SalesManagement.Core/Models/ResearchEnums.cs`: 실행·후보·중복 상태 열거형
- `src/backend/SalesManagement.Core/Models/ResearchTargetProfile.cs`: 타깃 조건과 한도
- `src/backend/SalesManagement.Core/Models/ResearchRun.cs`: 실행 상태·건수·사용량
- `src/backend/SalesManagement.Core/Models/ProspectCandidate.cs`: 정규화된 후보와 검토 상태
- `src/backend/SalesManagement.Core/Models/ResearchEvidence.cs`: 출처와 조사일
- `src/backend/SalesManagement.Core/Models/ProspectAssessment.cs`: 점수와 AI 평가
- `src/backend/SalesManagement.Core/Models/ProspectActivity.cs`: 변경·승인·전환 이력
- `src/backend/SalesManagement.Core/Research/ResearchContracts.cs`: 명령·조회·공급자 결과 계약
- `src/backend/SalesManagement.Core/Research/ProspectScoringService.cs`: 규칙 점수 계산
- `src/backend/SalesManagement.Core/Research/DuplicateDetectionService.cs`: 중복 후보 산정
- `src/backend/SalesManagement.Core/Research/ResearchRunService.cs`: 작업 생성·처리·재처리
- `src/backend/SalesManagement.Core/Research/ProspectReviewService.cs`: 담당·상태·승인·전환
- `src/backend/SalesManagement.Core/Research/UsageBudgetService.cs`: 월 사용량과 실행 허용 판정
- `src/backend/SalesManagement.Core/Research/ProspectExcelService.cs`: Excel 검증과 후보 입력
- `src/backend/SalesManagement.Core/Repositories/IResearchRepositories.cs`: 저장소 계약
- `src/backend/SalesManagement.Core/Research/IProspectResearchProvider.cs`: 검색·AI 공급자 계약
- `src/backend/SalesManagement.InMemory/Repositories/InMemoryResearchRepositories.cs`: Front-end/Back-end 단계 저장소
- `src/backend/SalesManagement.Api/Endpoints/ResearchEndpoints.cs`: `/api/research` 계약
- `src/backend/SalesManagement.Api/Workers/ResearchWorker.cs`: 대기 실행 처리
- `src/backend/SalesManagement.Api/Workers/WeeklyResearchWorker.cs`: Asia/Seoul 주간 예약
- `src/backend/SalesManagement.Api/Research/OpenAiProspectResearchProvider.cs`: 외부 검색·AI 어댑터
- `src/backend/SalesManagement.Api/Research/ResearchProviderOptions.cs`: 환경변수·한도 설정
- `src/backend/SalesManagement.Api/Program.cs`: DI, worker, endpoint 등록
- `src/backend/SalesManagement.Tests/ProspectResearchTests.cs`: 점수·중복·상태·예산 단위 테스트
- `src/backend/SalesManagement.Tests/ResearchContractTests.cs`: API·직렬화 계약 테스트
- `src/backend/SalesManagement.Tests/ResearchAuthorizationTests.cs`: 팀장·팀원 권한 테스트
- `src/backend/SalesManagement.Tests/ResearchProviderTests.cs`: 공급자 최소전송·오류 테스트

### PostgreSQL and deployment

- `src/backend/SalesManagement.Postgres/Configurations/ResearchConfiguration.cs`: 테이블·인덱스·관계·동시성
- `src/backend/SalesManagement.Postgres/Repositories/ResearchRepositories.cs`: EF 저장소
- `src/backend/SalesManagement.Postgres/SalesDbContext.cs`: Research DbSet
- `src/backend/SalesManagement.Postgres/PostgresServiceExtensions.cs`: 저장소 DI
- `src/backend/SalesManagement.Postgres/Migrations/<timestamp>_AddProspectResearch.cs`: 스키마 마이그레이션
- `src/backend/SalesManagement.DbTests/ProspectResearchDatabaseTests.cs`: 제약·멱등·전환 통합 테스트
- `src/backend/SalesManagement.Api/appsettings.json`: 비밀값이 없는 기본 옵션
- `render.yaml`: worker/웹 서비스 환경변수 선언
- `Dockerfile`: 전체 테스트·빌드 유지
- `deploy/runbook.md`: 공급자 키, 한도, 예약, 장애 및 롤백 절차

---

### Task 1: Build the research UI mockup

**Files:**
- Modify: `src/mockup/index.html`
- Modify: `src/mockup/styles.css`
- Modify: `src/mockup/mock-data.js`
- Modify: `src/mockup/app.js`
- Create: `src/mockup/research-mockup-check.mjs`

**Interfaces:**
- Consumes: 설계 명세 5~6절의 화면과 상태
- Produces: `/research` 목업, `window.researchMockData`, `data-research-action` 상호작용 표식

- [ ] **Step 1: Write the failing static check**

`research-mockup-check.mjs`가 메뉴, KPI 5종, 타깃 프로필, 후보 검토함, 상세 근거, 중복 비교, 수동 실행, 주간 예약, Excel 가져오기 및 상태 8종을 검사하게 한다.

- [ ] **Step 2: Ask for test permission and run the check to verify RED**

Run: `node src/mockup/research-mockup-check.mjs`

Expected: FAIL with missing research mockup markers.

- [ ] **Step 3: Add representative mock data**

한국·미국·독일·중국·인도·베트남 후보를 포함하고 우선검토, 일반, 낮음, 중복의심, 제외 상태와 출처·조사일을 제공한다.

- [ ] **Step 4: Implement the mockup workflow**

검색조건 편집, 후보 필터, 상세 선택, 담당자 배정, 보류·제외·승인, 중복 비교, 즉시 실행, 예약 편집, Excel 미리보기가 새로고침 없이 동작하게 한다.

- [ ] **Step 5: Ask for test permission and run the static check to verify GREEN**

Run: `node src/mockup/research-mockup-check.mjs`

Expected: `PASS research mockup checks`.

- [ ] **Step 6: Commit the UI mockup**

```powershell
git add src/mockup
git commit -m "feat: add prospect research UI mockup"
```

- [ ] **Step 7: STOP for UI mockup approval**

브라우저로 목업을 보여주고 사용자 승인 전 Front-end를 수정하지 않는다.

### Task 2: Define Front-end research contracts and Mock API

**Files:**
- Modify: `src/frontend/src/shared/api/contracts.ts`
- Modify: `src/frontend/src/shared/api/SalesApi.ts`
- Modify: `src/frontend/src/shared/api/MockSalesApi.ts`
- Create: `src/frontend/tests/research-ui.test.tsx`

**Interfaces:**
- Consumes: Task 1의 상태·필드 명칭
- Produces: `ResearchTargetProfile`, `ResearchRun`, `ProspectCandidate`, `ResearchEvidence`, `ProspectScore`, `AiAssessment`, `DuplicateMatch`, `ResearchSummary`; `SalesApi.listProspectCandidates`, `getProspectCandidate`, `saveResearchTargetProfile`, `startResearchRun`, `assignProspectCandidate`, `reviewProspectCandidate`, `convertProspectCandidate`

- [ ] **Step 1: Write failing contract and Mock API tests**

팀원에게 승인 버튼이 없고, 팀장은 근거가 있는 후보를 승인하며, `DuplicateSuspected` 후보는 중복 판정 전 승인할 수 없다는 테스트를 작성한다.

- [ ] **Step 2: Ask for test permission and verify RED**

Run: `& 'C:\Users\14Z980\.cache\codex-runtimes\codex-primary-runtime\dependencies\bin\fallback\pnpm.cmd' --dir src/frontend test -- research-ui.test.tsx`

Expected: FAIL because research contracts and API methods do not exist.

- [ ] **Step 3: Add exact TypeScript contracts**

상태 값은 `collected | enriching | reviewPending | duplicateSuspected | onHold | excluded | approved | converted`, 실행 방식은 `manual | scheduled | excel`, 우선도는 `high | normal | low`로 고정한다.

- [ ] **Step 4: Implement Mock API state transitions**

목업과 동일한 샘플을 사용하고 승인 시 `networkCompanyId`를 생성하여 기존 `NetworkCompany` 목록에도 잠재기업을 추가한다.

- [ ] **Step 5: Ask for test permission and verify GREEN**

Run: `& 'C:\Users\14Z980\.cache\codex-runtimes\codex-primary-runtime\dependencies\bin\fallback\pnpm.cmd' --dir src/frontend test -- research-ui.test.tsx`

Expected: all research contract tests pass.

- [ ] **Step 6: Commit contracts and Mock API**

```powershell
git add src/frontend/src/shared/api src/frontend/tests/research-ui.test.tsx
git commit -m "feat: add prospect research frontend contracts"
```

### Task 3: Implement the React research workspace

**Files:**
- Create: `src/frontend/src/features/research/ResearchPage.tsx`
- Create: `src/frontend/src/features/research/ResearchDashboard.tsx`
- Create: `src/frontend/src/features/research/TargetProfileEditor.tsx`
- Create: `src/frontend/src/features/research/CandidateInbox.tsx`
- Create: `src/frontend/src/features/research/CandidateDetail.tsx`
- Create: `src/frontend/src/features/research/DuplicateReview.tsx`
- Create: `src/frontend/src/features/research/ResearchRunPanel.tsx`
- Create: `src/frontend/src/features/research/ProspectImportPanel.tsx`
- Create: `src/frontend/src/features/research/research.css`
- Modify: `src/frontend/src/app/App.tsx`
- Modify: `src/frontend/tests/research-ui.test.tsx`

**Interfaces:**
- Consumes: Task 2의 `SalesApi` 메서드와 DTO
- Produces: 인증된 `/research` 라우트와 역할·상태별 화면

- [ ] **Step 1: Add failing interaction tests**

지역·업종 필터, 상세 근거 링크, 점수 내역, 담당자 배정, 중복 판정, 팀장 승인, 오류·빈 결과·부분 성공 표시를 테스트한다.

- [ ] **Step 2: Ask for test permission and verify RED**

Run: `& 'C:\Users\14Z980\.cache\codex-runtimes\codex-primary-runtime\dependencies\bin\fallback\pnpm.cmd' --dir src/frontend test -- research-ui.test.tsx`

Expected: FAIL because research components and route do not exist.

- [ ] **Step 3: Implement page composition and navigation**

목업의 정보 구조를 유지하고 팀장 전용 설정·승인 제어에는 `PermissionGate`와 Back-end 권한 오류 표시를 함께 사용한다.

- [ ] **Step 4: Implement loading, empty, partial-error and conflict states**

후보 목록과 상세 요청을 분리하고 오래된 응답이 새 선택을 덮어쓰지 않도록 요청 세대 또는 취소 처리를 적용한다.

- [ ] **Step 5: Ask for test permission and verify GREEN plus build**

Run: `& 'C:\Users\14Z980\.cache\codex-runtimes\codex-primary-runtime\dependencies\bin\fallback\pnpm.cmd' --dir src/frontend test -- research-ui.test.tsx`

Run: `& 'C:\Users\14Z980\.cache\codex-runtimes\codex-primary-runtime\dependencies\bin\fallback\pnpm.cmd' --dir src/frontend build`

Expected: tests pass and Vite build completes without TypeScript errors.

- [ ] **Step 6: Commit the React workspace**

```powershell
git add src/frontend
git commit -m "feat: add prospect research frontend"
```

- [ ] **Step 7: STOP for Front-end approval**

Mock API 기반 React 화면을 브라우저로 보여주고 승인 전 Back-end를 수정하지 않는다.

### Task 4: Add scoring, normalization, and domain models

**Files:**
- Create: `src/backend/SalesManagement.Core/Models/ResearchEnums.cs`
- Create: `src/backend/SalesManagement.Core/Models/ResearchTargetProfile.cs`
- Create: `src/backend/SalesManagement.Core/Models/ResearchRun.cs`
- Create: `src/backend/SalesManagement.Core/Models/ProspectCandidate.cs`
- Create: `src/backend/SalesManagement.Core/Models/ResearchEvidence.cs`
- Create: `src/backend/SalesManagement.Core/Models/ProspectAssessment.cs`
- Create: `src/backend/SalesManagement.Core/Models/ProspectActivity.cs`
- Create: `src/backend/SalesManagement.Core/Research/ResearchContracts.cs`
- Create: `src/backend/SalesManagement.Core/Research/ProspectScoringService.cs`
- Create: `src/backend/SalesManagement.Tests/ProspectResearchTests.cs`

**Interfaces:**
- Consumes: 설계 명세 8~9절
- Produces: `ProspectScoringService.Score(ProspectScoringInput input, string ruleVersion) -> ProspectScoreResult`; Research enums and commands used by Tasks 5~10

- [ ] **Step 1: Write failing scoring tests**

각 항목 최대 배점 30/20/20/10/10/10, 총점 70 이상 high, 50~69 normal, 49 이하 low, 규칙 버전 보존, 범위 밖 입력 거절을 검증한다.

- [ ] **Step 2: Ask for test permission and verify RED**

Run: `& 'C:\Users\14Z980\Documents\ChatGPT\영업관리 프로세스 구축\.tools\dotnet\dotnet.exe' test src/backend/SalesManagement.Tests/SalesManagement.Tests.csproj -c Release --filter ProspectResearchTests`

Expected: build/test failure because research types do not exist.

- [ ] **Step 3: Implement models and scoring service**

모든 수정 가능한 엔터티는 `EntityBase`를 상속하고, 점수 결과는 각 항목·총점·우선도·규칙 버전을 불변 값으로 반환한다.

- [ ] **Step 4: Ask for test permission and verify GREEN**

Run the Task 4 test command.

Expected: all scoring and model tests pass.

- [ ] **Step 5: Commit domain scoring**

```powershell
git add src/backend/SalesManagement.Core src/backend/SalesManagement.Tests/ProspectResearchTests.cs
git commit -m "feat: add prospect research domain scoring"
```

### Task 5: Add duplicate detection and review rules

**Files:**
- Create: `src/backend/SalesManagement.Core/Repositories/IResearchRepositories.cs`
- Create: `src/backend/SalesManagement.Core/Research/DuplicateDetectionService.cs`
- Create: `src/backend/SalesManagement.Core/Research/ProspectReviewService.cs`
- Create: `src/backend/SalesManagement.InMemory/Repositories/InMemoryResearchRepositories.cs`
- Modify: `src/backend/SalesManagement.InMemory/Repositories/InMemoryRepositories.cs`
- Modify: `src/backend/SalesManagement.Tests/ProspectResearchTests.cs`
- Create: `src/backend/SalesManagement.Tests/ResearchAuthorizationTests.cs`

**Interfaces:**
- Consumes: Task 4 models and `NetworkCompanyService`
- Produces: `DuplicateDetectionService.FindMatchesAsync(ProspectCandidateDraft, CancellationToken) -> IReadOnlyList<DuplicateMatch>`; `ProspectReviewService.AssignAsync`, `ReviewAsync`, `ApproveAndConvertAsync`

- [ ] **Step 1: Write failing duplicate and authorization tests**

루트 도메인 일치, 국가+정규화 회사명 일치, 본사·법인 유사명은 `DuplicateSuspected`, 자동 병합 금지, 팀원의 타 담당 후보 변경 금지, 팀장만 승인 가능을 검증한다.

- [ ] **Step 2: Ask for test permission and verify RED**

Run: `& 'C:\Users\14Z980\Documents\ChatGPT\영업관리 프로세스 구축\.tools\dotnet\dotnet.exe' test src/backend/SalesManagement.Tests/SalesManagement.Tests.csproj -c Release --filter "ProspectResearchTests|ResearchAuthorizationTests"`

Expected: FAIL because duplicate and review services do not exist.

- [ ] **Step 3: Implement normalized matching and review transitions**

IDN을 ASCII 루트 도메인으로 정규화하고 회사명은 대소문자·법인 접미사·공백을 제거하되 유사명은 확정 중복이 아닌 검토 대상으로만 반환한다.

- [ ] **Step 4: Implement conversion through existing network service**

승인 후보를 `LifecycleType.Prospect`로 만들고 `ProspectConversion`에 생성된 `NetworkCompany.Id`를 기록한다. 동일 후보 재호출은 같은 전환 결과를 반환한다.

- [ ] **Step 5: Ask for test permission and verify GREEN**

Run the Task 5 test command.

Expected: all duplicate, transition, conversion, and authorization tests pass.

- [ ] **Step 6: Commit review workflow**

```powershell
git add src/backend
git commit -m "feat: add prospect duplicate and review workflow"
```

### Task 6: Add research runs, provider boundary, and Excel intake

**Files:**
- Create: `src/backend/SalesManagement.Core/Research/IProspectResearchProvider.cs`
- Create: `src/backend/SalesManagement.Core/Research/ResearchRunService.cs`
- Create: `src/backend/SalesManagement.Core/Research/ProspectExcelService.cs`
- Modify: `src/backend/SalesManagement.Core/Repositories/IResearchRepositories.cs`
- Modify: `src/backend/SalesManagement.InMemory/Repositories/InMemoryResearchRepositories.cs`
- Modify: `src/backend/SalesManagement.Tests/ProspectResearchTests.cs`
- Create: `src/backend/SalesManagement.Tests/ResearchProviderTests.cs`

**Interfaces:**
- Consumes: Task 5 duplicate/review services
- Produces: `IProspectResearchProvider.SearchAsync(ResearchQuery, CancellationToken) -> IAsyncEnumerable<ResearchProviderResult>`; `ResearchRunService.StartAsync`, `ProcessAsync`, `RetryFailedAsync`; `ProspectExcelService.Validate(Stream)`, `ImportAsync(Guid validationId, User actor)`

- [ ] **Step 1: Write failing run and provider tests**

수동·예약·Excel 실행, 공급자 부분 실패, 정상 후보 보존, 실패 항목 재처리, 동일 provider result ID와 동일 Excel 해시의 멱등 처리를 검증한다.

- [ ] **Step 2: Ask for test permission and verify RED**

Run: `& 'C:\Users\14Z980\Documents\ChatGPT\영업관리 프로세스 구축\.tools\dotnet\dotnet.exe' test src/backend/SalesManagement.Tests/SalesManagement.Tests.csproj -c Release --filter "ProspectResearchTests|ResearchProviderTests"`

Expected: FAIL because run and provider interfaces do not exist.

- [ ] **Step 3: Implement provider boundary and run orchestration**

공급자 결과마다 source ID, company name, country, domain, summary, evidence URL, observed date를 요구하고 저장 전 정규화·중복판정을 실행한다.

- [ ] **Step 4: Implement Excel validation and import**

ClosedXML로 `CompanyName`, `Country`, `Website` 필수 열과 선택 `Industry`, `Products`, `Notes` 열을 읽고 파일 해시와 행 번호로 멱등 키를 생성한다.

- [ ] **Step 5: Ask for test permission and verify GREEN**

Run the Task 6 test command.

Expected: run, partial-failure, retry, and Excel tests pass.

- [ ] **Step 6: Commit ingestion workflow**

```powershell
git add src/backend
git commit -m "feat: add prospect research ingestion workflow"
```

### Task 7: Expose authenticated research APIs

**Files:**
- Create: `src/backend/SalesManagement.Api/Endpoints/ResearchEndpoints.cs`
- Modify: `src/backend/SalesManagement.Api/Program.cs`
- Create: `src/backend/SalesManagement.Tests/ResearchContractTests.cs`
- Modify: `src/backend/SalesManagement.Tests/ResearchAuthorizationTests.cs`
- Modify: `src/frontend/src/shared/api/HttpSalesApi.ts`
- Create: `src/frontend/tests/research-http-api.test.ts`

**Interfaces:**
- Consumes: Tasks 4~6 services
- Produces: `/api/research/summary`, `/profiles`, `/runs`, `/candidates`, `/candidates/{id}`, `/assign`, `/review`, `/approve`, `/duplicates`, `/imports/validate`, `/imports/{id}/apply`

- [ ] **Step 1: Write failing API and HTTP mapping tests**

메서드, URL, camelCase enum, optimistic concurrency `expectedVersion`, 401/403/409/422 응답을 고정한다. 근거 또는 조사일이 없는 승인 요청은 422여야 한다.

- [ ] **Step 2: Ask for test permission and verify RED**

Run: `& 'C:\Users\14Z980\Documents\ChatGPT\영업관리 프로세스 구축\.tools\dotnet\dotnet.exe' test src/backend/SalesManagement.Tests/SalesManagement.Tests.csproj -c Release --filter "ResearchContractTests|ResearchAuthorizationTests"`

Run: `& 'C:\Users\14Z980\.cache\codex-runtimes\codex-primary-runtime\dependencies\bin\fallback\pnpm.cmd' --dir src/frontend test -- research-http-api.test.ts`

Expected: FAIL because endpoints and HTTP mappings do not exist.

- [ ] **Step 3: Implement endpoints and DI**

기존 인증 principal 매핑과 `NetworkProblemMapper` 패턴을 따르고 파일 업로드 크기·확장자·MIME 검증을 추가한다.

- [ ] **Step 4: Implement Front-end HTTP adapter**

Mock API와 같은 DTO를 반환하도록 enum과 nullable 필드를 정규화한다.

- [ ] **Step 5: Ask for test permission and verify GREEN**

Run both Task 7 commands.

Expected: all API contract, permission, and HTTP mapping tests pass.

- [ ] **Step 6: Commit APIs**

```powershell
git add src/backend src/frontend
git commit -m "feat: expose prospect research APIs"
```

### Task 8: Add usage budgets, worker, and external provider adapter

**Files:**
- Create: `src/backend/SalesManagement.Core/Research/UsageBudgetService.cs`
- Create: `src/backend/SalesManagement.Api/Research/ResearchProviderOptions.cs`
- Create: `src/backend/SalesManagement.Api/Research/OpenAiProspectResearchProvider.cs`
- Create: `src/backend/SalesManagement.Api/Workers/ResearchWorker.cs`
- Create: `src/backend/SalesManagement.Api/Workers/WeeklyResearchWorker.cs`
- Modify: `src/backend/SalesManagement.Api/Program.cs`
- Modify: `src/backend/SalesManagement.Api/appsettings.json`
- Modify: `src/backend/SalesManagement.Tests/ResearchProviderTests.cs`

**Interfaces:**
- Consumes: Task 6 provider/run interfaces
- Produces: `UsageBudgetService.CanStart(RunKind, UsageSnapshot, User) -> BudgetDecision`; hosted worker processing; environment options `Research__Provider`, `Research__ApiKey`, `Research__Model`, `Research__WeeklyDay`, `Research__WeeklyTime`, `Research__MonthlyLimit`

- [ ] **Step 1: Write failing budget and minimal-transmission tests**

70/90% 경고, 100% 예약 중지, 팀장 수동 실행 허용, 팀원 수동 실행 거절, 고객 매출·단가·연락처가 provider payload에 포함되지 않음을 검증한다.

- [ ] **Step 2: Ask for test permission and verify RED**

Run: `& 'C:\Users\14Z980\Documents\ChatGPT\영업관리 프로세스 구축\.tools\dotnet\dotnet.exe' test src/backend/SalesManagement.Tests/SalesManagement.Tests.csproj -c Release --filter ResearchProviderTests`

Expected: FAIL because budget, worker, and adapter do not exist.

- [ ] **Step 3: Implement budget decisions and workers**

worker는 cancellation을 준수하고 실행당 최대 건수를 적용한다. 예약 시각은 `Asia/Seoul`에서 다음 실행을 계산하고 하나의 활성 실행만 생성한다.

- [ ] **Step 4: Implement the external adapter**

공식 공급자 문서를 구현 시점에 확인하고 `HttpClient`로 구조화 JSON을 요청한다. 출처 URL이 없는 결과는 저장 대상에서 제외하며 API 키와 원문 민감정보를 로그에 남기지 않는다.

- [ ] **Step 5: Ask for test permission and verify GREEN**

Run the Task 8 test command.

Expected: budget, scheduling, payload, timeout, partial-result, and redaction tests pass without a live API call.

- [ ] **Step 6: Run full Back-end verification after permission**

Run: `& 'C:\Users\14Z980\Documents\ChatGPT\영업관리 프로세스 구축\.tools\dotnet\dotnet.exe' test src/backend/SalesManagement.sln -c Release`

Expected: all Back-end tests pass.

- [ ] **Step 7: Commit workers and provider**

```powershell
git add src/backend
git commit -m "feat: automate prospect research runs"
```

- [ ] **Step 8: STOP for Back-end approval**

인메모리 저장소와 가짜 공급자로 API 흐름을 시연하고 승인 전 PostgreSQL을 수정하지 않는다.

### Task 9: Persist research data in PostgreSQL

**Files:**
- Create: `src/backend/SalesManagement.Postgres/Configurations/ResearchConfiguration.cs`
- Create: `src/backend/SalesManagement.Postgres/Repositories/ResearchRepositories.cs`
- Modify: `src/backend/SalesManagement.Postgres/SalesDbContext.cs`
- Modify: `src/backend/SalesManagement.Postgres/PostgresServiceExtensions.cs`
- Create: `src/backend/SalesManagement.Postgres/Migrations/<timestamp>_AddProspectResearch.cs`
- Create: `src/backend/SalesManagement.DbTests/ProspectResearchDatabaseTests.cs`

**Interfaces:**
- Consumes: Task 5 repository interfaces and Task 4 entities
- Produces: PostgreSQL repository implementations and `AddProspectResearch` migration

- [ ] **Step 1: Write failing PostgreSQL tests**

필수 FK, cascade 제한, 동시성 버전, profile/run/provider-result uniqueness, evidence URL+observed date, one conversion per candidate, activity immutability, normalized domain indexes를 검증한다.

- [ ] **Step 2: Ask for test permission and verify RED**

Run: `& 'C:\Users\14Z980\Documents\ChatGPT\영업관리 프로세스 구축\.tools\dotnet\dotnet.exe' test src/backend/SalesManagement.DbTests/SalesManagement.DbTests.csproj -c Release --filter ProspectResearchDatabaseTests`

Expected: FAIL because mappings and repositories do not exist.

- [ ] **Step 3: Implement EF mappings and repositories**

후보 데이터는 상태 변경 이력을 별도 테이블에 추가하고 `ResearchEvidence`와 `ProspectActivity`는 물리 삭제 없이 보존한다.

- [ ] **Step 4: Generate and inspect migration**

Run: `& 'C:\Users\14Z980\Documents\ChatGPT\영업관리 프로세스 구축\.tools\dotnet\dotnet.exe' ef migrations add AddProspectResearch --project src/backend/SalesManagement.Postgres --startup-project src/backend/SalesManagement.Api`

Expected: migration contains all research tables, foreign keys, indexes, unique constraints and no unrelated destructive operations.

- [ ] **Step 5: Ask for test permission and verify GREEN**

Run the Task 9 test command.

Expected: all PostgreSQL research tests pass.

- [ ] **Step 6: Commit persistence**

```powershell
git add src/backend
git commit -m "feat: persist prospect research data"
```

### Task 10: Complete deployment, migration, and end-to-end verification

**Files:**
- Modify: `render.yaml`
- Modify: `Dockerfile`
- Modify: `deploy/runbook.md`
- Modify: `deploy/tests/RenderDeployment.Tests.ps1`
- Modify: `src/backend/SalesManagement.DbTests/EndToEndTests.cs`
- Modify: `src/frontend/tests/business-flows.test.tsx`

**Interfaces:**
- Consumes: Tasks 1~9 complete module
- Produces: deployable Render configuration, migration startup, documented secrets and rollback, full workflow proof

- [ ] **Step 1: Write failing deployment and end-to-end tests**

동일 Excel 재업로드 멱등성, 수동 조사 → 근거 → 점수 → 중복 검토 → 팀장 승인 → `NetworkCompany` 전환, 공급자 장애 격리, 환경변수 누락 readiness를 검증한다.

- [ ] **Step 2: Ask for test permission and verify RED**

Run: `& 'C:\Users\14Z980\Documents\ChatGPT\영업관리 프로세스 구축\.tools\dotnet\dotnet.exe' test src/backend/SalesManagement.sln -c Release`

Run: `& 'C:\Users\14Z980\.cache\codex-runtimes\codex-primary-runtime\dependencies\bin\fallback\pnpm.cmd' --dir src/frontend test -- --run`

Run: `pwsh -File deploy/scripts/Test-DeploymentScripts.ps1`

Expected: new deployment assertions fail before configuration and runbook changes.

- [ ] **Step 3: Add deployment configuration and runbook**

비밀값 자체는 추가하지 않고 변수명, 권한, 월 한도, 주간 일정, 키 교체, 공급자 중지, 마이그레이션 롤백 및 장애 시 수동 운영 절차를 문서화한다.

- [ ] **Step 4: Ask for test permission and run complete local verification**

Run: `node src/mockup/research-mockup-check.mjs`

Run: `& 'C:\Users\14Z980\.cache\codex-runtimes\codex-primary-runtime\dependencies\bin\fallback\pnpm.cmd' --dir src/frontend test -- --run`

Run: `& 'C:\Users\14Z980\.cache\codex-runtimes\codex-primary-runtime\dependencies\bin\fallback\pnpm.cmd' --dir src/frontend build`

Run: `& 'C:\Users\14Z980\Documents\ChatGPT\영업관리 프로세스 구축\.tools\dotnet\dotnet.exe' test src/backend/SalesManagement.sln -c Release`

Run: `& 'C:\Users\14Z980\Documents\ChatGPT\영업관리 프로세스 구축\.tools\dotnet\dotnet.exe' build src/backend/SalesManagement.sln -c Release --no-restore`

Run: `pwsh -File deploy/scripts/Test-DeploymentScripts.ps1`

Expected: every command exits 0; no warnings or failed tests.

- [ ] **Step 5: Commit deployment integration**

```powershell
git add render.yaml Dockerfile deploy/runbook.md deploy/tests src/backend/SalesManagement.DbTests src/frontend/tests
git commit -m "feat: deploy prospect research module"
```

- [ ] **Step 6: STOP for Database-stage approval and deployment authorization**

마이그레이션 내용, 백업 가능 여부, 외부 API 예상 비용과 비밀변수 목록을 제시한다. 승인 후에만 Render 환경변수 설정, Git push 및 운영 데이터베이스 마이그레이션을 수행한다.

- [ ] **Step 7: Deploy and verify live behavior**

Render 배포 로그에서 컨테이너 테스트와 마이그레이션을 확인하고 `/health/ready`, 로그인 보호, 팀장·팀원 권한, 후보 검토·전환을 브라우저로 검증한다. 테스트 API 키와 샘플 데이터만 사용한다.

- [ ] **Step 8: Record operational handoff**

실행 주기, 한도, 키 교체, 장애 재처리, 후보 삭제 대신 제외 처리, 데이터 백업·복원 및 사내 서버 이전 절차를 최종 인수 문서에 기록한다.

---

## Execution Notes

- 권장 실행 방식은 **Native**다. 도메인 계약, Mock API, Back-end 서비스, EF 스키마가 순차적으로 강하게 의존하므로 동일 세션에서 인터페이스 일관성을 유지하는 편이 효율적이다.
- 각 Task는 RED → 최소 구현 → GREEN → 커밋 순서로 수행한다.
- UI 목업, Front-end, Back-end, Database 승인 게이트에서 반드시 멈춘다.
- 테스트 명령은 계획에 정의되어 있어도 사용자 허락을 받은 뒤 실행한다.
- 구현 중 외부 API의 정확한 요청·응답 계약은 공식 공급자 문서로 확인하고 테스트 더블로 고정한 뒤 라이브 키를 사용한다.
