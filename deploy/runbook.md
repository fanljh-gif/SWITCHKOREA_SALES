# SWITCH KOREA 영업관리 시스템 운영 인수서

## 임시 Render 테스트 환경

사내 설치 전 담당자 검증은 비공개 GitHub 저장소와 Render 무료 Web Service/PostgreSQL에서 수행한다. `Dockerfile`은 프런트엔드와 API를 하나의 이미지로 만들고 `deploy/render/start.sh`가 마이그레이션 성공 후 API를 시작한다. Render에는 `DATABASE_URL`, `PORT`, `RENDER_EXTERNAL_HOSTNAME`이 주입되어야 하며 `SWITCHKOREA_HOSTING_MODE=Render`를 사용한다.

테스트 환경에는 가상 데이터만 입력한다. 무료 데이터베이스는 30일 내 종료하고, 테스트 완료 후 Web Service와 PostgreSQL을 삭제한다. 초기 비밀번호와 외부 데이터베이스 URL은 Git, Render 로그, 이 문서에 기록하지 않는다.

### 잠재고객 리서치 자동화

- 기본값은 `Research__Provider=Disabled`다. 화면·Excel 검토는 외부 API 없이 사용할 수 있다.
- 자동 웹 리서치를 사용할 때만 Render Secret에 `Research__ApiKey`를 입력하고 `Research__Provider=OpenAI`로 변경한다. 키 값은 Git, DB, 문서, 로그에 남기지 않는다.
- 기본 예약은 매주 월요일 09:00(Asia/Seoul), 월 비용 한도는 100,000원이다. 70%·90%에서 경고하며 100%가 되면 예약 실행만 중지한다. 팀장 수동 실행은 계속 가능하다.
- 외부로 보내는 값은 지역·업종·제품 키워드·검색 건수뿐이다. 고객 연락처, 매출, 단가, 인증정보는 전송하지 않는다.
- 출처 URL과 조사일이 없는 결과는 저장·승인하지 않는다. AI 결과는 추천 자료이며 영업망 전환은 팀장이 직접 승인한다.
- 공급자 장애 시 기존 후보와 다른 영업관리 기능은 계속 사용할 수 있다. 실패 실행은 `partial` 상태에서 재처리한다.
- 공급자를 즉시 중지하려면 `Research__Provider=Disabled`로 되돌리고 Web Service를 재시작한다. 데이터베이스 롤백은 먼저 백업을 확보한 후 `AddProspectResearch` 직전 마이그레이션까지만 수행한다.

### 견적·고객문의 모듈

- 고객문의는 담당자 소유권을 적용하며 팀원은 본인 건만, 팀장은 전체 건과 담당자 변경을 처리한다.
- 견적번호는 `SWK-YYYYMM-NNN`, 개정은 `Rev.0`부터 시작한다. 승인 요청 이후 편집은 잠기며 팀장 승인본만 PDF 생성·발송 기록이 가능하다.
- 수출 견적은 Incoterms 코드와 지정 장소를 모두 입력한다. 국내 견적으로 바꾸면 두 값은 저장하지 않는다.
- 국문 PDF는 Windows의 맑은 고딕, Render Linux의 `fonts-nanum` 단독 TTF로 생성한다. Docker 빌드의 테스트 단계와 런타임 모두 글꼴을 설치한다. PDFsharp는 TTC 컬렉션을 지원하지 않으므로 Noto CJK TTC를 사용하지 않는다. PDF 실패 시 런타임 글꼴 설치와 `/api/quotes/{id}/revisions/{revisionId}/pdf` 응답 로그를 확인한다.
- 기본 Linux 파일은 `/usr/share/fonts/truetype/nanum/NanumGothic.ttf`, `NanumGothicBold.ttf`다. 별도 글꼴 폴더를 쓸 때만 서버 프로세스 환경변수 `SWITCHKOREA_PDF_FONT_DIRECTORY`에 이 두 파일이 있는 폴더를 지정하고 프로세스를 재시작한다. 웹 요청에서 폴더 경로를 받지 않으며 글꼴이 없으면 오류로 처리한다.
- 문의 첨부는 PDF·Excel·Word·PNG·JPEG만 허용하고 파일당 10MB로 제한한다. 첨부는 PostgreSQL `bytea`에 저장되므로 백업 용량과 디스크 증가율을 함께 감시한다.
- 수주·실주·보류는 견적 결과로만 관리하며 출고·매출 데이터를 자동 생성하지 않는다.
- 장애 롤백 전 반드시 백업을 만든다. 애플리케이션을 이전 이미지로 되돌리더라도 `AddQuotationInquiry` 마이그레이션은 기존 문의·견적 데이터 보존을 위해 자동 하향하지 않는다.

