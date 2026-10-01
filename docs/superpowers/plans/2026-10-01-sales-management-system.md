# 영업관리 시스템 1차 구축 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Windows 사내망에서 팀장 이재학과 팀원 박태원·최명철·이소연·이창선이 거래처·품목·단가·출고·환율·목표·매출을 공동 관리하는 영업관리 시스템 1차 버전을 구축한다.

**Architecture:** IIS가 React Front-end와 ASP.NET Core Web API를 동일 출처로 제공하는 모듈형 단일 애플리케이션이다. UI 목업과 Front-end는 목 데이터를 사용하고, Back-end는 저장소 인터페이스와 인메모리 구현으로 먼저 완성한 뒤 마지막 Database 단계에서 PostgreSQL 구현체로 교체한다.

**Tech Stack:** HTML/CSS/JavaScript, React 19, TypeScript 5, Vite, Vitest, Testing Library, ASP.NET Core 10 LTS, xUnit, ClosedXML, EF Core, Npgsql, PostgreSQL 18, IIS

**Spec:** `docs/superpowers/specs/2026-10-01-sales-management-system-design.md`

## Global Constraints

- 개발 순서는 반드시 `UI 목업 → Front-end → Back-end → Database`로 진행한다.
- 각 단계 결과를 사용자에게 제시하고 명시적 승인을 받은 뒤 다음 단계로 진행한다.
- 테스트 또는 검증 명령을 실행하기 직전에 사용자에게 허가를 요청하고, 승인 전에는 실행하지 않는다.
- 1차 범위는 공통 기반과 영업관리 프로그램으로 제한하며 영업망·견적/문의·잠재고객 모듈은 구현하지 않는다.
- Windows 사내 서버와 사내망 전용 접속을 기준으로 한다.
- 팀원은 전체 자료를 조회하지만 본인 담당 거래처만 수정하고, 팀장은 전체 자료를 관리한다.
- 매출은 부가세 제외 공급가액이며 KRW 환율은 1, USD·CNY는 출고월의 팀장 지정환율을 사용한다.
- 일별 공시환율은 참고용이며 매출 계산에 사용하지 않는다.
- 출고 저장 시 적용단가·적용환율·공급가액·원화 환산금액을 스냅샷으로 보존한다.
- 삭제 대신 취소 또는 사용 중지 상태를 사용한다.
- ERP 자동 연동은 구현하지 않고 `.xlsx` 가져오기와 직접 입력을 지원한다.
- 회사명과 시스템 브랜드는 `스위치코리아 (SWITCH KOREA)`를 사용한다.
- 품목 구분은 `제품 | 상품` 두 값으로 등록하고 조회·필터한다.
- 대시보드는 KRW·USD·CNY별 원통화 판매액, 원화 환산액, 전체 원화 환산매출 대비 비중(%)을 표시한다.
- Git 커밋 전에 사용자가 제공한 저장소 로컬 `user.name`과 `user.email`을 설정하며 임의의 신원을 만들지 않는다.

## Review Focus

- 출고번호가 없거나 중복인 행: 후보로 분류하고 사용자가 결정하기 전에는 실제 자료에 반영하지 않는다(Task 8, 10).
- 같은 거래처·품목의 단가 적용기간이 겹치는 입력: 저장을 거부하고 충돌 기간을 알려준다(Task 7, 11).
- 월 지정환율 또는 목표가 없거나 목표가 0인 경우: 출고 저장과 달성률 표시가 정의된 상태로 동작한다(Task 4, 8, 9).
- 두 사용자가 같은 자료를 동시에 수정하는 경우: 먼저 저장된 값을 덮어쓰지 않고 충돌 응답을 반환한다(Task 7, 8, 11).
- 손상된 파일, 필수 열 누락, 잘못된 자료형이 있는 Excel: 행별 오류를 반환하고 전체 반영을 취소한다(Task 5, 10).

---

## 파일 구조

```text
docs/
  superpowers/
    specs/2026-10-01-sales-management-system-design.md
    plans/2026-10-01-sales-management-system.md
src/
  mockup/
    index.html                 # 모든 승인 대상 화면의 정적 구조
    styles.css                 # 목업 전용 반응형 레이아웃과 디자인 토큰
    mock-data.js               # 5명, 거래처, 품목, 매출 예시 자료
    app.js                     # 메뉴 전환, 모달, 필터, 업로드 단계 상호작용
  frontend/
    package.json
    vite.config.ts
    src/
      app/                     # 라우팅, 세션, 레이아웃
      shared/api/              # API 계약, 클라이언트, 목 어댑터
      shared/ui/               # 표, 필터, 통화, 오류 표시 공용 컴포넌트
      features/auth/           # 로그인과 최초 비밀번호 변경
      features/dashboard/      # KPI, 차트, 필터
      features/customers/      # 거래처와 담당자
      features/products/       # 품목
      features/prices/         # 거래처별 단가와 기간
      features/shipments/      # 출고 직접 입력과 조회
      features/imports/        # Excel 검증과 반영 흐름
      features/settings/       # 환율과 목표
      features/admin/          # 사용자와 감사 이력
    tests/                     # Front-end 기능 및 접근성 테스트
  backend/
    SalesManagement.sln
    SalesManagement.Api/       # HTTP, 인증 쿠키, 미들웨어, 구성
    SalesManagement.Core/      # 엔터티, 서비스, 저장소 인터페이스
    SalesManagement.InMemory/  # Database 단계 전 임시 저장소
    SalesManagement.Postgres/  # EF Core DbContext와 PostgreSQL 저장소
    SalesManagement.Tests/     # 단위 및 API 테스트
    SalesManagement.DbTests/   # PostgreSQL 통합 테스트
deploy/
  iis/                         # web.config와 IIS 배포 안내
  scripts/                     # Windows 서비스·백업·복원 스크립트
  runbook.md                   # 설치, 운영, 장애 및 복원 절차
```

---

## Phase 1 — UI 목업

### Task 1: 전체 UI 목업과 승인 게이트

**Files:**
- Create: `src/mockup/index.html`
- Create: `src/mockup/styles.css`
- Create: `src/mockup/mock-data.js`
- Create: `src/mockup/app.js`

