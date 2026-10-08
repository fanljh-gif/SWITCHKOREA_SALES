# B2B Quotation and Customer Inquiry Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 고객문의 접수부터 담당자 대응, 견적 작성·개정, 팀장 승인, 국문·영문 PDF 발행, 발송 기록 및 수주·실주·보류까지 관리하는 독립 B2B 견적·고객문의 모듈을 구축한다.

**Architecture:** 기존 React + ASP.NET Core + PostgreSQL 모듈형 단일 애플리케이션에 `Inquiry & Quotation` 영역을 추가한다. UI는 현황·고객문의·견적서의 통합 작업공간으로 구성하고, 도메인은 문의 흐름과 불변 견적 개정본을 분리하며, PDF·첨부파일은 인터페이스 뒤에 격리한다.

**Tech Stack:** HTML/CSS/JavaScript mockup, React 18, TypeScript 5.7, Vitest, ASP.NET Core/.NET 10, PDFsharp-MigraDoc 6.2.4, EF Core 10, PostgreSQL 18, xUnit, Render Docker

**Spec:** `docs/superpowers/specs/2026-10-08-b2b-quotation-inquiry-design.md`

## Global Constraints

- 구현 순서는 UI 목업 → Front-end → Back-end → Database이며 각 단계 완료 후 사용자 승인을 받는다.
- 테스트 명령은 실행 직전에 사용자에게 알리고 허용된 범위에서 진행한다.
- 신규 메뉴 `견적·고객문의`는 `출고·매출 관리` 다음, `환율·목표 관리` 앞에 둔다.
- 견적번호는 `SWK-YYYYMM-NNN`, 최초 개정은 `Rev.0`이다.
- 통화는 KRW, USD, CNY만 허용하며 한 견적에서 혼합하지 않는다.
- 모든 견적은 팀장 승인 후에만 PDF 생성과 발송 완료 처리가 가능하다.
- 수출 견적은 Incoterms 코드와 지정 장소·항구를 필수로 가진다.
- 수주·실주·보류는 출고·매출과 연결하지 않는다.
- 팀원은 본인 담당 건만, 팀장은 전체 건을 조회한다.
- 이메일 자동수집·자동발송, 전자서명, 고객 포털은 구현하지 않는다.
- 기존 미추적 파일 `deploy/installation-report.md`를 수정하거나 커밋하지 않는다.

## Review Focus

- 월 경계와 동시 요청에서 `SWK-YYYYMM-NNN`이 중복되거나 이전 달 순번을 이어가지 않아야 한다. Task 6과 Task 8의 번호발급 테스트가 보장한다.
- 국내 견적으로 전환한 뒤 남아 있는 Incoterms 값이 저장되지 않아야 하고, 수출 견적은 코드와 장소가 모두 없으면 승인 요청이 거절되어야 한다. Task 4와 Task 6의 검증 테스트가 보장한다.
- 승인된 개정본은 직접 수정할 수 없고 새 개정본이 직전 스냅샷을 정확히 복사해야 한다. Task 6의 불변성 테스트가 보장한다.
- 부가세 포함·별도·면세와 할인·운송비·기타 비용의 반올림 결과가 KRW·USD·CNY 각각에서 일관되어야 한다. Task 4의 계산 테스트가 보장한다.
- 확장자 위조, MIME 불일치, 10MB 초과 및 경로 문자가 포함된 첨부파일은 저장 전에 거절되어야 한다. Task 7과 Task 8의 첨부파일 테스트가 보장한다.

---

## File Structure

### UI mockup

- `src/mockup/index.html`: 신규 메뉴와 견적·고객문의 작업공간 구조
- `src/mockup/styles.css`: 현황 카드, 문의 목록, 견적 편집기, 승인 패널 및 반응형 스타일
- `src/mockup/mock-data.js`: 문의, 활동, 견적 개정, Incoterms 및 대시보드 샘플
- `src/mockup/app.js`: 탭, 필터, 상태 전이, 계산, 승인·반려 및 PDF 미리보기 상호작용
- `src/mockup/quotation-inquiry-mockup-check.mjs`: 필수 화면·상태·메뉴 순서 정적 검사

### Front-end

- `src/frontend/src/features/quotes/QuotationInquiryPage.tsx`: 탭·필터·선택 상태와 하위 화면 조합
- `src/frontend/src/features/quotes/QuoteDashboard.tsx`: KPI와 우선 업무 목록
- `src/frontend/src/features/quotes/InquiryList.tsx`: 문의 검색·필터·목록
- `src/frontend/src/features/quotes/InquiryDetail.tsx`: 문의 상세, 활동, 첨부파일 및 연결 견적
- `src/frontend/src/features/quotes/QuoteList.tsx`: 견적 필터·상태·통화 목록
- `src/frontend/src/features/quotes/QuoteEditor.tsx`: 기본정보, 품목, 세금·비용 및 수출조건 편집
- `src/frontend/src/features/quotes/QuoteApprovalPanel.tsx`: 승인요청, 승인·반려, PDF·발송·결과 동작
- `src/frontend/src/features/quotes/quoteCalculations.ts`: 브라우저 표시용 순수 금액 계산
- `src/frontend/src/features/quotes/quotes.css`: 모듈 전용 반응형 스타일
- `src/frontend/src/shared/api/contracts.ts`: 문의·견적 DTO와 상태 타입
- `src/frontend/src/shared/api/SalesApi.ts`: 문의·견적 API 인터페이스
- `src/frontend/src/shared/api/MockSalesApi.ts`: Front-end 단계용 인메모리 흐름
- `src/frontend/src/shared/api/HttpSalesApi.ts`: `/api/inquiries`, `/api/quotes`, Incoterms HTTP 매핑
- `src/frontend/src/app/App.tsx`: 메뉴와 `/quotes` 라우트
- `src/frontend/src/features/admin/AdminPage.tsx`: Incoterms 활성·비활성 및 표시순서 관리
- `src/frontend/tests/quotation-inquiry-ui.test.tsx`: 역할·상태·계산 UI 테스트
- `src/frontend/tests/quotation-inquiry-http-api.test.ts`: URL·메서드·본문 계약 테스트
- `src/frontend/tests/app-routing.test.tsx`: 정확한 메뉴 순서 테스트