### Render Blueprint 및 계정 초기화

1. 비공개 GitHub 저장소에서 루트의 `render.yaml`을 Blueprint로 적용한다. 미리보기에는 `switch-korea-sales-test` Web Service와 `switch-korea-sales-test-db` PostgreSQL만 있어야 한다.
2. 최초 계정 생성 시간에만 현재 PC의 공인 IP를 데이터베이스 접근 허용 목록에 추가하고 Render의 외부 PostgreSQL URL을 현재 PowerShell 프로세스에만 설정한다.

   ```powershell
   $env:RENDER_DATABASE_URL = Read-Host 'Render external PostgreSQL URL'
   .\deploy\scripts\Initialize-RenderTestEnvironment.ps1
   .\deploy\scripts\Initialize-RenderTestEnvironment.ps1 -VerifyOnly
   Remove-Item Env:RENDER_DATABASE_URL
   ```

3. 두 번째 실행이 `created=0, existing=5`를 보고하고 `C:\SwitchKorea\Secrets\render-test-initial-credentials.txt`가 변경되지 않았는지 확인한다.
4. 즉시 데이터베이스 접근 허용 목록을 private-network-only로 되돌린다. 자격증명 파일은 각 담당자에게 개별 전달하고 공유 문서나 채팅에 붙여넣지 않는다.

## 1. 확정 구성

- 서버: `JHLEE`
- 서비스 주소: `https://sales.switchkorea.local`
- 설치 위치: `C:\SwitchKorea\SalesManagement`
- 공개 루트 인증서: `C:\SwitchKorea\Certificates\SwitchKorea-RootCA.cer`
- 초기 자격증명: `C:\SwitchKorea\Secrets\initial-credentials.txt`
- 데이터보호 키: `C:\SwitchKorea\DataProtectionKeys`
- 백업: `C:\SwitchKorea\Backups`, 매일 02:00, 30일 보관

연결 문자열과 비밀번호는 문서·Git·일반 로그에 기록하지 않는다. PostgreSQL은 localhost에서만 수신하고 외부 5432 방화벽 규칙을 만들지 않는다.

## 2. HTTPS 및 IIS 준비

1. 관리자 PowerShell에서 실제 서버 IPv4와 사내망 CIDR로 사전 점검한다.

   ```powershell
   .\deploy\scripts\Test-LocalDeploymentPrerequisites.ps1 -ServerIp 192.0.2.10 -IntranetCidr 192.0.2.0/24
   ```

   위 주소는 문서 예시다. 실제 설치에는 `-DiscoverNetwork` 결과를 검토한 뒤 고정 IP 또는 DHCP 예약 주소를 사용한다.

2. 루트 CA와 서버 인증서를 만들거나 유효한 기존 인증서를 재사용한다.

   ```powershell
   .\deploy\scripts\New-SwitchKoreaCertificate.ps1 -ServerIp <실제-IP>
   ```

3. `ConnectionStrings__SalesManagement`와 `SWITCHKOREA_DATA_PROTECTION_PATH`를 Machine 범위의 보호된 설치 단계에서 설정한다. 연결 문자열은 명령 이력에 직접 입력하지 않는다.
4. 설치 계획을 먼저 확인하고 동일 인자로 실제 설치한다.

   ```powershell
   .\deploy\scripts\Install-SalesManagement.ps1 -SourceRoot <저장소> -ServerIp <실제-IP> -IntranetCidr <실제-CIDR> -CertificateThumbprint <서버-인증서-지문> -PlanOnly
   ```

IIS 앱 풀은 AlwaysRunning이며, HTTP 80은 ASP.NET Core의 HTTPS 전환 경로로만 사용한다. 방화벽은 지정 CIDR에서 들어오는 80/443만 허용한다.

### 전체 로컬 설치 실행

실제 IP와 CIDR을 `C:\SwitchKorea\InstallState\network.json`에 `ServerIp`, `IntranetCidr` 속성으로 저장한다. 공식 PostgreSQL 18 Windows 설치 파일의 SHA-256을 게시자 값과 대조한 뒤 다음과 같이 계획을 확인한다.

```powershell
.\deploy\scripts\Install-LocalSalesManagement.ps1 -NetworkConfigPath C:\SwitchKorea\InstallState\network.json -PlanOnly
```

실제 실행에는 `-PostgresInstallerPath`와 `-PostgresInstallerSha256`을 추가한다. 설치 상태는 `C:\SwitchKorea\InstallState\state.json`에 비밀 없이 저장된다. 실패나 재부팅 뒤에는 동일한 명령을 다시 실행하면 완료 단계는 건너뛰고 실패 단계부터 재개한다. 기존 PostgreSQL 18 서비스를 이어서 구성할 때만 `-PostgresAdminPassword`를 보안 문자열로 전달한다.