**Interfaces:**
- Consumes: 설계 명세의 7개 메뉴, 팀장·팀원 권한, 계산 및 Excel 업무 흐름
- Produces: Front-end가 그대로 구현할 화면 구조, 한국어 라벨, 상태, 필터 및 입력 흐름

- [ ] **Step 1: 목업 데이터 계약을 작성한다**

`mock-data.js`에 `users`, `customers`, `products`, `prices`, `shipments`, `monthlyRates`, `dailyRates`, `targets`를 정의한다. 통화는 `KRW | USD | CNY`, 역할은 `manager | member`, 상태는 `active | inactive | cancelled`만 사용한다.

- [ ] **Step 2: 공통 셸을 작성한다**

`index.html`에 로그인 화면, 좌측 메뉴, 상단 사용자 영역, 본문, 알림 영역을 만들고 대시보드·거래처·품목/단가·출고/매출·Excel 가져오기·환율/목표·시스템 관리 화면을 연결한다.

- [ ] **Step 3: 대시보드 목업을 작성한다**

오늘 매출, 당월 매출, 목표, 달성률, 담당자·거래처·품목별 실적, KRW·USD·CNY 원통화, 원화 환산값과 기간/담당자/거래처/품목 필터를 표현한다.

- [ ] **Step 4: 기준정보와 출고 목업을 작성한다**

거래처, 품목, 기간형 단가, 출고 목록·등록·수정·취소와 변경 이력 패널을 구현한다. 팀원 화면에서는 타 담당 거래처의 수정 동작을 비활성화한다.

- [ ] **Step 5: Excel·환율·목표·관리 목업을 작성한다**

Excel의 `파일 선택 → 열 연결 → 검증 → 오류 확인 → 최종 반영`, 월별 적용환율, 일별 참고환율, 담당자별 목표, 사용자 및 감사 이력 화면을 구현한다.

- [ ] **Step 6: 목업 상호작용을 작성한다**

`app.js`에서 메뉴 이동, 필터, 모달, 역할 전환, 업로드 단계 전환만 구현한다. 영구 저장이나 실제 파일 분석은 하지 않는다.

- [ ] **Step 7: 목업 검증 실행 허가를 요청한다**

사용자에게 브라우저 확인 및 정적 파일 검사 실행 여부를 묻고, 승인 전에는 다음 명령을 실행하지 않는다.

- [ ] **Step 8: 승인 후 목업 검증을 실행한다**

Run: 브라우저에서 `src/mockup/index.html` 열기 및 정적 구문 검사

Expected: 7개 메뉴가 열리고, 팀장/팀원 상태와 Excel 5단계 흐름이 동작하며 콘솔 오류가 없다.

- [ ] **Step 9: UI 목업 단계 승인을 받는다**

목업을 사용자에게 제시하고 수정 요청을 반영한다. 사용자가 명시적으로 승인할 때까지 Front-end 작업을 시작하지 않는다.

- [ ] **Step 10: 승인된 목업을 커밋한다**

```bash
git add src/mockup
git commit -m "feat: add approved sales management UI mockup"
```

---

## Phase 2 — Front-end

### Task 2: Front-end 기반과 API 계약

**Files:**
- Create: `src/frontend/package.json`
- Create: `src/frontend/vite.config.ts`
- Create: `src/frontend/tsconfig.json`
- Create: `src/frontend/src/main.tsx`
- Create: `src/frontend/src/app/App.tsx`
- Create: `src/frontend/src/shared/api/contracts.ts`
- Create: `src/frontend/src/shared/api/SalesApi.ts`
- Create: `src/frontend/src/shared/api/MockSalesApi.ts`
- Test: `src/frontend/tests/app-routing.test.tsx`

**Interfaces:**
- Consumes: Task 1에서 승인된 메뉴, 라벨과 상태 흐름
- Produces: `SalesApi` 인터페이스와 전체 Front-end가 공유하는 DTO 타입

- [ ] **Step 1: Front-end 프로젝트를 구성한다**

`package.json`, TypeScript와 Vite 설정을 만들고 React, Router, Vitest, Testing Library, ESLint 의존성을 고정한다. 설치 명령은 프로젝트 폴더에서 `npm install` 하나만 사용한다.

- [ ] **Step 2: 실패하는 라우팅 테스트를 작성한다**

```tsx
it('renders all seven approved routes', () => {
  render(<App api={mockApi} />)
  expect(screen.getByRole('link', { name: '대시보드' })).toBeVisible()
  expect(screen.getByRole('link', { name: '시스템 관리' })).toBeVisible()
})
```

- [ ] **Step 3: 테스트 실행 허가를 요청한다**

승인 전에는 테스트 명령을 실행하지 않는다.

- [ ] **Step 4: 승인 후 실패를 확인한다**

Run: `npm test -- app-routing.test.tsx`

Expected: FAIL — `App` 또는 라우트가 아직 없음.

- [ ] **Step 5: 계약과 목 API를 작성한다**

`contracts.ts`에 `Role`, `Currency`, `ItemType = "product" | "goods"`, `User`, `Customer`, `Product`, `Price`, `Shipment`, `MonthlyRate`, `DailyRate`, `SalesTarget`, `DashboardQuery`, `DashboardSummary`, `ImportValidation`을 정의한다. `Product.itemType`은 필수이며, `SalesApi.ts`는 각 기능의 `list/get/create/update/cancel/validate/commit` 메서드를 명시한다.

- [ ] **Step 6: 앱 셸과 라우팅을 구현한다**

승인된 목업의 7개 메뉴와 로그인 전용 라우트를 구성하고 `api: SalesApi`를 Context로 주입한다.

- [ ] **Step 7: 승인 후 Front-end 기반 테스트를 실행한다**

Run: `npm test -- app-routing.test.tsx`

Expected: PASS.

- [ ] **Step 8: 커밋한다**

```bash
git add src/frontend
git commit -m "feat: establish frontend contracts and app shell"
```

### Task 3: 로그인, 세션 및 권한 UI

**Files:**
- Create: `src/frontend/src/features/auth/LoginPage.tsx`
- Create: `src/frontend/src/features/auth/ChangePasswordPage.tsx`
- Create: `src/frontend/src/app/AuthSession.tsx`
- Create: `src/frontend/src/shared/ui/PermissionGate.tsx`
- Test: `src/frontend/tests/auth-permissions.test.tsx`