### Back-end core and API

- `src/backend/SalesManagement.Core/Models/QuotationEnums.cs`: 문의·견적·세금·언어·거래 상태
- `src/backend/SalesManagement.Core/Models/CustomerInquiry.cs`: 문의 기본정보와 담당자·기한
- `src/backend/SalesManagement.Core/Models/InquiryActivity.cs`: 추가 전용 문의 활동
- `src/backend/SalesManagement.Core/Models/InquiryAttachment.cs`: 첨부파일 내용과 메타데이터
- `src/backend/SalesManagement.Core/Models/Quote.cs`: 견적 식별자와 연결 문의
- `src/backend/SalesManagement.Core/Models/QuoteRevision.cs`: 불변 승인 스냅샷과 합계
- `src/backend/SalesManagement.Core/Models/QuoteLine.cs`: 품목별 스냅샷과 계산값
- `src/backend/SalesManagement.Core/Models/QuoteApproval.cs`: 요청·승인·반려 이력
- `src/backend/SalesManagement.Core/Models/QuoteDispatch.cs`: PDF 해시와 발송 기록
- `src/backend/SalesManagement.Core/Models/Incoterm.cs`: 관리 가능한 수출조건
- `src/backend/SalesManagement.Core/Quotations/QuotationContracts.cs`: 명령·조회·PDF 계약
- `src/backend/SalesManagement.Core/Quotations/QuoteCalculator.cs`: 금액·세액·반올림 계산
- `src/backend/SalesManagement.Core/Quotations/InquiryService.cs`: 문의 소유권·상태·활동 규칙
- `src/backend/SalesManagement.Core/Quotations/QuoteService.cs`: 견적 생성·개정·승인·결과 규칙
- `src/backend/SalesManagement.Core/Quotations/QuotationDashboardService.cs`: 역할·기간·담당자별 KPI와 우선 업무
- `src/backend/SalesManagement.Core/Quotations/AttachmentValidator.cs`: 크기·형식·시그니처 검증
- `src/backend/SalesManagement.Core/Quotations/IQuotePdfRenderer.cs`: 승인 스냅샷 PDF 계약
- `src/backend/SalesManagement.Core/Repositories/IQuotationRepositories.cs`: 문의·견적·번호·Incoterms 저장소 계약
- `src/backend/SalesManagement.InMemory/Repositories/InMemoryQuotationRepositories.cs`: Back-end 단계용 저장소
- `src/backend/SalesManagement.Api/Endpoints/InquiryEndpoints.cs`: `/api/inquiries` HTTP 계약
- `src/backend/SalesManagement.Api/Endpoints/QuoteEndpoints.cs`: `/api/quotes` HTTP 계약
- `src/backend/SalesManagement.Api/Endpoints/IncotermEndpoints.cs`: `/api/admin/incoterms` HTTP 계약
- `src/backend/SalesManagement.Api/Quotations/MigraDocQuotePdfRenderer.cs`: 국문·영문 PDF 생성
- `src/backend/SalesManagement.Api/Quotations/SystemFontResolver.cs`: Windows·Linux 한글 폰트 해석
- `src/backend/SalesManagement.Api/Program.cs`: 서비스·저장소·엔드포인트 등록
- `src/backend/SalesManagement.Api/SalesManagement.Api.csproj`: `PDFsharp-MigraDoc` 6.2.4 참조
- `src/backend/SalesManagement.Tests/QuoteCalculationTests.cs`: 할인·세금·비용 계산 테스트
- `src/backend/SalesManagement.Tests/InquiryWorkflowTests.cs`: 문의 상태·소유권 테스트
- `src/backend/SalesManagement.Tests/QuoteWorkflowTests.cs`: 번호·개정·승인·결과 테스트
- `src/backend/SalesManagement.Tests/QuotationContractTests.cs`: API·직렬화 계약 테스트
- `src/backend/SalesManagement.Tests/QuotationAuthorizationTests.cs`: 직접 객체 접근 권한 테스트
- `src/backend/SalesManagement.Tests/QuotePdfTests.cs`: PDF 조건·한글·영문 렌더링 테스트

### PostgreSQL and deployment