## 3. 담당자 PC 설정

1. `SwitchKorea-RootCA.cer`와 `Install-SwitchKoreaClientTrust.ps1`만 담당자 PC로 복사한다. `.pfx`나 서버·루트 개인키는 복사하지 않는다.
2. 담당자 PC의 관리자 PowerShell에서 인증서 지문을 대조해 설치한다.

   ```powershell
   .\Install-SwitchKoreaClientTrust.ps1 -CertificatePath .\SwitchKorea-RootCA.cer -ExpectedThumbprint <루트-지문>
   ```

3. 사내 DNS에 `sales.switchkorea.local`을 서버 고정 IP로 등록한다. DNS 적용 전에는 관리자 권한으로 hosts 파일에 같은 매핑을 임시 추가한다.
4. 브라우저에서 서비스 주소를 열고 인증서 경고가 없는지 확인한다.

## 4. 초기 사용자 및 보안

- 설치 시 `jhlee`, `twpark`, `mcchoi`, `sylee`, `cslee`를 멱등 방식으로 만든다.
- 계정별 임시 비밀번호는 서로 다르며 최초 로그인 후 변경해야 한다.
- 초기 자격증명 파일은 현재 설치 사용자와 로컬 Administrators만 읽는다. 다섯 사용자의 변경 완료 후 안전하게 삭제한다.
- 로그인·권한 거부·단가·출고·담당자 변경은 감사 이력과 대조한다.

## 5. 점검 및 장애 대응

1. `/health`로 프로세스 생존 상태, `/health/ready`로 PostgreSQL 연결 상태를 확인한다.
2. IIS 앱 풀, Windows 응용프로그램 로그, PostgreSQL 서비스·로그, 디스크 여유 공간 순으로 확인한다.
3. PostgreSQL이 외부 인터페이스의 5432에서 수신하지 않는지 확인한다.
4. 인증서 만료 90일 전에 승인된 갱신 절차를 진행한다. 스크립트는 잘못된 기존 인증서를 자동 교체하지 않는다.
5. 서버 PC가 업무시간과 02:00 백업 시간에 절전되지 않도록 전원 정책을 유지한다.

## 6. 백업 및 복원

- 설치기는 별도 `switch_korea_backup` 역할과 Machine DPAPI 보호 파일 `C:\SwitchKorea\Secrets\backup-connection.dpapi`를 만든다. 백업 작업은 제한된 `LOCAL SERVICE` 계정으로 실행하며 평문 비밀번호를 인수나 로그에 남기지 않는다.
- 작업 스케줄러의 `SwitchKorea-Sales-Backup`을 매일 02:00 실행하고 결과 코드를 점검한다. 성공 백업은 custom-format dump, `pg_restore --list` 검증, SHA-256 sidecar까지 모두 있어야 한다.
- 운영 DB에 직접 복원하지 않는다. `Test-SalesBackupRestore.ps1`로 최신 검증 dump를 고유한 검증 DB에 복원한 뒤 사용자·거래처·품목·출고·감사 이력 건수를 비교한다.
- `Restore-SalesDatabase.ps1`은 운영 DB 이름을 기본 차단한다. 운영 복원은 서비스 중지와 변경 승인을 받은 뒤 `-AllowProductionRestore`와 PowerShell 확인을 모두 거쳐야 한다.
- 로컬 백업은 PC 고장에 함께 손실될 수 있으므로 운영 안정화 후 NAS 또는 별도 저장장치로 2차 복사한다.

## 7. 운영 승인 체크리스트

- Front-end 테스트·빌드와 Back-end·DB 테스트 통과
- 인증서 SAN, 체인, 만료일과 HTTPS 무경고 확인
- 서버 재부팅 후 IIS·PostgreSQL 자동 시작 및 데이터 유지
- 다섯 계정 최초 로그인·비밀번호 변경 확인
- 담당자 소유권별 수정 허용/403 거부 확인
- CIDR 외 80/443 차단 및 외부 5432 미노출 확인
- 백업 생성·SHA-256·격리 복원 건수 일치
- 팀장 최종 승인과 승인 시각 기록
- 리서치 API 키 미노출, 월 한도, 예약 시각, 부분실패 재처리 및 승인 근거 확인
- 문의 등록 → 견적 작성 → 팀장 승인 → 국문·영문 PDF → 발송 → 결과 기록 확인
- 다른 담당자 문의·견적 직접 URL 접근 시 403, 첨부 10MB 초과 시 422 확인