**Interfaces:**
- Consumes: `SalesApi.login`, `logout`, `changePassword`, `getCurrentUser`; `Role`
- Produces: `useAuth(): AuthSessionState`, `<PermissionGate allow="manager|owner">`

- [ ] **Step 1: 권한 테스트를 작성한다**

```tsx
it('allows members to view all customers but edit only owned customers', async () => {
  renderMemberApp({ memberId: 'u2', customerOwnerId: 'u1' })
  expect(await screen.findByText('거래처 A')).toBeVisible()
  expect(screen.queryByRole('button', { name: '수정' })).not.toBeInTheDocument()
})
```

- [ ] **Step 2: 테스트 실행 허가를 요청하고 승인 후 실패를 확인한다**

Run: `npm test -- auth-permissions.test.tsx`

Expected: FAIL — 세션과 권한 컴포넌트가 없음.

- [ ] **Step 3: 로그인과 최초 비밀번호 변경 UI를 구현한다**

로그인 실패, 임시 잠금, 최초 로그인 비밀번호 변경 필요 상태를 각각 표시한다. 인증정보를 localStorage에 저장하지 않는다.

- [ ] **Step 4: `PermissionGate`를 구현한다**

팀장은 모든 편집 동작, 팀원은 `ownerUserId === currentUser.id`인 자료의 편집 동작만 렌더링한다. 이는 UX 제어이며 서버 권한 검사를 대체하지 않는다.

- [ ] **Step 5: 승인 후 권한 테스트를 실행한다**

Run: `npm test -- auth-permissions.test.tsx`

Expected: PASS.

- [ ] **Step 6: 커밋한다**

```bash
git add src/frontend/src/features/auth src/frontend/src/app/AuthSession.tsx src/frontend/src/shared/ui/PermissionGate.tsx src/frontend/tests/auth-permissions.test.tsx
git commit -m "feat: add frontend authentication and permission states"
```

### Task 4: 대시보드 Front-end

**Files:**
- Create: `src/frontend/src/features/dashboard/DashboardPage.tsx`
- Create: `src/frontend/src/features/dashboard/DashboardFilters.tsx`
- Create: `src/frontend/src/features/dashboard/KpiCards.tsx`
- Create: `src/frontend/src/features/dashboard/SalesBreakdown.tsx`
- Create: `src/frontend/src/shared/ui/Money.tsx`
- Test: `src/frontend/tests/dashboard.test.tsx`

**Interfaces:**
- Consumes: `SalesApi.getDashboard(query: DashboardQuery): Promise<DashboardSummary>`
- Produces: URL과 동기화되는 `DashboardQuery`; KRW·USD·CNY와 달성률 표시

- [ ] **Step 1: 대시보드 표시 테스트를 작성한다**

```tsx
it('shows original currencies, KRW total, and a safe zero-target state', async () => {
  renderDashboard({ krwTotal: 1500000, targetKrw: 0, achievementRate: null })
  expect(await screen.findByText('₩1,500,000')).toBeVisible()
  expect(screen.getByText('목표 미설정')).toBeVisible()
})
```

- [ ] **Step 2: 테스트 실행 허가를 요청하고 승인 후 실패를 확인한다**

Run: `npm test -- dashboard.test.tsx`

Expected: FAIL — 대시보드 컴포넌트가 없음.

- [ ] **Step 3: 필터와 KPI를 구현한다**

기간, 담당자, 거래처, 품목 필터를 URL 검색조건으로 유지하고 오늘 매출, 당월 매출, 목표, 달성률을 표시한다.

- [ ] **Step 4: 실적 분해와 통화 표시를 구현한다**

담당자·거래처·품목별 실적과 KRW·USD·CNY 원통화 합계, 원화 환산합계, 전체 원화 환산매출 대비 통화별 비중(%)을 제공한다. 목표가 0 또는 없으면 나눗셈을 하지 않고 `목표 미설정`을 표시한다.

- [ ] **Step 5: 승인 후 대시보드 테스트를 실행한다**

Run: `npm test -- dashboard.test.tsx`

Expected: PASS.

- [ ] **Step 6: 커밋한다**

```bash
git add src/frontend/src/features/dashboard src/frontend/src/shared/ui/Money.tsx src/frontend/tests/dashboard.test.tsx
git commit -m "feat: add sales dashboard frontend"
```

### Task 5: 업무 화면 Front-end와 단계 승인

**Files:**
- Create: `src/frontend/src/features/customers/CustomerPage.tsx`
- Create: `src/frontend/src/features/products/ProductPage.tsx`
- Create: `src/frontend/src/features/prices/PricePage.tsx`
- Create: `src/frontend/src/features/shipments/ShipmentPage.tsx`
- Create: `src/frontend/src/features/imports/ImportWizard.tsx`
- Create: `src/frontend/src/features/settings/SettingsPage.tsx`
- Create: `src/frontend/src/features/admin/AdminPage.tsx`
- Create: `src/frontend/src/shared/ui/ConcurrencyNotice.tsx`
- Test: `src/frontend/tests/business-flows.test.tsx`

**Interfaces:**
- Consumes: Task 2의 고객·품목·단가·출고·가져오기·환율·목표·사용자·감사 API
- Produces: 승인된 7개 메뉴의 완성된 Front-end와 충돌·검증 오류 표시 규칙

- [ ] **Step 1: 핵심 업무 흐름 테스트를 작성한다**

```tsx
it('keeps invalid import rows out of the commit request', async () => {
  renderImportWizard(validationWithOneInvalidRow)
  expect(screen.getByText('오류 1건')).toBeVisible()
  expect(screen.getByRole('button', { name: '최종 반영' })).toBeDisabled()
})
```

- [ ] **Step 2: 테스트 실행 허가를 요청하고 승인 후 실패를 확인한다**

Run: `npm test -- business-flows.test.tsx`

Expected: FAIL — 업무 화면이 없음.

- [ ] **Step 3: 거래처·품목·단가 화면을 구현한다**

조회, 신규, 수정, 사용 중지, 담당자 변경, 품목의 제품·상품 구분, 단가 적용기간 입력을 구현한다. API의 `409 Conflict`를 `다른 사용자가 먼저 수정했습니다` 또는 `단가 적용기간이 겹칩니다`로 구분한다.

- [ ] **Step 4: 출고 화면을 구현한다**