- `src/backend/SalesManagement.Postgres/Configurations/QuotationConfiguration.cs`: 테이블·정밀도·인덱스·삭제 제한
- `src/backend/SalesManagement.Postgres/Repositories/QuotationRepositories.cs`: EF 저장소와 월별 번호발급
- `src/backend/SalesManagement.Postgres/SalesDbContext.cs`: 신규 DbSet
- `src/backend/SalesManagement.Postgres/PostgresServiceExtensions.cs`: 저장소 DI
- `src/backend/SalesManagement.Postgres/Migrations/<timestamp>_AddQuotationInquiry.cs`: additive-only 스키마
- `src/backend/SalesManagement.DbTests/QuotationInquiryDatabaseTests.cs`: 제약·동시성·파일·왕복 테스트
- `Dockerfile`: Linux 한글 글꼴 `fonts-noto-cjk`와 기존 빌드 검증
- `deploy/runbook.md`: PDF 글꼴, 첨부파일 용량, 백업·복원 및 배포 절차

---

### Task 1: Build the quotation and inquiry UI mockup

**Files:**
- Modify: `src/mockup/index.html`
- Modify: `src/mockup/styles.css`
- Modify: `src/mockup/mock-data.js`
- Modify: `src/mockup/app.js`
- Create: `src/mockup/quotation-inquiry-mockup-check.mjs`

**Interfaces:**
- Consumes: 설계 명세 3~10절의 화면·상태·권한
- Produces: `/quotes` 목업, `window.quotationMockData`, `data-quote-action` 상호작용 표식

- [ ] **Step 1: Write the failing static check**

`quotation-inquiry-mockup-check.mjs`가 정확한 메뉴 순서, 현황·고객문의·견적서 탭, KPI 6종, 문의 상태 6종, 견적 상태 8종, `SWK-202610-001`, `Rev.0`, 국내·수출 조건, 승인·반려, 국문·영문 PDF 및 수주·실주·보류 동작 표식을 검사하게 한다.

- [ ] **Step 2: Ask for test permission and verify RED**

Run: `node src/mockup/quotation-inquiry-mockup-check.mjs`

Expected: FAIL with missing quotation and inquiry markers.

- [ ] **Step 3: Add representative mock data**

팀원 4명의 문의, 정상·기한초과·승인대기·반려·발송·수주·실주·보류 사례, KRW·USD·CNY, 국내·수출 및 최소 2개 개정본을 추가한다.

- [ ] **Step 4: Implement the mockup workflow**

탭 전환, 목록 필터, 문의 상세, 활동 추가, 연결 견적 생성, 품목·할인·세금·비용 계산, 수출조건 표시, 승인요청·승인·반려, PDF 언어 선택, 발송 및 결과 처리를 새로고침 없이 동작시킨다.

- [ ] **Step 5: Ask for test permission and verify GREEN**

Run: `node src/mockup/quotation-inquiry-mockup-check.mjs`

Expected: `PASS quotation inquiry mockup checks`.

- [ ] **Step 6: Verify the mockup in the browser**

팀장과 팀원 상태에서 메뉴, 세 탭, 기한초과, 승인 버튼, 수출조건, 계산 및 반응형 레이아웃을 확인하고 콘솔 오류가 없음을 기록한다.

- [ ] **Step 7: Commit the UI mockup**

```powershell
git add src/mockup
git commit -m "feat: add quotation inquiry UI mockup"
```

- [ ] **Step 8: STOP for UI mockup approval**

사용자 승인 전 Front-end 파일을 수정하지 않는다.

### Task 2: Define Front-end contracts and Mock API

**Files:**
- Modify: `src/frontend/src/shared/api/contracts.ts`
- Modify: `src/frontend/src/shared/api/SalesApi.ts`
- Modify: `src/frontend/src/shared/api/MockSalesApi.ts`
- Create: `src/frontend/tests/quotation-inquiry-ui.test.tsx`

**Interfaces:**
- Consumes: Task 1의 상태·필드·동작 명칭
- Produces: `CustomerInquiryDto`, `InquiryActivityDto`, `InquiryAttachmentDto`, `QuoteDto`, `QuoteRevisionDto`, `QuoteLineDto`, `QuoteApprovalDto`, `QuoteDispatchDto`, `IncotermDto`, `QuoteDashboardDto`; `SalesApi.getQuotationDashboard`, `listInquiries`, `getInquiry`, `saveInquiry`, `assignInquiry`, `changeInquiryStatus`, `addInquiryActivity`, `uploadInquiryAttachment`, `downloadInquiryAttachment`, `listQuotes`, `getQuote`, `createQuote`, `saveQuoteRevision`, `submitQuoteRevision`, `approveQuoteRevision`, `rejectQuoteRevision`, `createQuoteRevision`, `downloadQuotePdf`, `recordQuoteDispatch`, `recordQuoteResult`, `listIncoterms`, `saveIncoterm`

- [ ] **Step 1: Write failing Mock API workflow tests**

`renders manager dashboard and member-owned records`, `calculates domestic and export quotes`, `requires manager approval before PDF or dispatch`, `creates Rev.1 from approved Rev.0`, `keeps won quotes separate from shipments`를 작성한다.

- [ ] **Step 2: Ask for test permission and verify RED**

Run: `& 'C:\Users\14Z980\.cache\codex-runtimes\codex-primary-runtime\dependencies\bin\fallback\pnpm.cmd' --dir src/frontend test -- quotation-inquiry-ui.test.tsx`

Expected: FAIL because quotation contracts and API methods do not exist.

- [ ] **Step 3: Add exact TypeScript contracts**

문의 상태는 `received | assigned | responding | quoting | answered | closed`, 견적 상태는 `draft | approvalRequested | approved | rejected | sent | won | lost | onHold`, 세금은 `exclusive | inclusive | exempt`, 언어는 `ko | en`, 거래는 `domestic | export`로 고정한다.

