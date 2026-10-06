window.SALES_MOCK_DATA = {
  users: [
    { id: "u1", name: "이재학", loginId: "manager", role: "manager", title: "영업팀장", status: "active" },
    { id: "u2", name: "박태원", loginId: "taewon", role: "member", title: "영업담당", status: "active" },
    { id: "u3", name: "최명철", loginId: "myeongcheol", role: "member", title: "영업담당", status: "active" },
    { id: "u4", name: "이소연", loginId: "soyeon", role: "member", title: "영업담당", status: "active" },
    { id: "u5", name: "이창선", loginId: "changseon", role: "member", title: "영업담당", status: "active" }
  ],
  customers: [
    { id: "c1", code: "C-1001", name: "대성전자", country: "대한민국", currency: "KRW", ownerId: "u2", owner: "박태원", status: "active", monthlySales: 184200000 },
    { id: "c2", code: "C-1002", name: "Horizon Controls", country: "미국", currency: "USD", ownerId: "u3", owner: "최명철", status: "active", monthlySales: 137700000 },
    { id: "c3", code: "C-1003", name: "上海精密科技", country: "중국", currency: "CNY", ownerId: "u4", owner: "이소연", status: "active", monthlySales: 98400000 },
    { id: "c4", code: "C-1004", name: "한빛오토텍", country: "대한민국", currency: "KRW", ownerId: "u5", owner: "이창선", status: "active", monthlySales: 76100000 },
    { id: "c5", code: "C-1005", name: "Nexa Devices", country: "베트남", currency: "USD", ownerId: "u2", owner: "박태원", status: "inactive", monthlySales: 32800000 }
  ],
  products: [
    { id: "p1", code: "SW-TAC-001", name: "Tact Switch 6×6", type: "제품", spec: "160gf / SMT", status: "active" },
    { id: "p2", code: "SW-MIC-014", name: "Micro Switch", type: "제품", spec: "1A 125VAC / Lever", status: "active" },
    { id: "p3", code: "SW-ROT-007", name: "Rotary Switch", type: "제품", spec: "12 Position / DIP", status: "active" },
    { id: "p4", code: "SW-SLD-021", name: "Slide Switch", type: "상품", spec: "SPDT / SMT", status: "active" },
    { id: "p5", code: "SW-DET-005", name: "Detector Switch", type: "상품", spec: "Right Angle / SMT", status: "inactive" }
  ],
  prices: [
    { id: "pr1", customer: "대성전자", product: "Tact Switch 6×6", currency: "KRW", amount: 142, start: "2026-07-01", end: "-", status: "active" },
    { id: "pr2", customer: "Horizon Controls", product: "Micro Switch", currency: "USD", amount: 0.84, start: "2026-09-01", end: "-", status: "active" },
    { id: "pr3", customer: "上海精密科技", product: "Rotary Switch", currency: "CNY", amount: 4.2, start: "2026-08-15", end: "-", status: "active" },
    { id: "pr4", customer: "한빛오토텍", product: "Slide Switch", currency: "KRW", amount: 318, start: "2026-01-01", end: "2026-12-31", status: "active" }
  ],
  shipments: [
    { id: "s1", date: "2026-10-01", number: "SO-261001-04", customer: "대성전자", product: "Tact Switch 6×6", quantity: 120000, currency: "KRW", unitPrice: 142, originalAmount: 17040000, rate: 1, krwAmount: 17040000, ownerId: "u2", owner: "박태원", status: "active" },
    { id: "s2", date: "2026-10-01", number: "SO-261001-03", customer: "Horizon Controls", product: "Micro Switch", quantity: 18000, currency: "USD", unitPrice: 0.84, originalAmount: 15120, rate: 1350, krwAmount: 20412000, ownerId: "u3", owner: "최명철", status: "active" },
    { id: "s3", date: "2026-10-01", number: "SO-261001-02", customer: "上海精密科技", product: "Rotary Switch", quantity: 26000, currency: "CNY", unitPrice: 4.2, originalAmount: 109200, rate: 188, krwAmount: 20529600, ownerId: "u4", owner: "이소연", status: "active" },
    { id: "s4", date: "2026-09-30", number: "SO-260930-11", customer: "한빛오토텍", product: "Slide Switch", quantity: 34000, currency: "KRW", unitPrice: 318, originalAmount: 10812000, rate: 1, krwAmount: 10812000, ownerId: "u5", owner: "이창선", status: "active" },
    { id: "s5", date: "2026-09-29", number: "SO-260929-08", customer: "Nexa Devices", product: "Tact Switch 6×6", quantity: 10000, currency: "USD", unitPrice: 0.31, originalAmount: 3100, rate: 1345, krwAmount: 4169500, ownerId: "u2", owner: "박태원", status: "cancelled" }
  ],
  monthlyRates: [
    { month: "2026-10", currency: "USD", rate: 1350, updatedBy: "이재학", updatedAt: "2026-09-30 16:40" },
    { month: "2026-10", currency: "CNY", rate: 188, updatedBy: "이재학", updatedAt: "2026-09-30 16:41" }
  ],
  dailyRates: [
    { date: "2026-10-01", currency: "USD", rate: 1346.2, change: -3.1, status: "success", collectedAt: "09:02" },
    { date: "2026-10-01", currency: "CNY", rate: 187.4, change: 0.6, status: "success", collectedAt: "09:02" }
  ],
  targets: [
    { userId: "u1", owner: "이재학", target: 100000000, actual: 90000000 },
    { userId: "u2", owner: "박태원", target: 220000000, actual: 180000000 },
    { userId: "u3", owner: "최명철", target: 180000000, actual: 150000000 },
    { userId: "u4", owner: "이소연", target: 160000000, actual: 128900000 },
    { userId: "u5", owner: "이창선", target: 140000000, actual: 100000000 }
  ],
  audits: [
    { at: "2026-10-01 09:18", user: "박태원", action: "출고 등록", target: "SO-261001-04", detail: "대성전자 / 120,000 EA" },
    { at: "2026-10-01 09:07", user: "최명철", action: "출고 등록", target: "SO-261001-03", detail: "Horizon Controls / 18,000 EA" },
    { at: "2026-09-30 16:41", user: "이재학", action: "환율 변경", target: "2026-10 CNY", detail: "186.50 → 188.00" },
    { at: "2026-09-30 16:40", user: "이재학", action: "환율 변경", target: "2026-10 USD", detail: "1,340.00 → 1,350.00" },
    { at: "2026-09-30 14:22", user: "이재학", action: "담당자 변경", target: "한빛오토텍", detail: "이소연 → 이창선" }
  ],
  networkCompanies: [
    { id: "nc-samsung", name: "삼성전자", code: "OEM-001", country: "대한민국", industry: "전자", lifecycleType: "customer", isOem: true, ownerId: "u1", owner: "이재학", status: "active", address: "경기도 수원시", note: "주요 원청사" },
    { id: "nc-lg", name: "LG전자", code: "OEM-002", country: "대한민국", industry: "전자", lifecycleType: "customer", isOem: true, ownerId: "u1", owner: "이재학", status: "active", address: "서울특별시 영등포구", note: "주요 원청사" },
    { id: "nc-a", name: "대성전자", code: "C-1001", country: "대한민국", industry: "전자부품", lifecycleType: "customer", isOem: false, customerId: "c1", ownerId: "u2", owner: "박태원", status: "active", address: "경기도 화성시", note: "삼성·LG 공통 1차 협력사" },
    { id: "nc-b", name: "세진모듈", code: "P-2001", country: "대한민국", industry: "모듈", lifecycleType: "prospect", isOem: false, ownerId: "u3", owner: "최명철", status: "active", address: "충청남도 아산시", note: "신규 스위치 적용 검토 중" },
    { id: "nc-c", name: "한빛정밀", code: "P-2002", country: "대한민국", industry: "정밀가공", lifecycleType: "prospect", isOem: false, ownerId: "u2", owner: "박태원", status: "active", address: "경기도 안산시", note: "대성전자 추천 잠재고객" },
    { id: "nc-d", name: "한빛오토텍", code: "C-1004", country: "대한민국", industry: "자동차 전장", lifecycleType: "customer", isOem: false, customerId: "c4", ownerId: "u5", owner: "이창선", status: "active", address: "경기도 평택시", note: "차량용 스위치 납품" },
    { id: "nc-e", name: "미래하네스", code: "P-2003", country: "대한민국", industry: "와이어링", lifecycleType: "prospect", isOem: false, ownerId: "u4", owner: "이소연", status: "active", address: "경상북도 구미시", note: "하위 공급망 후보" }
  ],
  networkRelationships: [
    { id: "nr1", upstreamId: "nc-samsung", downstreamId: "nc-a", productIds: ["p1", "p2"], application: "생활가전 조작부", validFrom: "2024-01-01", validTo: "-", status: "active", notes: "양산 적용" },
    { id: "nr2", upstreamId: "nc-samsung", downstreamId: "nc-b", productIds: ["p2"], application: "세탁기 도어", validFrom: "2026-05-01", validTo: "-", status: "active", notes: "샘플 평가" },
    { id: "nr3", upstreamId: "nc-lg", downstreamId: "nc-a", productIds: ["p4"], application: "공조기", validFrom: "2025-03-01", validTo: "-", status: "active", notes: "공통 협력사" },
    { id: "nr4", upstreamId: "nc-a", downstreamId: "nc-c", productIds: ["p1"], application: "조작부 ASSY", validFrom: "2026-02-01", validTo: "-", status: "active", notes: "개발 협력" },
    { id: "nr5", upstreamId: "nc-c", downstreamId: "nc-d", productIds: ["p3"], application: "차량용 콘솔", validFrom: "2026-04-01", validTo: "-", status: "active", notes: "승인 완료" },
    { id: "nr6", upstreamId: "nc-d", downstreamId: "nc-e", productIds: ["p5"], application: "하네스 모듈", validFrom: "2026-08-01", validTo: "-", status: "active", notes: "견적 협의" }
  ],
  networkContacts: [
    { id: "nct1", companyId: "nc-a", name: "김현수", department: "구매팀", position: "부장", role: "구매 의사결정", mobile: "010-2345-6789", email: "hskim@daesung.example", validFrom: "2025-01-01", validTo: "-", employmentStatus: "재직" },
    { id: "nct2", companyId: "nc-a", name: "정다은", department: "개발팀", position: "책임", role: "기술 검토", mobile: "010-3456-7890", email: "dajung@daesung.example", validFrom: "2026-03-01", validTo: "-", employmentStatus: "재직" },
    { id: "nct3", companyId: "nc-a", name: "오민석", department: "구매팀", position: "차장", role: "구매 실무", mobile: "-", email: "-", validFrom: "2023-01-01", validTo: "2024-12-31", employmentStatus: "퇴사" },
    { id: "nct4", companyId: "nc-b", name: "박지영", department: "개발구매", position: "과장", role: "샘플 평가", mobile: "010-4567-8901", email: "jypark@sejin.example", validFrom: "2026-05-01", validTo: "-", employmentStatus: "재직" }
  ],
  networkAssignments: [
    { id: "na1", companyId: "nc-a", ownerId: "u2", owner: "박태원", validFrom: "2026-01-01", validTo: "-", reason: "권역 재조정", current: true },
    { id: "na2", companyId: "nc-a", ownerId: "u4", owner: "이소연", validFrom: "2024-01-01", validTo: "2025-12-31", reason: "담당자 변경", current: false },
    { id: "na3", companyId: "nc-b", ownerId: "u3", owner: "최명철", validFrom: "2026-05-01", validTo: "-", reason: "신규 발굴", current: true },
    { id: "na4", companyId: "nc-c", ownerId: "u2", owner: "박태원", validFrom: "2026-02-01", validTo: "-", reason: "연계 영업", current: true }
  ],
  networkHistory: [
    { companyId: "nc-a", at: "2026-10-02 11:20", user: "박태원", action: "담당자 연락처 수정", detail: "김현수 부장 휴대폰 변경" },
    { companyId: "nc-a", at: "2026-09-18 15:10", user: "이재학", action: "협력 관계 수정", detail: "LG전자 공조기 프로젝트 추가" },
    { companyId: "nc-a", at: "2026-01-01 09:00", user: "이재학", action: "당사 담당자 변경", detail: "이소연 → 박태원" },
    { companyId: "nc-b", at: "2026-10-01 10:35", user: "최명철", action: "잠재고객 등록", detail: "세진모듈 신규 등록" }
  ]
};