출고일·거래처·품목을 선택해 유효 단가를 조회하고 수량에 따른 공급가액과 원화 환산금액을 미리 표시한다. 월 지정환율이 없으면 저장을 막고 설정 화면으로 안내한다.

- [ ] **Step 5: Excel 가져오기 화면을 구현한다**

파일 선택, 열 연결, 검증, 오류·중복 확인, 최종 반영의 5단계를 구현한다. 치명적 오류가 하나라도 있으면 최종 반영 버튼을 비활성화한다.

- [ ] **Step 6: 환율·목표·시스템 관리 화면을 구현한다**

팀장 전용 편집, 일별 참고환율 상태, 사용자 생성·중지, 감사 이력 필터를 구현한다.

- [ ] **Step 7: 승인 후 전체 Front-end 테스트를 실행한다**

Run: `npm test`

Expected: 모든 Front-end 테스트 PASS.

- [ ] **Step 8: Front-end 단계 승인을 받는다**

구현 화면을 사용자에게 제시하고 수정 요청을 반영한다. 사용자가 명시적으로 승인할 때까지 Back-end 작업을 시작하지 않는다.

- [ ] **Step 9: 승인된 Front-end를 커밋한다**

```bash
git add src/frontend
git commit -m "feat: complete approved sales management frontend"
```

---

## Phase 3 — Back-end

### Task 6: Back-end 기반, 계약 및 인메모리 저장소

**Files:**
- Create: `src/backend/SalesManagement.sln`
- Create: `src/backend/SalesManagement.Core/SalesManagement.Core.csproj`
- Create: `src/backend/SalesManagement.Core/Models/EntityBase.cs`
- Create: `src/backend/SalesManagement.Core/Models/Enums.cs`
- Create: `src/backend/SalesManagement.Core/Models/User.cs`
- Create: `src/backend/SalesManagement.Core/Models/Customer.cs`
- Create: `src/backend/SalesManagement.Core/Models/Product.cs`
- Create: `src/backend/SalesManagement.Core/Models/Price.cs`
- Create: `src/backend/SalesManagement.Core/Models/Shipment.cs`
- Create: `src/backend/SalesManagement.Core/Models/ExchangeRates.cs`
- Create: `src/backend/SalesManagement.Core/Models/SalesTarget.cs`
- Create: `src/backend/SalesManagement.Core/Models/AuditEntry.cs`
- Create: `src/backend/SalesManagement.Core/Models/ImportBatch.cs`
- Create: `src/backend/SalesManagement.Core/Models/YearMonth.cs`
- Create: `src/backend/SalesManagement.Core/Repositories/IUserRepository.cs`
- Create: `src/backend/SalesManagement.Core/Repositories/ICustomerRepository.cs`
- Create: `src/backend/SalesManagement.Core/Repositories/IProductRepository.cs`
- Create: `src/backend/SalesManagement.Core/Repositories/IPriceRepository.cs`
- Create: `src/backend/SalesManagement.Core/Repositories/IShipmentRepository.cs`
- Create: `src/backend/SalesManagement.Core/Repositories/IRateRepository.cs`
- Create: `src/backend/SalesManagement.Core/Repositories/ITargetRepository.cs`
- Create: `src/backend/SalesManagement.Core/Repositories/IAuditRepository.cs`
- Create: `src/backend/SalesManagement.Core/Repositories/IImportBatchRepository.cs`
- Create: `src/backend/SalesManagement.InMemory/SalesManagement.InMemory.csproj`
- Create: `src/backend/SalesManagement.InMemory/Repositories/InMemoryRepositories.cs`
- Create: `src/backend/SalesManagement.Api/SalesManagement.Api.csproj`
- Create: `src/backend/SalesManagement.Api/Program.cs`
- Create: `src/backend/SalesManagement.Tests/SalesManagement.Tests.csproj`
- Test: `src/backend/SalesManagement.Tests/ContractTests.cs`

**Interfaces:**
- Consumes: Front-end `SalesApi` DTO와 상태값
- Produces: `IUserRepository`, `ICustomerRepository`, `IProductRepository`, `IPriceRepository`, `IShipmentRepository`, `IRateRepository`, `ITargetRepository`, `IAuditRepository`, `IImportBatchRepository`

- [ ] **Step 1: Back-end 솔루션을 구성한다**

`dotnet new`로 solution, class library 2개, minimal Web API, xUnit 프로젝트를 만들고 프로젝트 참조를 `Api → Core + InMemory`, `InMemory → Core`, `Tests → Core + InMemory + Api` 방향으로 제한한다.

- [ ] **Step 2: 계약 테스트를 작성한다**

```csharp
[Fact]
public void Currency_AllowsOnlyApprovedValues()
{
    Enum.GetNames<Currency>().Should().Equal("KRW", "USD", "CNY");
}
```

- [ ] **Step 3: 테스트 실행 허가를 요청하고 승인 후 실패를 확인한다**

Run: `dotnet test src/backend/SalesManagement.Tests --filter ContractTests`

Expected: FAIL — 솔루션과 계약이 없음.

- [ ] **Step 4: Core 모델과 저장소 인터페이스를 작성한다**

모든 업무 엔터티는 `Guid Id`, `DateTimeOffset CreatedAt`, `DateTimeOffset UpdatedAt`, `long Version`을 가진다. 금액과 환율은 `decimal`, 날짜는 `DateOnly`, 월은 `YearMonth` 값 객체를 사용하며 품목은 필수 `ItemType.Product | ItemType.Goods` 값을 가진다.

- [ ] **Step 5: 인메모리 저장소와 API 구성을 작성한다**

저장소는 비동기 메서드와 `CancellationToken`을 사용한다. 갱신 시 `expectedVersion`이 다르면 `ConcurrencyException`을 발생시킨다.

- [ ] **Step 6: 승인 후 계약 테스트를 실행한다**

Run: `dotnet test src/backend/SalesManagement.Tests --filter ContractTests`

Expected: PASS.

- [ ] **Step 7: 커밋한다**

```bash
git add src/backend
git commit -m "feat: establish backend contracts and in-memory storage"
```

### Task 7: 인증, 권한, 거래처·품목·단가 API