- [ ] **Step 4: Implement Mock API rules**

팀원 소유권, 팀장 승인, `SWK-YYYYMM-NNN`, 개정 복사, 승인본 불변성, 국내·수출 검증 및 출고 미생성을 실제 상태 전이로 구현한다.

- [ ] **Step 5: Ask for test permission and verify GREEN**

Run: `& 'C:\Users\14Z980\.cache\codex-runtimes\codex-primary-runtime\dependencies\bin\fallback\pnpm.cmd' --dir src/frontend test -- quotation-inquiry-ui.test.tsx`

Expected: quotation Mock API tests pass.

- [ ] **Step 6: Commit Front-end contracts**

```powershell
git add src/frontend/src/shared/api src/frontend/tests/quotation-inquiry-ui.test.tsx
git commit -m "feat: add quotation inquiry frontend contracts"
```

### Task 3: Implement the React quotation workspace

**Files:**
- Create: `src/frontend/src/features/quotes/QuotationInquiryPage.tsx`
- Create: `src/frontend/src/features/quotes/QuoteDashboard.tsx`
- Create: `src/frontend/src/features/quotes/InquiryList.tsx`
- Create: `src/frontend/src/features/quotes/InquiryDetail.tsx`
- Create: `src/frontend/src/features/quotes/QuoteList.tsx`
- Create: `src/frontend/src/features/quotes/QuoteEditor.tsx`
- Create: `src/frontend/src/features/quotes/QuoteApprovalPanel.tsx`
- Create: `src/frontend/src/features/quotes/quoteCalculations.ts`
- Create: `src/frontend/src/features/quotes/quotes.css`
- Create: `src/frontend/tests/quotation-inquiry-http-api.test.ts`
- Modify: `src/frontend/src/shared/api/HttpSalesApi.ts`
- Modify: `src/frontend/src/app/App.tsx`
- Modify: `src/frontend/src/features/admin/AdminPage.tsx`
- Modify: `src/frontend/tests/app-routing.test.tsx`
- Modify: `src/frontend/tests/quotation-inquiry-ui.test.tsx`

**Interfaces:**
- Consumes: Task 2의 DTO와 `SalesApi` 메서드
- Produces: 인증된 `/quotes` 라우트, 정확한 메뉴 순서, 역할·상태별 UI와 HTTP 계약

- [ ] **Step 1: Add failing page, permission and calculation tests**

세 탭, 팀원 소유 데이터만 표시, 팀장 승인 버튼, 기한초과 강조, 정확한 메뉴 순서, 세금 3종, 수출조건 필수, 승인 후 편집 잠금, 새 개정 버튼 및 팀장 전용 Incoterms 관리를 검증한다.

- [ ] **Step 2: Add failing HTTP adapter tests**

설계 명세 12절의 URL·메서드·본문과 첨부파일 `multipart/form-data`, PDF `Blob` 응답 매핑을 검증한다.

- [ ] **Step 3: Ask for test permission and verify RED**

Run: `& 'C:\Users\14Z980\.cache\codex-runtimes\codex-primary-runtime\dependencies\bin\fallback\pnpm.cmd' --dir src/frontend test -- quotation-inquiry-ui.test.tsx quotation-inquiry-http-api.test.ts app-routing.test.tsx`

Expected: FAIL because `/quotes` workspace and HTTP mappings do not exist.

- [ ] **Step 4: Implement `calculateQuote(input: QuoteCalculationInput): QuoteCalculationResult`**

품목 할인 후 합계에 운송비와 기타 비용을 더하고 세금구분별 공급금액·세액·최종금액을 반환한다. KRW는 정수, USD·CNY는 소수점 둘째 자리에서 반올림한다.

- [ ] **Step 5: Implement the page components**

목업의 탭·필터·상세·편집·승인 흐름을 재현하고 `PermissionGate`와 `useAuth`를 사용해 역할별 동작을 제한한다.

- [ ] **Step 6: Add the menu and route**

`App.tsx`의 `/quotes`를 `출고·매출 관리` 다음, `환율·목표 관리` 앞에 추가하고 라우팅 테스트의 정확한 순서를 갱신한다.

- [ ] **Step 7: Implement HTTP mappings**

Task 2의 메서드를 설계 명세 12절 API로 매핑하고 `AdminPage`에 Incoterms 활성·비활성·순서 편집기를 추가한다. Back-end 구현 전 404·501 응답은 기존 `SalesApiError`로 표시한다.

- [ ] **Step 8: Ask for test permission and verify GREEN**

Run: `& 'C:\Users\14Z980\.cache\codex-runtimes\codex-primary-runtime\dependencies\bin\fallback\pnpm.cmd' --dir src/frontend test`

Expected: all Front-end tests pass.

- [ ] **Step 9: Ask for build permission and verify production output**

Run: `& 'C:\Users\14Z980\.cache\codex-runtimes\codex-primary-runtime\dependencies\bin\fallback\pnpm.cmd' --dir src/frontend lint` and `& 'C:\Users\14Z980\.cache\codex-runtimes\codex-primary-runtime\dependencies\bin\fallback\pnpm.cmd' --dir src/frontend build`

Expected: lint has 0 errors and Vite build succeeds.

- [ ] **Step 10: Verify the React page in the browser**

팀장·팀원 메뉴, 세 탭, 문의→견적 흐름, 국내·수출, 계산, 승인 잠금 및 콘솔 오류를 확인한다.

