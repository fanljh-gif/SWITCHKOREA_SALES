# SWITCH KOREA 영업관리 시스템 운영 인수서

## 1. 운영 전 확정값

배포 전 다음 값을 확정하고 변경관리 기록에 남긴다.

- 사내 DNS 이름: `미확정`
- 허용 사내망 CIDR: `미확정`
- TLS 인증서와 갱신 담당자: `미확정`
- PostgreSQL 서버·데이터베이스·운영 계정: `미확정`
- 일별 공시환율 제공처와 수집 시각: `미확정`
- 백업 폴더·별도 저장장치·보관일수: `미확정`
- 장애 연락 담당자와 연락처: `미확정`

비밀번호와 연결 문자열은 문서나 Git에 기록하지 않는다. IIS 서버의 보호된 Machine 환경변수 `ConnectionStrings__SalesManagement`로 제공한다.

## 2. 설치

1. Windows Server에 IIS와 ASP.NET Core Hosting Bundle, PostgreSQL client, .NET SDK, Node.js/pnpm을 준비한다.
2. TLS 인증서를 LocalMachine/My 저장소에 설치한다.
3. 관리자 PowerShell에서 보호된 연결 문자열과 `PGPASSWORD`를 작업 시점에만 설정한다.
4. `Install-SalesManagement.ps1`을 `SourceRoot`, `InstallPath`, `HostName`, `IntranetCidr`와 함께 실행한다. TLS를 사용할 때 `CertificateThumbprint`를 지정한다.
5. `Migrations/InitialCreate.sql`을 검토 후 운영 DB에 적용한다.
6. `/health`와 로그인 화면을 확인하고 방화벽이 지정 CIDR 이외 접근을 차단하는지 확인한다.

## 3. 초기 사용자와 일상 점검

- 최초 팀장 계정은 운영 승인자가 일회성 초기 비밀번호로 생성하고 즉시 변경한다.
- 매일 09:10에 USD/CNY 일별환율 수집 상태와 마지막 성공값을 확인한다.
- IIS 로그, Windows 응용프로그램 로그, PostgreSQL 로그에서 오류를 확인한다.
- 사용자 중지, 역할 변경, 환율·목표·단가·출고 변경은 감사 이력과 대조한다.

## 4. 백업

- 작업 스케줄러에서 `Backup-SalesDatabase.ps1`을 매일 실행한다.
- 백업은 날짜별 custom-format 파일이며 스크립트 실패 종료코드를 모니터링한다.
- 최소 월 1회 별도 저장장치 복사본을 확인하고, 분기 1회 검증 DB로 복원 연습한다.

## 5. 복원 연습과 장애 대응

1. 운영 DB가 아닌 별도 검증 DB를 만든다.
2. `Restore-SalesDatabase.ps1`을 실행하고 PowerShell 확인 질문에서 대상 서버·DB를 재확인한다.
3. 사용자·거래처·출고·감사 이력 건수를 백업 원본과 비교한다.
4. 장애 시 IIS 앱 풀 → `/health` → 연결 문자열 존재 여부 → PostgreSQL 연결/용량 → 최근 migration → 최근 백업 순으로 점검한다.
5. 운영 복원은 서비스 중지, 변경 승인, 복원 대상 시각 확정 후에만 수행한다.

## 6. 배포 체크리스트

- Front-end 테스트·빌드 통과
- Back-end 및 DB 테스트 통과
- migration SQL 검토
- 임시 IIS 사이트 동일 출처 `/api` 확인
- CIDR 외 접근 차단 확인
- 백업 생성 및 별도 검증 DB 복원 건수 일치
- 팀장 최종 승인과 배포 시각 기록