**Files:**
- Create: `src/backend/SalesManagement.Core/Auth/AuthService.cs`
- Create: `src/backend/SalesManagement.Core/Auth/AuthorizationService.cs`
- Create: `src/backend/SalesManagement.Core/Customers/CustomerService.cs`
- Create: `src/backend/SalesManagement.Core/Products/ProductService.cs`
- Create: `src/backend/SalesManagement.Core/Prices/PriceService.cs`
- Create: `src/backend/SalesManagement.Api/Endpoints/AuthEndpoints.cs`
- Create: `src/backend/SalesManagement.Api/Endpoints/MasterDataEndpoints.cs`
- Test: `src/backend/SalesManagement.Tests/AuthAndMasterDataTests.cs`

**Interfaces:**
- Consumes: Task 6 저장소, `IPasswordHasher<User>`, 보안 쿠키 인증
- Produces: `/api/auth/*`, `/api/customers`, `/api/products`, `/api/prices`; `PriceService.SaveAsync(command, actor, expectedVersion, ct)`

- [ ] **Step 1: 권한·단가 충돌 테스트를 작성한다**

```csharp
[Fact]
public async Task Member_Cannot_Update_Other_Owners_Customer()
{
    var fixture = MasterDataFixture.MemberEditingAnotherOwnersCustomer();
    await Assert.ThrowsAsync<ForbiddenException>(() => fixture.ExecuteAsync());
}

[Fact]
public async Task Price_Rejects_Overlapping_Effective_Period()
{
    var fixture = MasterDataFixture.WithOverlappingPricePeriods();
    await Assert.ThrowsAsync<PricePeriodConflictException>(() => fixture.ExecuteAsync());
}
```

- [ ] **Step 2: 테스트 실행 허가를 요청하고 승인 후 실패를 확인한다**

Run: `dotnet test src/backend/SalesManagement.Tests --filter AuthAndMasterDataTests`

Expected: FAIL — 서비스와 엔드포인트가 없음.

- [ ] **Step 3: 인증 서비스를 구현한다**

초기 비밀번호 변경, 연속 실패 잠금, 미사용 세션 만료를 구성값으로 받고 비밀번호 해시와 보안 쿠키만 사용한다.

- [ ] **Step 4: 서버 권한 서비스를 구현한다**

팀장은 전체 수정, 팀원은 `Customer.OwnerUserId == actor.Id`인 거래처와 연결 자료만 수정하도록 한다. 조회는 활성 사용자 모두 허용한다.

- [ ] **Step 5: 기준정보와 기간형 단가 서비스를 구현한다**

같은 거래처·품목의 활성 단가 기간이 겹치면 `PricePeriodConflictException`을 반환한다. 동시 수정의 `Version` 불일치는 HTTP 409로 변환한다.

- [ ] **Step 6: 승인 후 테스트를 실행한다**

Run: `dotnet test src/backend/SalesManagement.Tests --filter AuthAndMasterDataTests`

Expected: PASS.

- [ ] **Step 7: 커밋한다**

```bash
git add src/backend
git commit -m "feat: add authentication permissions and master data APIs"
```

### Task 8: 출고 계산과 직접 입력 API

**Files:**
- Create: `src/backend/SalesManagement.Core/Shipments/SalesCalculator.cs`
- Create: `src/backend/SalesManagement.Core/Shipments/ShipmentService.cs`
- Create: `src/backend/SalesManagement.Api/Endpoints/ShipmentEndpoints.cs`
- Test: `src/backend/SalesManagement.Tests/ShipmentTests.cs`

**Interfaces:**
- Consumes: `IPriceRepository`, `IRateRepository`, `IShipmentRepository`, `AuthorizationService`
- Produces: `SalesCalculator.Calculate(quantity, unitPrice, currency, monthlyRate): SalesAmounts`; `/api/shipments` CRUD, `/cancel`, 팀장 전용 `/recalculate`

- [ ] **Step 1: 계산·스냅샷·중복 테스트를 작성한다**

```csharp
[Fact]
public void Calculates_Supply_And_Krw_Amounts()
{
    Assert.Equal(new SalesAmounts(200m, 200m),
        SalesCalculator.Calculate(2m, 100m, Currency.KRW, 1m));
    Assert.Equal(new SalesAmounts(30m, 40500m),
        SalesCalculator.Calculate(3m, 10m, Currency.USD, 1350m));
}

[Fact]
public async Task Missing_Monthly_Rate_Rejects_Foreign_Currency_Shipment()
{
    var fixture = ShipmentFixture.ForeignCurrencyWithoutMonthlyRate();
    await Assert.ThrowsAsync<MissingMonthlyRateException>(() => fixture.SaveAsync());
}

[Fact]
public async Task Duplicate_External_Number_Is_Rejected_Per_Customer()
{
    var fixture = ShipmentFixture.WithDuplicateCustomerShipmentNumber();
    await Assert.ThrowsAsync<DuplicateShipmentNumberException>(() => fixture.SaveAsync());
}

[Fact]
public async Task Only_Manager_Can_Recalculate_Confirmed_Shipments()
{
    var fixture = ShipmentFixture.MemberRequestingHistoricalRecalculation();
    await Assert.ThrowsAsync<ForbiddenException>(() => fixture.RecalculateAsync());
}
```

- [ ] **Step 2: 테스트 실행 허가를 요청하고 승인 후 실패를 확인한다**

Run: `dotnet test src/backend/SalesManagement.Tests --filter ShipmentTests`

Expected: FAIL — 계산기와 서비스가 없음.

- [ ] **Step 3: `SalesCalculator`를 구현한다**

`supplyAmount = quantity * unitPrice`, `krwAmount = supplyAmount * rate`를 `decimal`로 계산한다. KRW는 rate 1만 허용한다.

- [ ] **Step 4: `ShipmentService`를 구현한다**

출고일에 유효한 단가와 출고월 지정환율을 조회하고 단가·환율·두 금액을 스냅샷으로 저장한다. 외화 지정환율이 없으면 저장을 거부한다.

- [ ] **Step 5: 중복·취소·동시수정 규칙을 구현한다**

출고번호가 있으면 거래처 내 유일성을 강제한다. 번호가 없으면 날짜·거래처·품목·수량 조합을 중복 후보로 반환하며 자동 차단하지 않는다. 취소는 상태와 이력을 갱신한다.

- [ ] **Step 6: 팀장 전용 과거 매출 재계산을 구현한다**