- [ ] **Step 11: Commit the Front-end**

```powershell
git add src/frontend
git commit -m "feat: add quotation inquiry frontend"
```

- [ ] **Step 12: STOP for Front-end approval**

사용자 승인 전 Back-end 파일을 수정하지 않는다.

### Task 4: Define quotation domain models and calculations

**Files:**
- Create: `src/backend/SalesManagement.Core/Models/QuotationEnums.cs`
- Create: `src/backend/SalesManagement.Core/Models/CustomerInquiry.cs`
- Create: `src/backend/SalesManagement.Core/Models/InquiryActivity.cs`
- Create: `src/backend/SalesManagement.Core/Models/InquiryAttachment.cs`
- Create: `src/backend/SalesManagement.Core/Models/Quote.cs`
- Create: `src/backend/SalesManagement.Core/Models/QuoteRevision.cs`
- Create: `src/backend/SalesManagement.Core/Models/QuoteLine.cs`
- Create: `src/backend/SalesManagement.Core/Models/QuoteApproval.cs`
- Create: `src/backend/SalesManagement.Core/Models/QuoteDispatch.cs`
- Create: `src/backend/SalesManagement.Core/Models/Incoterm.cs`
- Create: `src/backend/SalesManagement.Core/Quotations/QuotationContracts.cs`
- Create: `src/backend/SalesManagement.Core/Quotations/QuoteCalculator.cs`
- Create: `src/backend/SalesManagement.Tests/QuoteCalculationTests.cs`

**Interfaces:**
- Consumes: 기존 `EntityBase`, `Currency`, `ItemType`, `User`, `Customer`, `Product`
- Produces: `QuoteCalculator.Calculate(QuoteCalculationInput): QuoteAmounts`; 설계 명세 11절 엔터티와 Task 5~8이 사용하는 명령 레코드

- [ ] **Step 1: Write failing calculation and validation tests**

`calculates exclusive tax`, `backs tax out of inclusive total`, `keeps exempt tax zero`, `rounds KRW to zero decimals`, `rounds USD and CNY to two decimals`, `rejects mixed currency`, `clears incoterm for domestic`, `requires incoterm and named place for export`를 작성한다.

- [ ] **Step 2: Ask for test permission and verify RED**

Run: `& 'C:\Users\14Z980\Documents\ChatGPT\영업관리 프로세스 구축\.tools\dotnet\dotnet.exe' test src/backend/SalesManagement.Tests/SalesManagement.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~QuoteCalculationTests"`

Expected: FAIL because quotation models and calculator do not exist.

- [ ] **Step 3: Add entities, enums and command contracts**

모든 상태값과 필드를 설계 명세와 일치시키고 금액은 `decimal`, 수량은 `decimal`, 날짜는 업무일에 `DateOnly`, 사건 시각은 `DateTimeOffset`을 사용한다.

- [ ] **Step 4: Implement the pure calculator**

라인별 할인, 비용, 세금구분 및 통화별 반올림을 한 곳에서 계산하고 저장되는 계산값과 PDF 표시값이 같은 결과를 사용하게 한다.

- [ ] **Step 5: Ask for test permission and verify GREEN**

Run the Task 4 filtered test command.

Expected: all `QuoteCalculationTests` pass.

- [ ] **Step 6: Commit domain models and calculator**

```powershell
git add src/backend/SalesManagement.Core src/backend/SalesManagement.Tests/QuoteCalculationTests.cs
git commit -m "feat: add quotation domain calculations"
```

### Task 5: Implement customer inquiry workflow and ownership

**Files:**
- Create: `src/backend/SalesManagement.Core/Repositories/IQuotationRepositories.cs`
- Create: `src/backend/SalesManagement.Core/Quotations/InquiryService.cs`
- Create: `src/backend/SalesManagement.Core/Quotations/AttachmentValidator.cs`
- Create: `src/backend/SalesManagement.InMemory/Repositories/InMemoryQuotationRepositories.cs`
- Create: `src/backend/SalesManagement.Tests/InquiryWorkflowTests.cs`
- Create: `src/backend/SalesManagement.Tests/QuotationAuthorizationTests.cs`

**Interfaces:**
- Consumes: Task 4의 문의·활동·첨부 모델과 기존 `AuthorizationService`
- Produces: `InquiryService.ListAsync(InquiryQuery, User)`, `GetAsync(Guid, User)`, `SaveAsync(SaveInquiryCommand, User)`, `AssignAsync(Guid, Guid, long, User)`, `ChangeStatusAsync(Guid, InquiryStatus, long, User)`, `AddActivityAsync(Guid, AddInquiryActivityCommand, User)`, `AddAttachmentAsync(Guid, AttachmentUpload, User)`, `ReopenAsync(Guid, long, User)`

- [ ] **Step 1: Write failing workflow and object-authorization tests**

팀원 본인 데이터 범위, 다른 담당자 ID 직접 접근 403, 팀장 전체 범위, 상태 전이, 종료 재개, 기한초과 계산, 추가 전용 활동, 파일 크기·확장자·MIME·시그니처·안전한 파일명을 검증한다.

- [ ] **Step 2: Ask for test permission and verify RED**

Run: `& 'C:\Users\14Z980\Documents\ChatGPT\영업관리 프로세스 구축\.tools\dotnet\dotnet.exe' test src/backend/SalesManagement.Tests/SalesManagement.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~InquiryWorkflowTests|FullyQualifiedName~QuotationAuthorizationTests"`

Expected: FAIL because inquiry services and repositories do not exist.

- [ ] **Step 3: Define focused repository contracts**

문의, 활동, 첨부, 견적, 개정, 품목, 승인, 발송, Incoterms 및 월별 번호발급 저장소를 분리하고 각 계약에 필요한 조회만 노출한다.

- [ ] **Step 4: Implement in-memory repositories and `AttachmentValidator`**

Front-end 샘플과 별개인 테스트 저장소를 만들고 허용 형식과 10MB 제한을 서버에서 강제한다.

- [ ] **Step 5: Implement `InquiryService`**

소유권, 팀장 담당자 변경, 상태 전이, 종료 재개, 활동 자동기록 및 기한초과 계산을 적용한다.

- [ ] **Step 6: Ask for test permission and verify GREEN**

Run the Task 5 filtered test command.

Expected: inquiry and authorization tests pass.

- [ ] **Step 7: Commit inquiry workflow**

```powershell
git add src/backend/SalesManagement.Core src/backend/SalesManagement.InMemory src/backend/SalesManagement.Tests
git commit -m "feat: add customer inquiry workflow"
```

### Task 6: Implement quotation revisions, approvals and results

**Files:**
- Create: `src/backend/SalesManagement.Core/Quotations/QuoteService.cs`
- Create: `src/backend/SalesManagement.Core/Quotations/QuotationDashboardService.cs`
- Create: `src/backend/SalesManagement.Core/Quotations/IQuotePdfRenderer.cs`
- Create: `src/backend/SalesManagement.Tests/QuoteWorkflowTests.cs`

**Interfaces:**
- Consumes: Task 4의 계산기·모델, Task 5의 저장소 계약, `ITransactionRunner`
- Produces: `QuoteService.CreateAsync(CreateQuoteCommand, User)`, `SaveRevisionAsync(Guid, Guid, SaveQuoteRevisionCommand, User)`, `SubmitAsync(Guid, Guid, long, User)`, `ApproveAsync(Guid, Guid, long, User)`, `RejectAsync(Guid, Guid, string, long, User)`, `CreateRevisionAsync(Guid, Guid, long, User)`, `RecordDispatchAsync(Guid, Guid, RecordDispatchCommand, User)`, `RecordResultAsync(Guid, Guid, QuoteStatus, string?, long, User)`, `GetPdfModelAsync(Guid, Guid, User)`; `QuotationDashboardService.GetAsync(QuotationDashboardQuery, User)`; `IQuotePdfRenderer.Render(QuotePdfModel): byte[]`

- [ ] **Step 1: Write failing quote workflow tests**

번호 형식과 월별 초기화, 국내·수출 조건, 승인요청 편집 잠금, 팀장 승인·반려, 승인 전 PDF 금지, 승인본 불변성, `Rev.1` 스냅샷 복사, 발송 후 수주·실주·보류, 출고 미생성, 팀원·팀장별 KPI와 `수주/(수주+실주)` 수주율을 검증한다.

- [ ] **Step 2: Ask for test permission and verify RED**

Run: `& 'C:\Users\14Z980\Documents\ChatGPT\영업관리 프로세스 구축\.tools\dotnet\dotnet.exe' test src/backend/SalesManagement.Tests/SalesManagement.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~QuoteWorkflowTests"`

Expected: FAIL because `QuoteService` does not exist.

- [ ] **Step 3: Implement quote creation and snapshot persistence**

문의 소유권을 확인하고 번호 저장소에서 다음 번호를 받아 `Rev.0`과 품목·고객·조건 스냅샷을 생성한다.

- [ ] **Step 4: Implement revision and approval state machine**

`draft/rejected`만 수정, `approvalRequested` 잠금, 팀장 전용 승인·반려, 승인본 불변 및 승인본에서만 다음 개정 생성을 강제한다.

- [ ] **Step 5: Implement dispatch and independent result recording**

승인본에 PDF 해시·생성자·생성시각과 발송일시·메모를 기록하고, `won/lost/onHold` 변경 시 Shipment 저장소를 호출하지 않는다.

- [ ] **Step 6: Implement `QuotationDashboardService`**

팀원은 본인 담당 건만, 팀장은 전체 또는 선택 담당자의 미답변·기한초과·승인대기·통화별 견적액·결과 건수·수주율을 반환한다.

- [ ] **Step 7: Ask for test permission and verify GREEN**

Run the Task 6 filtered test command.

Expected: all `QuoteWorkflowTests` pass.

- [ ] **Step 8: Commit quote workflow**

```powershell
git add src/backend/SalesManagement.Core src/backend/SalesManagement.Tests/QuoteWorkflowTests.cs
git commit -m "feat: add quotation approval workflow"
```

### Task 7: Expose HTTP APIs and generate approved PDFs

**Files:**
- Create: `src/backend/SalesManagement.Api/Endpoints/InquiryEndpoints.cs`
- Create: `src/backend/SalesManagement.Api/Endpoints/QuoteEndpoints.cs`
- Create: `src/backend/SalesManagement.Api/Endpoints/IncotermEndpoints.cs`
- Create: `src/backend/SalesManagement.Api/Quotations/MigraDocQuotePdfRenderer.cs`
- Create: `src/backend/SalesManagement.Api/Quotations/SystemFontResolver.cs`
- Modify: `src/backend/SalesManagement.Api/Program.cs`
- Modify: `src/backend/SalesManagement.Api/SalesManagement.Api.csproj`
- Create: `src/backend/SalesManagement.Tests/QuotationContractTests.cs`
- Create: `src/backend/SalesManagement.Tests/QuotePdfTests.cs`
- Modify: `Dockerfile`