`RecalculateAsync(shipmentIds, actor, confirmationToken, ct)`는 영향 건수를 먼저 미리보기로 반환하고, 팀장이 확인 토큰을 보낸 경우에만 현재 단가·지정환율 스냅샷으로 갱신하며 감사 이력을 남긴다.

- [ ] **Step 7: 승인 후 출고 테스트를 실행한다**

Run: `dotnet test src/backend/SalesManagement.Tests --filter ShipmentTests`

Expected: PASS.

- [ ] **Step 8: 커밋한다**

```bash
git add src/backend
git commit -m "feat: add shipment calculations and direct entry API"
```

### Task 9: 환율·목표·대시보드 API

**Files:**
- Create: `src/backend/SalesManagement.Core/Rates/RateService.cs`
- Create: `src/backend/SalesManagement.Core/Rates/DailyRateCollector.cs`
- Create: `src/backend/SalesManagement.Core/Targets/TargetService.cs`
- Create: `src/backend/SalesManagement.Core/Dashboard/DashboardService.cs`
- Create: `src/backend/SalesManagement.Api/Endpoints/RateTargetEndpoints.cs`
- Create: `src/backend/SalesManagement.Api/Endpoints/DashboardEndpoints.cs`
- Create: `src/backend/SalesManagement.Api/Workers/DailyRateWorker.cs`
- Test: `src/backend/SalesManagement.Tests/RateTargetDashboardTests.cs`

**Interfaces:**
- Consumes: 출고 스냅샷, `IRateRepository`, `ITargetRepository`, `IShipmentRepository`
- Produces: `/api/rates/monthly`, `/api/rates/daily`, `/api/targets`, `/api/dashboard`; `DashboardSummary`

- [ ] **Step 1: 대시보드 및 환율 실패 테스트를 작성한다**

```csharp
[Fact]
public async Task Zero_Target_Returns_Null_Achievement_Rate()
{
    var summary = await DashboardFixture.WithTarget(0m).QueryAsync();
    Assert.Null(summary.AchievementRate);
}

[Fact]
public async Task Daily_Rate_Failure_Keeps_Last_Successful_Value()
{
    var rate = await RateFixture.LastSuccess(1350m).ThenProviderFails().CollectAsync();
    Assert.Equal(1350m, rate.Value);
    Assert.Equal(CollectionStatus.Failed, rate.Status);
}

[Fact]
public async Task Filters_By_Date_Owner_Customer_And_Product()
{
    var summary = await DashboardFixture.FilteredResult().QueryAsync();
    Assert.Equal(40500m, summary.KrwTotal);
    Assert.Single(summary.ByOwner);
    Assert.Single(summary.ByCustomer);
    Assert.Single(summary.ByProduct);
}
```

- [ ] **Step 2: 테스트 실행 허가를 요청하고 승인 후 실패를 확인한다**

Run: `dotnet test src/backend/SalesManagement.Tests --filter RateTargetDashboardTests`

Expected: FAIL — 서비스가 없음.

- [ ] **Step 3: 월 지정환율과 목표 서비스를 구현한다**

팀장만 수정할 수 있고, KRW 지정환율은 항상 1이다. 목표는 담당자·월 조합당 하나이며 0은 미설정과 동일한 달성률 상태를 반환한다.

- [ ] **Step 4: 대시보드 집계를 구현한다**

취소 출고를 제외하고 출고 스냅샷을 일·월, 담당자, 거래처, 품목으로 집계한다. 원통화 합계와 원화 합계를 분리해 반환한다.

- [ ] **Step 5: 일별 환율 수집 작업을 구현한다**

외부 제공처 어댑터를 인터페이스로 격리한다. 실패 시 마지막 성공값을 유지하고 `LastAttemptAt`, `LastSuccessAt`, `Status=Failed`를 저장한다.

- [ ] **Step 6: 승인 후 테스트를 실행한다**

Run: `dotnet test src/backend/SalesManagement.Tests --filter RateTargetDashboardTests`

Expected: PASS.

- [ ] **Step 7: 커밋한다**

```bash
git add src/backend
git commit -m "feat: add rates targets and dashboard APIs"
```

### Task 10: Excel 가져오기, 감사 이력 및 Back-end 승인

**Files:**
- Create: `src/backend/SalesManagement.Core/Imports/ExcelImportService.cs`
- Create: `src/backend/SalesManagement.Core/Imports/ImportModels.cs`
- Create: `src/backend/SalesManagement.Core/Audit/AuditService.cs`
- Create: `src/backend/SalesManagement.Api/Endpoints/ImportEndpoints.cs`
- Create: `src/backend/SalesManagement.Api/Endpoints/AdminEndpoints.cs`
- Test: `src/backend/SalesManagement.Tests/ExcelImportTests.cs`
- Test: `src/backend/SalesManagement.Tests/AuditTests.cs`

**Interfaces:**
- Consumes: ClosedXML, Task 6 저장소, Task 7~9 서비스
- Produces: `ValidateAsync(stream, mapping, actor, ct): ImportValidation`; `CommitAsync(batchId, actor, ct): ImportResult`; 감사 조회 API

- [ ] **Step 1: Excel 오류와 원자성 테스트를 작성한다**

```csharp
[Fact]
public async Task Corrupt_Workbook_Returns_Actionable_Error()
{
    var result = await ImportFixture.CorruptWorkbook().ValidateAsync();
    Assert.Contains(result.Errors, error => error.Message == "파일을 읽을 수 없습니다");
}

[Fact]
public async Task Missing_Columns_Returns_Actionable_Error()
{
    var result = await ImportFixture.WorkbookMissingRequiredColumns().ValidateAsync();
    Assert.Contains(result.Errors, error => error.Message == "필수 열이 없습니다");
}

[Fact]
public async Task Commit_With_Fatal_Row_Writes_Nothing()
{
    var fixture = ImportFixture.WithFatalRow();
    await Assert.ThrowsAsync<InvalidImportBatchException>(() => fixture.CommitAsync());
    Assert.Equal(0, await fixture.Shipments.CountAsync());
}

[Fact]
public async Task Missing_Number_Duplicate_Is_Returned_As_Candidate()
{
    var result = await ImportFixture.MissingNumberDuplicateCandidate().ValidateAsync();
    Assert.Contains(result.Rows, row => row.Status == ImportRowStatus.Duplicate);
}
```