**Interfaces:**
- Consumes: Task 5~6 서비스와 설계 명세 12절 API 경계
- Produces: 인증된 `/api/inquiries`, `/api/quotes`, 팀장 전용 `/api/admin/incoterms`; `MigraDocQuotePdfRenderer : IQuotePdfRenderer`

- [ ] **Step 1: Write failing endpoint contract tests**

모든 경로·메서드, `Version` 본문, 400·403·404·409·422 매핑, 팀원 직접 객체 접근 차단, 첨부파일과 PDF 콘텐츠 헤더를 검증한다.

- [ ] **Step 2: Write failing PDF tests**

국문·영문 `QuotePdfModel` 계약에 견적번호·개정·합계·Incoterms가 정확히 들어가고 렌더러가 `%PDF-` 바이트를 생성하며, 미승인 모델은 서비스에서 렌더러까지 도달하지 않는지 검증한다.

- [ ] **Step 3: Ask for test permission and verify RED**

Run: `& 'C:\Users\14Z980\Documents\ChatGPT\영업관리 프로세스 구축\.tools\dotnet\dotnet.exe' test src/backend/SalesManagement.Tests/SalesManagement.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~QuotationContractTests|FullyQualifiedName~QuotePdfTests"`

Expected: FAIL because endpoints and renderer do not exist.

- [ ] **Step 4: Ask for dependency permission and add the PDF package**

Run: `& 'C:\Users\14Z980\Documents\ChatGPT\영업관리 프로세스 구축\.tools\dotnet\dotnet.exe' add src/backend/SalesManagement.Api/SalesManagement.Api.csproj package PDFsharp-MigraDoc --version 6.2.4`

- [ ] **Step 5: Implement endpoints and error mappings**

현재 사용자 조회를 공통화하고 DTO 외부에서 엔터티를 직접 노출하지 않으며 파일 업로드에 요청 크기 제한을 적용한다.

- [ ] **Step 6: Implement `SystemFontResolver` and `MigraDocQuotePdfRenderer`**

Windows는 맑은 고딕, Linux는 `fonts-noto-cjk`를 읽고 PDFsharp Core font resolver에 등록한다. 국문·영문 템플릿은 동일 계산 결과와 승인 스냅샷만 사용한다.

- [ ] **Step 7: Update the Docker runtime image**

`fonts-noto-cjk`를 `libgssapi-krb5-2`와 함께 설치하고 런타임 사용자가 글꼴 파일을 읽을 수 있게 한다.

- [ ] **Step 8: Ask for test permission and verify GREEN**

Run the Task 7 filtered test command, then full `SalesManagement.Tests`.

Expected: endpoint/PDF tests and all Back-end tests pass.

- [ ] **Step 9: Verify Linux-compatible publish**

Run: `& 'C:\Users\14Z980\Documents\ChatGPT\영업관리 프로세스 구축\.tools\dotnet\dotnet.exe' publish src/backend/SalesManagement.Api/SalesManagement.Api.csproj -c Release --no-restore -o .tmp/quote-api-publish`

Expected: publish succeeds with 0 errors.

- [ ] **Step 10: Commit Back-end API and PDF**

```powershell
git add src/backend Dockerfile
git commit -m "feat: add quotation inquiry backend"
```

- [ ] **Step 11: STOP for Back-end approval**

사용자 승인 전 PostgreSQL 파일과 마이그레이션을 수정하지 않는다.

### Task 8: Add PostgreSQL persistence and migration

**Files:**
- Create: `src/backend/SalesManagement.Postgres/Configurations/QuotationConfiguration.cs`
- Create: `src/backend/SalesManagement.Postgres/Repositories/QuotationRepositories.cs`
- Modify: `src/backend/SalesManagement.Postgres/SalesDbContext.cs`
- Modify: `src/backend/SalesManagement.Postgres/PostgresServiceExtensions.cs`
- Create: `src/backend/SalesManagement.Postgres/Migrations/<timestamp>_AddQuotationInquiry.cs`
- Modify: `src/backend/SalesManagement.Postgres/Migrations/SalesDbContextModelSnapshot.cs`
- Create: `src/backend/SalesManagement.DbTests/QuotationInquiryDatabaseTests.cs`

**Interfaces:**
- Consumes: Task 5의 저장소 계약과 Task 4의 엔터티
- Produces: PostgreSQL 저장소, 테이블 9종, 월별 번호 시퀀스, 첨부파일 `bytea`, additive-only migration

- [ ] **Step 1: Write failing database tests**

문의·활동·첨부·견적·개정·품목·승인·발송·Incoterms 왕복, 담당자/문의/품목 FK, 삭제 제한, 견적번호·개정번호 고유성, 승인본 저장, 10MB 파일, 동시 번호발급 및 `Version` 충돌을 검증한다.

- [ ] **Step 2: Ask for test permission and verify RED**

Run: `& 'C:\Users\14Z980\Documents\ChatGPT\영업관리 프로세스 구축\.tools\dotnet\dotnet.exe' test src/backend/SalesManagement.DbTests/SalesManagement.DbTests.csproj -c Release --no-restore --filter "FullyQualifiedName~QuotationInquiryDatabaseTests"`

Expected: FAIL because DbSets, configurations and repositories do not exist.

- [ ] **Step 3: Configure tables, precision and relationships**

금액 `numeric(20,4)`, 할인율·세율 `numeric(7,4)`, 수량 `numeric(20,4)`, 파일 `bytea`, 상태 문자열 변환, 고유 인덱스 및 모든 업무 FK의 `Restrict` 삭제를 적용한다.

- [ ] **Step 4: Implement EF repositories and atomic numbering**

`quote_number_sequences`의 `(Year, Month)` PK 행을 `INSERT ... ON CONFLICT ... DO UPDATE SET "NextValue" = ... RETURNING`으로 증가시켜 `NNN`을 반환한다.

- [ ] **Step 5: Register DbSets and repositories**

`SalesDbContext`와 `PostgresServiceExtensions`를 갱신하고 InMemory·PostgreSQL 모드가 동일 서비스 계약을 충족하게 한다.

- [ ] **Step 6: Generate and inspect the migration**

Run: `& 'C:\Users\14Z980\Documents\ChatGPT\영업관리 프로세스 구축\.tools\dotnet\dotnet.exe' ef migrations add AddQuotationInquiry --project src/backend/SalesManagement.Postgres --startup-project src/backend/SalesManagement.Api`

Expected: 신규 테이블·인덱스·FK만 추가하고 기존 테이블 삭제·열 삭제·데이터 손실 SQL이 없다.

- [ ] **Step 7: Ask for test permission and verify GREEN**

Run the Task 8 filtered test command, then full `SalesManagement.DbTests`.

Expected: database tests pass.

- [ ] **Step 8: Verify PostgreSQL-specific behavior**

승인된 Docker/PostgreSQL 검사 환경에서 마이그레이션, 동시 번호발급, 10MB 첨부파일 왕복과 롤백을 실행한다. SQLite 통과만으로 완료 처리하지 않는다.

- [ ] **Step 9: Commit Database persistence**

```powershell
git add src/backend/SalesManagement.Postgres src/backend/SalesManagement.DbTests
git commit -m "feat: persist quotation inquiry module"
```

- [ ] **Step 10: STOP for Database approval**

사용자 승인 전 통합 배포를 진행하지 않는다.

### Task 9: Verify, document and deploy the completed module

**Files:**
- Modify: `deploy/runbook.md`
- Modify: `deploy/tests/RenderDeployment.Tests.ps1`
- Modify: `README.md`

**Interfaces:**
- Consumes: Task 1~8의 전체 모듈
- Produces: 운영 절차, 전체 검증 증거, Render 시험환경 배포

- [ ] **Step 1: Add failing deployment checks**

Docker 이미지의 `fonts-noto-cjk`, 메뉴 라우트, PDF 패키지, 마이그레이션 및 첨부파일 10MB 제한 문서가 존재하는지 검사한다.

- [ ] **Step 2: Ask for test permission and verify RED**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File deploy/tests/RenderDeployment.Tests.ps1`

Expected: FAIL until runbook and deployment assertions are updated.

- [ ] **Step 3: Update runbook and README**

한글 글꼴, PDF 진단, 첨부파일 데이터베이스 용량, 백업·복원, 승인흐름, 롤백 및 Render 검증 절차를 기록한다.

- [ ] **Step 4: Ask for complete verification permission**

다음 검증 묶음을 실행한다고 알리고 진행한다.

- [ ] **Step 5: Run Front-end verification**

Run: `& 'C:\Users\14Z980\.cache\codex-runtimes\codex-primary-runtime\dependencies\bin\fallback\pnpm.cmd' --dir src/frontend lint`, `test`, `build`

Expected: lint 0 errors, all tests pass, production build succeeds.

- [ ] **Step 6: Run Back-end and Database verification**

Run: `& 'C:\Users\14Z980\Documents\ChatGPT\영업관리 프로세스 구축\.tools\dotnet\dotnet.exe' test src/backend/SalesManagement.sln -c Release --no-restore` and `build -c Release --no-restore`

Expected: all tests pass and Release build has 0 errors.

- [ ] **Step 7: Run deployment verification**

Run: `powershell -NoProfile -ExecutionPolicy Bypass -File deploy/tests/RenderDeployment.Tests.ps1`

Expected: Render Blueprint and deployment checks pass.

- [ ] **Step 8: Verify the complete workflow in the browser**

팀원 문의 등록 → 견적 작성 → 팀장 승인 → 국문·영문 PDF → 발송 기록 → 수주·실주·보류를 확인한다. 메뉴 순서, 다른 담당자 데이터 차단, 기한초과, 수출조건 및 콘솔 오류도 확인한다.

- [ ] **Step 9: Commit documentation and final checks**

```powershell
git add deploy/runbook.md deploy/tests/RenderDeployment.Tests.ps1 README.md
git commit -m "docs: document quotation inquiry operations"
```

- [ ] **Step 10: Push the approved history and monitor Render**

원격 `main`이 현재 이력의 fast-forward 조상인지 확인한 뒤 푸시한다. 새 배포가 `live`, `/health/ready`가 200, 오류 로그가 비어 있고 운영 `/quotes`가 인증과 권한을 적용하는지 검증한다.

- [ ] **Step 11: Preserve the user-owned installation report**

`git status --short`에서 `deploy/installation-report.md`가 미추적 상태로 그대로 남아 있고 커밋에 포함되지 않았는지 확인한다.