- [ ] **Step 2: 감사 이력 테스트를 작성한다**

단가·출고·환율·목표·담당자 변경 시 수행자, 시각, 대상, 작업, 변경 전·후 값이 기록되며 비밀번호와 인증정보가 제외되는지 검증한다.

- [ ] **Step 3: 테스트 실행 허가를 요청하고 승인 후 실패를 확인한다**

Run: `dotnet test src/backend/SalesManagement.Tests --filter "ExcelImportTests|AuditTests"`

Expected: FAIL — 가져오기와 감사 서비스가 없음.

- [ ] **Step 4: Excel 검증과 임시 배치를 구현한다**

거래처·품목·단가·출고 템플릿을 지원하고 행 결과를 `New`, `Duplicate`, `ChangeCandidate`, `Invalid`로 분류한다. 검증 배치는 만료시간을 가진다.

- [ ] **Step 5: 원자적 반영을 구현한다**

치명적 오류가 있으면 `CommitAsync`를 거부한다. 인메모리 단계에서는 복사본에 적용한 후 전체 성공 시 교체하여 원자성을 보장한다.

- [ ] **Step 6: 감사 이력을 구현한다**

주요 서비스의 변경 작업과 로그인 결과를 기록한다. 민감한 속성은 허용 목록 방식으로 제외한다.

- [ ] **Step 7: 관리자 사용자 API를 구현한다**

팀장만 사용자 생성, 초기 비밀번호 발급, 사용 중지, 역할 변경을 수행할 수 있게 한다. 비밀번호 원문은 응답·로그·감사 이력에 남기지 않는다.

- [ ] **Step 8: 승인 후 전체 Back-end 테스트를 실행한다**

Run: `dotnet test src/backend/SalesManagement.sln`

Expected: 모든 Back-end 테스트 PASS.

- [ ] **Step 9: Back-end 단계 승인을 받는다**

API 계약, 권한, 계산, Excel 검증 결과를 사용자에게 제시한다. 사용자가 명시적으로 승인할 때까지 Database 작업을 시작하지 않는다.

- [ ] **Step 10: 승인된 Back-end를 커밋한다**

```bash
git add src/backend
git commit -m "feat: complete approved sales management backend"
```

---

## Phase 4 — Database 및 운영 배포

### Task 11: PostgreSQL 스키마와 저장소 구현

**Files:**
- Create: `src/backend/SalesManagement.Postgres/SalesManagement.Postgres.csproj`
- Create: `src/backend/SalesManagement.Postgres/SalesDbContext.cs`
- Create: `src/backend/SalesManagement.Postgres/Configurations/UserConfiguration.cs`
- Create: `src/backend/SalesManagement.Postgres/Configurations/CustomerConfiguration.cs`
- Create: `src/backend/SalesManagement.Postgres/Configurations/ProductConfiguration.cs`
- Create: `src/backend/SalesManagement.Postgres/Configurations/PriceConfiguration.cs`
- Create: `src/backend/SalesManagement.Postgres/Configurations/ShipmentConfiguration.cs`
- Create: `src/backend/SalesManagement.Postgres/Configurations/RateTargetConfiguration.cs`
- Create: `src/backend/SalesManagement.Postgres/Configurations/ImportAuditConfiguration.cs`
- Create: `src/backend/SalesManagement.Postgres/Repositories/UserRepository.cs`
- Create: `src/backend/SalesManagement.Postgres/Repositories/CustomerRepository.cs`
- Create: `src/backend/SalesManagement.Postgres/Repositories/ProductRepository.cs`
- Create: `src/backend/SalesManagement.Postgres/Repositories/PriceRepository.cs`
- Create: `src/backend/SalesManagement.Postgres/Repositories/ShipmentRepository.cs`
- Create: `src/backend/SalesManagement.Postgres/Repositories/RateTargetRepository.cs`
- Create: `src/backend/SalesManagement.Postgres/Repositories/ImportAuditRepository.cs`
- Create: `src/backend/SalesManagement.Postgres/Migrations/InitialCreate.cs`
- Modify: `src/backend/SalesManagement.Api/Program.cs`
- Create: `src/backend/SalesManagement.DbTests/SalesManagement.DbTests.csproj`
- Test: `src/backend/SalesManagement.DbTests/PostgresConstraintTests.cs`

**Interfaces:**
- Consumes: Task 6의 모든 저장소 인터페이스와 Core 엔터티
- Produces: 동일 인터페이스의 PostgreSQL 구현, EF Core migration, 원자적 transaction boundary

- [ ] **Step 1: 데이터베이스 제약 테스트를 작성한다**

```csharp
[Fact]
public async Task CustomerCode_Is_Unique()
{
    var fixture = await PostgresFixture.CreateAsync();
    await fixture.InsertCustomerAsync("C001");
    await Assert.ThrowsAsync<DbUpdateException>(() => fixture.InsertCustomerAsync("C001"));
}

[Fact]
public async Task CustomerShipmentNumber_Is_Unique_When_Not_Null()
{
    var fixture = await PostgresFixture.WithShipmentAsync("C001", "S001");
    await Assert.ThrowsAsync<DbUpdateException>(() => fixture.InsertShipmentAsync("C001", "S001"));
}

[Fact]
public async Task StaleVersion_Update_Is_Rejected()
{
    var fixture = await PostgresFixture.WithCustomerAsync("C001");
    await fixture.UpdateCustomerAsync("C001", expectedVersion: 1);
    await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
        () => fixture.UpdateCustomerAsync("C001", expectedVersion: 1));
}
```

단가 기간 중첩, 외래키 무결성, 취소 자료 보존, 감사 이력 원자성도 각각 테스트한다.

- [ ] **Step 2: 데이터베이스 테스트 실행 허가를 요청한다**

승인 전에는 PostgreSQL 컨테이너, 로컬 인스턴스 또는 migration 명령을 실행하지 않는다.

- [ ] **Step 3: 승인 후 실패를 확인한다**

Run: `dotnet test src/backend/SalesManagement.DbTests`

Expected: FAIL — DbContext, migration, 저장소가 없음.

- [ ] **Step 4: 스키마 구성을 작성한다**

사용자, 거래처, 품목, 단가, 출고, 월 환율, 일 환율, 목표, 업로드 배치, 감사 이력 테이블을 매핑한다. 금액은 `numeric(20,4)`, 환율은 `numeric(20,8)`, 수량은 `numeric(20,4)`, 시각은 `timestamptz`를 사용한다.

- [ ] **Step 5: 키와 제약조건을 작성한다**

거래처 코드, 품목 코드, 사용자 로그인 ID, 담당자·월 목표, 통화·월 환율을 고유하게 한다. 품목 구분은 `Product | Goods`만 허용하는 제약조건을 둔다. 출고번호가 null이 아닐 때 거래처·출고번호 부분 고유 인덱스를 사용한다. `Version`을 낙관적 동시성 토큰으로 사용한다.

- [ ] **Step 6: PostgreSQL 저장소와 transaction을 구현한다**

인메모리 구현과 동일한 저장소 인터페이스를 구현한다. Excel 최종 반영과 해당 감사 이력을 하나의 transaction으로 묶는다.

- [ ] **Step 7: API 구성을 PostgreSQL로 전환한다**

개발/운영 구성에서 Npgsql 연결 문자열을 환경변수 또는 보호된 IIS 설정으로 받고 소스에 비밀번호를 저장하지 않는다.

- [ ] **Step 8: 승인 후 데이터베이스 테스트를 실행한다**

Run: `dotnet test src/backend/SalesManagement.DbTests`

Expected: PASS.

- [ ] **Step 9: 커밋한다**

```bash
git add src/backend
git commit -m "feat: add PostgreSQL schema and repositories"
```

### Task 12: IIS 배포, 백업·복원 및 최종 승인

**Files:**
- Create: `deploy/iis/web.config`
- Create: `deploy/scripts/Install-SalesManagement.ps1`
- Create: `deploy/scripts/Backup-SalesDatabase.ps1`
- Create: `deploy/scripts/Restore-SalesDatabase.ps1`
- Create: `deploy/runbook.md`
- Create: `src/backend/SalesManagement.DbTests/EndToEndTests.cs`
- Modify: `src/frontend/src/shared/api/HttpSalesApi.ts`
- Modify: `src/frontend/src/main.tsx`

**Interfaces:**
- Consumes: React production build, ASP.NET Core publish output, PostgreSQL migration, IIS
- Produces: Windows 자동 시작, 사내망 접근, 일일 백업, 수동 복원 및 운영 인수 절차

- [ ] **Step 1: 통합 시나리오 테스트를 작성한다**

팀장 로그인, 팀원 생성, 거래처 배정, 단가 입력, 월 환율·목표 설정, 직접 출고, Excel 검증·반영, 대시보드 집계, 타 담당자 수정 거부를 하나의 시나리오로 정의한다.

- [ ] **Step 2: HTTP API 어댑터를 구현한다**

`HttpSalesApi`가 Task 2의 `SalesApi`를 구현하고 쿠키 자격증명, RFC 7807 오류, 401/403/409 상태를 공통 처리하게 한다.

- [ ] **Step 3: IIS 배포 구성을 작성한다**

동일 출처에서 Front-end와 `/api`를 제공하고 Windows 재시작 뒤 자동 구동되도록 문서화한다. 방화벽 범위는 사내망 CIDR을 설치 시 필수 입력으로 받는다.

- [ ] **Step 4: 백업·복원 스크립트를 작성한다**

백업 스크립트는 날짜별 파일, 보관일수, 실패 종료코드를 제공한다. 복원 스크립트는 대상 파일과 대상 데이터베이스를 명시적으로 요구하며 실행 전 확인 절차를 둔다.

- [ ] **Step 5: 운영 인수 문서를 작성한다**

설치, 구성값, 사용자 초기화, 일별 환율 상태 확인, 로그 위치, 백업 일정, 복원 연습, 장애 시 연락·점검 순서를 기록한다. 환율 제공처·사내 DNS·TLS·보관기간은 실제 환경값을 사용자와 확정해 기입한다.

- [ ] **Step 6: 통합·배포·복원 검증 실행 허가를 요청한다**

운영 데이터가 아닌 별도 검증 데이터베이스와 임시 IIS 사이트를 사용할 계획을 제시하고 사용자 승인을 기다린다.

- [ ] **Step 7: 승인 후 전체 검증을 실행한다**

Run: `npm test` in `src/frontend`, `dotnet test src/backend/SalesManagement.sln`, 임시 환경 배포, 백업 생성, 별도 DB 복원

Expected: 모든 테스트 PASS, 사내망 임시 URL 응답 정상, 백업과 복원 데이터 건수 일치.

- [ ] **Step 8: Database 및 1차 시스템 최종 승인을 받는다**

기능, 권한, 통합 흐름, 백업·복원 근거를 사용자에게 제시하고 수정 요청을 반영한다.

- [ ] **Step 9: 최종 문서와 배포 구성을 커밋한다**

```bash
git add src deploy docs
git commit -m "feat: complete intranet sales management system"
```

---

## 실행 시 승인 체크포인트

| 체크포인트 | 필요한 사용자 결정 |
|---|---|
| Task 1 검증 전 | UI 목업 검증 실행 허가 |
| Task 1 완료 | UI 목업 승인 |
| Task 2~5의 각 테스트 전 | Front-end 테스트 실행 허가 |
| Task 5 완료 | Front-end 단계 승인 |
| Task 6~10의 각 테스트 전 | Back-end 테스트 실행 허가 |
| Task 10 완료 | Back-end 단계 승인 |
| Task 11 테스트 전 | PostgreSQL 테스트 환경 사용 허가 |
| Task 12 검증 전 | 통합·배포·백업·복원 검증 허가 |
| Task 12 완료 | Database 및 1차 시스템 최종 승인 |

## 구현 시작 전 사용자 확정값

다음 값은 기능 설계를 변경하지 않지만 관련 작업을 시작하기 전에 사용자에게 확인한다.

- Git 저장소 로컬 커밋 작성자 이름과 이메일
- 실제 Windows 서버 버전과 CPU·메모리·디스크
- 사내 DNS 이름, 사내망 CIDR 및 TLS 사용 여부
- 일별 공시환율 제공처
- 기존 ERP/Excel 파일 샘플과 표준 열 이름
- 비밀번호 최소 길이, 잠금 횟수, 잠금시간, 세션 만료시간
- 백업 폴더, 보관일수 및 별도 저장장치 여부
