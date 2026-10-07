(function () {
  "use strict";

  const data = window.SALES_MOCK_DATA;
  const state = { role: "manager", currentUserId: "u1", view: "dashboard", importStep: 1, networkRootId: "nc-samsung", selectedNetworkCompanyId: "nc-a", networkTab: "basic", networkCollapsedIds: [], networkCompanyMode: "create", selectedResearchCandidateId: "pc1", researchStatus: "all", researchRegion: "all", researchOwner: "all", researchSearch: "", dialogType: null };
  const $ = (selector, root = document) => root.querySelector(selector);
  const $$ = (selector, root = document) => [...root.querySelectorAll(selector)];
  const won = new Intl.NumberFormat("ko-KR", { style: "currency", currency: "KRW", maximumFractionDigits: 0 });
  const number = new Intl.NumberFormat("ko-KR", { maximumFractionDigits: 2 });

  function formatAmount(currency, value) {
    if (currency === "KRW") return won.format(value);
    return `${currency === "USD" ? "$" : "¥"}${number.format(value)}`;
  }

  function badge(status) {
    const label = { active: "사용 중", inactive: "사용 중지", cancelled: "취소" }[status] || status;
    return `<span class="badge ${status}">${label}</span>`;
  }

  function login(role) {
    state.role = role;
    state.currentUserId = role === "manager" ? "u1" : "u2";
    $("#login-screen").classList.add("is-hidden");
    $("#app-shell").classList.remove("is-hidden");
    applyRole();
    renderAll();
  }

  function applyRole() {
    const user = data.users.find((item) => item.id === state.currentUserId);
    document.body.dataset.role = state.role;
    $("#user-name").textContent = user.name;
    $("#user-role").textContent = state.role === "manager" ? "영업팀장" : "영업담당자";
    $("#user-avatar").textContent = user.name.slice(0, 1);
    $("#welcome-name").textContent = user.name;
    $$('[data-role-switch]').forEach((button) => button.classList.toggle("active", button.dataset.roleSwitch === state.role));
    if (state.role === "member" && ["settings", "admin"].includes(state.view)) showView("dashboard");
    renderCustomers();
    renderShipments();
    renderNetworkTree();
    renderNetworkCompanyDetail();
    renderResearchCandidates();
    renderResearchCandidateDetail();
  }

  function showView(name) {
    const target = $(`#view-${name}`);
    if (!target || (state.role === "member" && ["settings", "admin"].includes(name))) return;
    state.view = name;
    $$(".view").forEach((view) => view.classList.toggle("active", view === target));
    $$(".nav-item").forEach((item) => item.classList.toggle("active", item.dataset.view === name));
    const title = target.dataset.title;
    $("#page-title").textContent = title;
    $("#breadcrumb-current").textContent = title;
    $("#sidebar").classList.remove("open");
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  function renderOwnerChart() {
    const max = Math.max(...data.targets.map((item) => item.target));
    $("#owner-chart").innerHTML = data.targets.map((item) => {
      const actualHeight = Math.max(4, Math.round((item.actual / max) * 125));
      const targetHeight = Math.max(4, Math.round((item.target / max) * 125));
      return `<div class="chart-group"><span class="chart-value">${Math.round(item.actual / 1000000)}M</span><div class="chart-bars"><i style="height:${actualHeight}px"></i><i style="height:${targetHeight}px"></i></div><span class="chart-name">${item.owner}</span></div>`;
    }).join("");
  }

  function renderDashboardShipments() {
    $("#dashboard-shipments").innerHTML = data.shipments.filter((item) => item.status === "active").slice(0, 4).map((item) => `
      <tr><td>${item.date}</td><td><strong>${item.number}</strong></td><td>${item.customer}</td><td>${item.product}</td><td class="right">${number.format(item.quantity)}</td><td class="right">${formatAmount(item.currency, item.originalAmount)}</td><td class="right"><strong>${won.format(item.krwAmount)}</strong></td><td>${item.owner}</td></tr>
    `).join("");
  }

  function renderCustomers() {
    $("#customer-table").innerHTML = data.customers.map((item) => {
      const canEdit = state.role === "manager" || item.ownerId === state.currentUserId;
      return `<tr><td><strong>${item.code}</strong></td><td><div class="subcell"><strong>${item.name}</strong><small>ID ${item.id.toUpperCase()}</small></div></td><td>${item.country}</td><td><span class="badge">${item.currency}</span></td><td>${item.owner}</td><td class="right"><strong>${won.format(item.monthlySales)}</strong></td><td>${badge(item.status)}</td><td><button class="row-action" ${canEdit ? "" : "disabled title=\"다른 담당자의 거래처는 수정할 수 없습니다\""}>수정</button></td></tr>`;
    }).join("");
  }

  function renderProducts() {
    $("#product-table").innerHTML = data.products.map((item, index) => `<tr><td><strong>${item.code}</strong></td><td>${item.name}</td><td><span class="badge item-type ${item.type === "상품" ? "goods" : "product"}">${item.type}</span></td><td>${item.spec}</td><td>${index + 2}개</td><td>${badge(item.status)}</td><td>2026-09-${String(28 - index).padStart(2, "0")}</td><td><button class="row-action">수정</button></td></tr>`).join("");
    $("#price-table").innerHTML = data.prices.map((item) => `<tr><td><strong>${item.customer}</strong></td><td>${item.product}</td><td><span class="badge">${item.currency}</span></td><td class="right"><strong>${formatAmount(item.currency, item.amount)}</strong></td><td>${item.start}</td><td>${item.end}</td><td>${badge(item.status)}</td><td><button class="row-action">수정</button></td></tr>`).join("");
  }

  function renderShipments() {
    $("#shipment-table").innerHTML = data.shipments.map((item) => {
      const canEdit = state.role === "manager" || item.ownerId === state.currentUserId;
      return `<tr><td>${item.date}</td><td><strong>${item.number}</strong></td><td><div class="subcell"><strong>${item.customer}</strong><small>${item.product}</small></div></td><td class="right">${number.format(item.quantity)}</td><td class="right">${formatAmount(item.currency, item.unitPrice)}</td><td class="right">${formatAmount(item.currency, item.originalAmount)}</td><td>${number.format(item.rate)}</td><td class="right"><strong>${won.format(item.krwAmount)}</strong></td><td>${badge(item.status)}</td><td><button class="row-action" ${canEdit ? "" : "disabled"}>수정</button></td></tr>`;
    }).join("");
  }

  function networkCompany(id) {
    return data.networkCompanies.find((item) => item.id === id);
  }

  function networkFilters() {
    return {
      lifecycle: $("#network-lifecycle-filter")?.value || "all",
      ownerId: $("#network-owner-filter")?.value || "all",
      status: $("#network-status-filter")?.value || "all",
      search: ($("#network-search")?.value || "").trim().toLowerCase()
    };
  }

  function networkMatches(company, filters) {
    if (!company) return false;
    if (filters.lifecycle === "oem" && !company.isOem) return false;
    if (filters.lifecycle === "customer" && (company.isOem || company.lifecycleType !== "customer")) return false;
    if (filters.lifecycle === "prospect" && company.lifecycleType !== "prospect") return false;
    if (filters.ownerId !== "all" && company.ownerId !== filters.ownerId) return false;
    if (filters.status !== "all" && company.status !== filters.status) return false;
    return !filters.search || `${company.name} ${company.code}`.toLowerCase().includes(filters.search);
  }

  function networkLevel(path) {
    return path.length;
  }

  function renderNetworkTree() {
    const tree = $("#network-tree");
    if (!tree) return;
    const root = networkCompany(state.networkRootId) || data.networkCompanies.find((item) => item.isOem);
    const filters = networkFilters();

    function branch(companyId, path) {
      if (path.includes(companyId)) return "";
      const company = networkCompany(companyId);
      if (!company) return "";
      const children = data.networkRelationships.filter((item) => item.upstreamId === companyId && item.status === "active");
      const childMarkup = children.map((relationship) => branch(relationship.downstreamId, [...path, companyId])).filter(Boolean).join("");
      const visible = (company.id === root.id && filters.lifecycle === "all" && filters.ownerId === "all" && filters.status === "all" && !filters.search) || networkMatches(company, filters) || childMarkup;
      if (!visible) return "";
      const active = company.id === state.selectedNetworkCompanyId ? " active" : "";
      const typeClass = company.lifecycleType === "prospect" ? " prospect" : "";
      const isFiltering = filters.lifecycle !== "all" || filters.ownerId !== "all" || filters.status !== "all" || filters.search;
      const collapsed = state.networkCollapsedIds.includes(company.id) && !isFiltering;
      const childList = childMarkup && !collapsed ? `<ul class="network-branch">${childMarkup}</ul>` : "";
      const level = networkLevel(path);
      const levelLabel = company.isOem ? "원청" : `${level}차 협력사`;
      const toggle = children.length ? `<button class="network-toggle" data-network-toggle="${company.id}" aria-label="${company.name} ${collapsed ? "펼치기" : "접기"}">${collapsed ? "▸" : "▾"}</button>` : `<span class="network-toggle-placeholder"></span>`;
      return `<li><div class="network-node-row">${toggle}<button class="network-node${active}${typeClass}" data-network-node="${company.id}"><span class="network-node-icon">${company.isOem ? "OEM" : company.name.slice(0, 1)}</span><span class="network-node-copy"><strong>${company.name}</strong><small>${company.code} · ${levelLabel} · ${company.lifecycleType === "prospect" ? "잠재고객" : "거래처"}</small></span><span class="network-node-owner">${company.owner}</span></button></div>${childList}</li>`;
    }

    tree.innerHTML = root ? `<ul class="network-branch">${branch(root.id, [])}</ul>` : `<div class="network-empty">표시할 영업망이 없습니다.</div>`;
    if (!tree.textContent.trim()) tree.innerHTML = `<div class="network-empty">조건에 맞는 기업이 없습니다.</div>`;
    $("#network-company-count").textContent = `${tree.querySelectorAll("[data-network-node]").length}개 기업`;
  }

  function toggleNetworkBranch(companyId) {
    state.networkCollapsedIds = state.networkCollapsedIds.includes(companyId)
      ? state.networkCollapsedIds.filter((id) => id !== companyId)
      : [...state.networkCollapsedIds, companyId];
    renderNetworkTree();
  }

  function relationName(id) {
    return networkCompany(id)?.name || "-";
  }

  function relationshipProducts(relationship) {
    return relationship.productIds.map((id) => data.products.find((item) => item.id === id)?.name || id).join(", ");
  }

  function renderNetworkCompanyDetail() {
    const company = networkCompany(state.selectedNetworkCompanyId) || networkCompany(state.networkRootId);
    if (!company || !$("#network-company-detail")) return;
    state.selectedNetworkCompanyId = company.id;
    const canEdit = state.role === "manager" || company.ownerId === state.currentUserId;
    const relationships = data.networkRelationships.filter((item) => item.upstreamId === company.id || item.downstreamId === company.id);
    const contacts = data.networkContacts.filter((item) => item.companyId === company.id);
    const assignments = data.networkAssignments.filter((item) => item.companyId === company.id);
    const history = data.networkHistory.filter((item) => item.companyId === company.id);
    const lifecycleLabel = company.isOem ? "원청" : (company.lifecycleType === "prospect" ? "잠재고객" : "정식 거래처");

    $("#network-company-detail").innerHTML = `
      <div class="network-detail-head">
        <div class="network-detail-title"><span>${company.isOem ? "OEM" : company.name.slice(0, 1)}</span><div><h3>${company.name}<i class="network-lifecycle ${company.lifecycleType}">${lifecycleLabel}</i></h3><p>${company.code} · ${company.country} · ${company.industry}</p></div></div>
        <div class="network-detail-actions"><button class="button secondary" data-network-dialog="networkRelationship" ${canEdit ? "" : "disabled"}>관계 추가</button><button class="button primary" data-network-dialog="networkCompany" data-network-company-mode="edit" ${canEdit ? "" : "disabled"}>기업 수정</button></div>
      </div>
      <div class="network-detail-stats"><div><span>당사 담당자</span><strong>${company.owner}</strong></div><div><span>연결 관계</span><strong>${relationships.length}건</strong></div><div><span>외부 담당자</span><strong>${contacts.filter((item) => item.employmentStatus === "재직").length}명</strong></div><div><span>상태</span><strong>${company.status === "active" ? "활성" : "비활성"}</strong></div></div>`;

    $("#network-basic-tab").innerHTML = `<dl class="network-info-grid"><div><dt>기업 코드</dt><dd>${company.code}</dd></div><div><dt>기업 구분</dt><dd>${lifecycleLabel}</dd></div><div><dt>국가</dt><dd>${company.country}</dd></div><div><dt>업종</dt><dd>${company.industry}</dd></div><div class="full"><dt>주소</dt><dd>${company.address}</dd></div><div class="full"><dt>관리 메모</dt><dd>${company.note}</dd></div></dl>`;
    $("#network-relationship-tab").innerHTML = `<div class="network-section-head"><h4>연결 관계 및 공급정보</h4><button class="row-action" data-network-dialog="networkRelationship" ${canEdit ? "" : "disabled"}>+ 관계 추가</button></div><div class="network-record-list">${relationships.length ? relationships.map((item) => {
      const direction = item.upstreamId === company.id ? `하위 협력사 · ${relationName(item.downstreamId)}` : `상위 기업 · ${relationName(item.upstreamId)}`;
      return `<div class="network-record"><strong>${direction}</strong><span>${item.validFrom} ~ ${item.validTo}</span><p><b>${relationshipProducts(item)}</b> / ${item.application}<br>${item.notes}</p></div>`;
    }).join("") : `<div class="network-empty">등록된 관계가 없습니다.</div>`}</div>`;
    $("#network-contact-tab").innerHTML = `<div class="network-section-head"><h4>거래처 담당자</h4><button class="row-action" data-network-dialog="networkContact" ${canEdit ? "" : "disabled"}>+ 담당자 추가</button></div><div class="network-contact-grid">${contacts.length ? contacts.map((item) => `<article class="network-contact ${item.employmentStatus === "재직" ? "" : "inactive"}"><h5>${item.name} · ${item.position}</h5><span>${item.department} / ${item.role} · ${item.employmentStatus}</span><dl><div><dt>휴대폰</dt><dd>${item.mobile}</dd></div><div><dt>이메일</dt><dd>${item.email}</dd></div><div><dt>재직기간</dt><dd>${item.validFrom} ~ ${item.validTo}</dd></div></dl></article>`).join("") : `<div class="network-empty">등록된 담당자가 없습니다.</div>`}</div>`;
    $("#network-assignment-tab").innerHTML = `<div class="network-section-head"><h4>당사 담당 이력</h4>${state.role === "manager" ? `<button class="row-action" data-network-dialog="networkAssignment">담당자 변경</button>` : ""}</div><div class="network-record-list">${assignments.length ? assignments.map((item) => `<div class="network-record"><strong>${item.owner}${item.current ? " · 현재 담당" : ""}</strong><span>${item.validFrom} ~ ${item.validTo}</span><p>${item.reason}</p></div>`).join("") : `<div class="network-empty">등록된 담당 이력이 없습니다.</div>`}</div>`;
    $("#network-history-tab").innerHTML = `<div class="network-section-head"><h4>변경 이력</h4></div><div class="network-record-list">${history.length ? history.map((item) => `<div class="network-record"><strong>${item.action}</strong><span>${item.at} · ${item.user}</span><p>${item.detail}</p></div>`).join("") : `<div class="network-empty">변경 이력이 없습니다.</div>`}</div>`;

    const convertButton = $("#convert-prospect");
    convertButton.classList.toggle("is-hidden", state.role !== "manager" || company.lifecycleType !== "prospect");
    setNetworkTab(state.networkTab);
  }

  function selectNetworkCompany(companyId) {
    if (!networkCompany(companyId)) return;
    state.selectedNetworkCompanyId = companyId;
    renderNetworkTree();
    renderNetworkCompanyDetail();
  }

  function setNetworkTab(tab) {
    const tabMap = { basic: "network-basic-tab", relationships: "network-relationship-tab", contacts: "network-contact-tab", assignments: "network-assignment-tab", history: "network-history-tab" };
    state.networkTab = tabMap[tab] ? tab : "basic";
    $$('[data-network-tab]').forEach((button) => button.classList.toggle("active", button.dataset.networkTab === state.networkTab));
    $$(".network-tab-content").forEach((panel) => panel.classList.toggle("active", panel.id === tabMap[state.networkTab]));
  }

  const researchStatusLabels = {
    collected: "수집됨", enriching: "정보 보강 중", reviewPending: "검토 대기", duplicateSuspected: "중복 의심",
    onHold: "보류", excluded: "제외", approved: "승인", converted: "전환 완료"
  };

  function researchCandidate(candidateId) {
    return data.prospectCandidates.find((item) => item.id === candidateId);
  }

  function renderResearchDashboard() {
    const candidates = data.prospectCandidates;
    $("#research-kpi-new").textContent = candidates.filter((item) => ["collected", "enriching"].includes(item.status)).length;
    $("#research-kpi-review").textContent = candidates.filter((item) => ["reviewPending", "onHold"].includes(item.status)).length;
    $("#research-kpi-duplicate").textContent = candidates.filter((item) => item.status === "duplicateSuspected").length;
    $("#research-kpi-approved").textContent = candidates.filter((item) => ["approved", "converted"].includes(item.status)).length;
  }

  function filteredResearchCandidates() {
    const query = state.researchSearch.toLowerCase();
    return data.prospectCandidates.filter((item) => {
      const text = `${item.name} ${item.domain} ${item.application}`.toLowerCase();
      return (state.researchStatus === "all" || item.status === state.researchStatus)
        && (state.researchRegion === "all" || item.region === state.researchRegion)
        && (state.researchOwner === "all" || item.ownerId === state.researchOwner)
        && (!query || text.includes(query));
    });
  }

  function renderResearchCandidates() {
    const rows = filteredResearchCandidates();
    $("#research-result-count").textContent = `${rows.length}개 기업`;
    $("#research-candidate-table").innerHTML = rows.map((item) => `
      <tr class="${item.id === state.selectedResearchCandidateId ? "selected" : ""}" data-research-candidate="${item.id}">
        <td><div class="subcell"><strong>${item.name}</strong><small>${item.domain}</small></div></td>
        <td><div class="subcell"><strong>${item.country}</strong><small>${item.industry}</small></div></td>
        <td><span class="research-score ${item.priority}">${item.score}</span></td><td>${item.owner}</td>
        <td><span class="badge research-status ${item.status}">${researchStatusLabels[item.status]}</span></td>
      </tr>`).join("") || `<tr><td colspan="5"><div class="network-empty">조건에 맞는 후보가 없습니다.</div></td></tr>`;
  }

  function renderDuplicateReview() {
    const candidate = researchCandidate(state.selectedResearchCandidateId);
    const panel = $("#research-duplicate-review");
    const visible = candidate?.status === "duplicateSuspected";
    panel.classList.toggle("is-hidden", !visible);
    if (!visible) { panel.innerHTML = ""; return; }
    panel.innerHTML = `<div class="panel-head"><div><h3>중복 후보 비교</h3><p>자동 병합하지 않고 담당자가 판정합니다.</p></div><span class="badge duplicateSuspected">확인 필요</span></div><div class="duplicate-compare"><div><small>신규 후보</small><strong>${candidate.name}</strong><span>${candidate.country} · ${candidate.domain}</span></div><b>↔</b><div><small>기존 영업망</small><strong>${candidate.duplicateName}</strong><span>유럽 · horizon-controls.example</span></div></div><div class="research-actions"><button class="button secondary" data-duplicate-action="dismiss">기존 기업과 중복</button><button class="button primary" data-duplicate-action="new">별도 신규 후보</button></div>`;
  }

  function renderResearchCandidateDetail() {
    const candidate = researchCandidate(state.selectedResearchCandidateId) || filteredResearchCandidates()[0];
    if (!candidate) { $("#research-candidate-detail").innerHTML = `<div class="network-empty">후보를 선택하세요.</div>`; return; }
    state.selectedResearchCandidateId = candidate.id;
    const evidence = data.researchEvidence.filter((item) => item.candidateId === candidate.id);
    const ownerOptions = data.users.filter((item) => item.role === "member").map((item) => `<option value="${item.id}" ${item.id === candidate.ownerId ? "selected" : ""}>${item.name}</option>`).join("");
    const managerActions = state.role === "manager" ? `<button class="button secondary" data-research-action="excluded">제외</button><button class="button secondary" data-research-action="approved" ${candidate.status === "duplicateSuspected" ? "disabled" : ""}>승인</button>${candidate.status === "approved" ? `<button class="button primary" data-research-action="converted">영업망 전환</button>` : ""}` : "";
    $("#research-candidate-detail").innerHTML = `
      <div class="research-detail-head"><div><p class="eyebrow">${candidate.country} · ${candidate.industry}</p><h3>${candidate.name}</h3><a href="${candidate.website}" target="_blank" rel="noreferrer">${candidate.domain} ↗</a></div><div class="research-score-ring" style="--score:${candidate.score}"><strong>${candidate.score}</strong><small>/ 100</small></div></div>
      <div class="research-detail-meta"><div><span>기업규모</span><strong>${candidate.size}</strong></div><div><span>예상 적용</span><strong>${candidate.application}</strong></div><div><span>조사일</span><strong>${candidate.researchedAt}</strong></div></div>
      <div class="research-score-breakdown">${["제품 적용", "업종", "공급망", "지역", "규모", "신뢰도"].map((label, index) => `<div><span>${label}</span><b>${candidate.scoreParts[index]}점</b></div>`).join("")}</div>
      <section class="research-ai"><span>AI 적합성 요약</span><p>${candidate.aiSummary}</p><small>주의: ${candidate.aiRisk}</small></section>
      <section class="research-evidence"><div class="network-section-head"><h4>조사 근거 ${evidence.length}건</h4><span>출처·조사일 필수</span></div>${evidence.length ? evidence.map((item) => `<a href="${item.url}" target="_blank" rel="noreferrer"><strong>${item.title}</strong><small>${item.observedAt} · 신뢰도 ${item.confidence}</small></a>`).join("") : `<p class="inline-warning">출처가 없어 승인할 수 없습니다.</p>`}</section>
      <label class="research-owner">담당자<select id="research-owner-select" ${state.role === "manager" ? "" : "disabled"}>${ownerOptions}</select></label>
      <div class="research-actions"><button class="button secondary" data-research-action="onHold">보류</button>${managerActions}</div>`;
    renderDuplicateReview();
  }

  function selectResearchCandidate(candidateId) {
    if (!researchCandidate(candidateId)) return;
    state.selectedResearchCandidateId = candidateId;
    renderResearchCandidates();
    renderResearchCandidateDetail();
  }

  function assignResearchCandidate(ownerId) {
    const candidate = researchCandidate(state.selectedResearchCandidateId);
    const owner = data.users.find((item) => item.id === ownerId);
    if (!candidate || !owner || state.role !== "manager") return;
    candidate.ownerId = owner.id; candidate.owner = owner.name;
    renderResearchCandidates(); renderResearchCandidateDetail(); showToast(`${owner.name} 담당으로 배정했습니다. (목업)`);
  }

  function reviewResearchCandidate(nextStatus) {
    const candidate = researchCandidate(state.selectedResearchCandidateId);
    if (!candidate) return;
    if (["approved", "converted", "excluded"].includes(nextStatus) && state.role !== "manager") return;
    if (nextStatus === "approved" && (candidate.status === "duplicateSuspected" || !data.researchEvidence.some((item) => item.candidateId === candidate.id))) { showToast("중복 판정과 출처 확인 후 승인할 수 있습니다."); return; }
    candidate.status = nextStatus;
    if (nextStatus === "converted" && !candidate.networkCompanyId) {
      candidate.networkCompanyId = `nc-research-${candidate.id}`;
      data.networkCompanies.push({ id: candidate.networkCompanyId, name: candidate.name, code: "P-AUTO", country: candidate.country, industry: candidate.industry, lifecycleType: "prospect", isOem: false, ownerId: candidate.ownerId, owner: candidate.owner, status: "active", address: "-", note: "잠재고객 리서치 승인 전환" });
    }
    renderResearchDashboard(); renderResearchCandidates(); renderResearchCandidateDetail();
    showToast(`${candidate.name}: ${researchStatusLabels[nextStatus]} 처리했습니다. (목업)`);
  }

  function resolveResearchDuplicate(decision) {
    const candidate = researchCandidate(state.selectedResearchCandidateId);
    if (!candidate || candidate.status !== "duplicateSuspected") return;
    candidate.status = decision === "new" ? "reviewPending" : "excluded";
    renderResearchDashboard(); renderResearchCandidates(); renderResearchCandidateDetail();
    showToast(decision === "new" ? "별도 신규 후보로 유지합니다." : "기존 기업과 중복으로 제외했습니다.");
  }

  function renderResearchRuns() {
    $("#research-run-list").innerHTML = data.researchRuns.map((item) => `<div class="research-run"><span class="research-run-kind">${{ scheduled: "예약", manual: "수동", excel: "Excel" }[item.kind]}</span><div><strong>${item.profile}</strong><small>${item.startedAt} · 발견 ${item.discovered} · 보강 ${item.enriched} · 실패 ${item.failed}</small></div><b>${item.apiCalls}회<small>₩${number.format(item.estimatedCost * 1000)}</small></b></div>`).join("");
  }

  function startResearchRun() {
    data.researchRuns.unshift({ id: `rr${data.researchRuns.length + 1}`, kind: "manual", profile: "글로벌 스위치 적용기업", startedAt: "2026-10-07 20:10", completedAt: "2026-10-07 20:12", status: "completed", discovered: 7, enriched: 6, failed: 1, apiCalls: 14, estimatedCost: 3.1 });
    renderResearchRuns(); showToast("즉시 조사를 완료했습니다. 신규 후보 6건을 검토하세요. (목업)");
  }

  function openProspectImport(open = true) {
    $("#research-import-panel").classList.toggle("is-hidden", !open);
  }

  function renderTargets() {
    $("#target-table").innerHTML = data.targets.map((item) => {
      const rate = Math.round((item.actual / item.target) * 1000) / 10;
      return `<tr><td><strong>${item.owner}</strong></td><td class="right">${won.format(item.target)}</td><td class="right"><strong>${won.format(item.actual)}</strong></td><td><div class="achievement"><div class="progress"><span style="width:${Math.min(rate, 100)}%"></span></div><span>${rate}%</span></div></td><td class="right">${won.format(item.target - item.actual)}</td></tr>`;
    }).join("");
  }

  function renderUsers() {
    $("#user-table").innerHTML = data.users.map((item, index) => `<tr><td><div class="subcell"><strong>${item.name}</strong><small>${item.title}</small></div></td><td>${item.loginId}</td><td><span class="badge ${item.role}">${item.role === "manager" ? "팀장" : "팀원"}</span></td><td>${data.customers.filter((customer) => customer.ownerId === item.id).length}개</td><td>${item.status === "active" ? `2026-10-01 ${String(9 - index).padStart(2, "0")}:12` : "-"}</td><td>${badge(item.status)}</td><td><button class="row-action">관리</button></td></tr>`).join("");
    $("#audit-table").innerHTML = data.audits.map((item) => `<tr><td>${item.at}</td><td><strong>${item.user}</strong></td><td><span class="badge">${item.action}</span></td><td>${item.target}</td><td>${item.detail}</td></tr>`).join("");
  }

  function renderAll() {
    renderOwnerChart();
    renderDashboardShipments();
    renderCustomers();
    renderProducts();
    renderShipments();
    renderNetworkTree();
    renderNetworkCompanyDetail();
    renderResearchDashboard();
    renderResearchCandidates();
    renderResearchCandidateDetail();
    renderResearchRuns();
    renderTargets();
    renderUsers();
  }

  const dialogTemplates = {
    shipment: {
      eyebrow: "NEW SHIPMENT", title: "출고 등록", body: `
        <label>출고일<input type="date" value="2026-10-01"></label><label>출고번호<input value="SO-261001-05"></label>
        <label class="full">거래처<select><option>대성전자 · 박태원</option><option>上海精密科技 · 이소연</option></select></label>
        <label>품목<select><option>Tact Switch 6×6</option><option>Rotary Switch</option></select></label><label>수량<input id="dialog-quantity" type="number" value="10000"></label>
        <div class="calculation-box"><div><span>적용단가</span><strong>₩142.00</strong></div><div><span>월 지정환율</span><strong>1.00</strong></div><div><span>원화 환산</span><strong id="dialog-total">₩1,420,000</strong></div></div>`
    },
    customer: { eyebrow: "NEW CUSTOMER", title: "거래처 등록", body: `<label>거래처 코드<input value="C-1006"></label><label>상태<select><option>사용 중</option></select></label><label class="full">회사명<input placeholder="정식 회사명"></label><label>국가<select><option>대한민국</option><option>미국</option><option>중국</option></select></label><label>기본 통화<select><option>KRW</option><option>USD</option><option>CNY</option></select></label><label class="full">담당자<select><option>박태원</option><option>최명철</option><option>이소연</option><option>이창선</option><option>이재학</option></select></label>` },
    product: { eyebrow: "NEW MASTER DATA", title: "품목 등록", body: `<label>품목 코드<input value="SW-"></label><label>구분<select><option>제품</option><option>상품</option></select></label><label class="full">품명<input placeholder="품목명"></label><label>상태<select><option>사용 중</option><option>사용 중지</option></select></label><label>규격<input placeholder="정격, 타입, 실장방식 등"></label>` },
    rate: { eyebrow: "MONTHLY RATE", title: "10월 적용환율 수정", body: `<label>기준 월<input type="month" value="2026-10" disabled></label><label>통화<select><option>USD</option><option>CNY</option></select></label><label class="full">지정환율<input type="number" value="1350.00"></label><label class="full">변경 사유<input placeholder="감사 이력에 기록됩니다."></label>` },
    target: { eyebrow: "MONTHLY TARGET", title: "10월 목표 편집", body: `<label>이재학<input value="100000000"></label><label>박태원<input value="220000000"></label><label>최명철<input value="180000000"></label><label>이소연<input value="160000000"></label><label class="full">이창선<input value="140000000"></label>` },
    user: { eyebrow: "NEW USER", title: "사용자 추가", body: `<label>성명<input placeholder="성명"></label><label>직위<input placeholder="직위"></label><label>로그인 ID<input placeholder="영문 ID"></label><label>역할<select><option>팀원</option><option>팀장</option></select></label><label class="full">초기 비밀번호<input type="password" value="Temp!2026"></label>` },
    networkRelationship: { eyebrow: "NETWORK RELATION", title: "협력 관계 등록", body: `<label>상위 기업<select><option>삼성전자</option><option>LG전자</option><option>대성전자</option></select></label><label>하위 협력사<select><option>대성전자</option><option>세진모듈</option><option>한빛정밀</option></select></label><label>적용 시작일<input type="date" value="2026-10-01"></label><label>적용 종료일<input id="network-relationship-end" type="date"></label><label>상태<select id="network-relationship-status"><option value="active">활성</option><option value="inactive">종료</option></select></label><label>공급 품목<select multiple><option value="p1">Tact Switch 6×6</option><option value="p2">Micro Switch</option><option value="p3">Rotary Switch</option><option value="p4">Slide Switch</option><option value="p5">Detector Switch</option></select></label><label class="full">적용 제품·프로젝트<input placeholder="적용처"></label><label class="full">관계 메모<input placeholder="협력 및 공급 메모"></label>` },
    networkContact: { eyebrow: "CUSTOMER CONTACT", title: "거래처 담당자 등록", body: `<label>성명<input placeholder="성명"></label><label>직위<input placeholder="직위"></label><label>부서<input placeholder="부서"></label><label>담당 역할<input placeholder="구매, 개발, 품질 등"></label><label>휴대폰<input placeholder="010-0000-0000"></label><label>이메일<input type="email" placeholder="name@company.com"></label><label>담당 시작일<input type="date" value="2026-10-01"></label><label>담당 종료일<input id="network-contact-end" type="date"></label><label class="full">재직 상태<select><option>재직</option><option>퇴사</option></select></label>` },
    networkAssignment: { eyebrow: "INTERNAL OWNER", title: "당사 담당자 변경", body: `<label class="full">새 담당자<select><option>박태원</option><option>최명철</option><option>이소연</option><option>이창선</option><option>이재학</option></select></label><label>적용 시작일<input type="date" value="2026-10-01"></label><label>기존 담당 종료일<input type="date" value="2026-09-30"></label><label class="full">변경 사유<input placeholder="변경 사유는 이력에 기록됩니다."></label>` },
    networkConversion: { eyebrow: "PROSPECT CONVERSION", title: "정식 거래처 전환", body: `<label>거래처 코드<input id="network-convert-code" value="C-"></label><label>기본 통화<select id="network-convert-currency"><option>KRW</option><option>USD</option><option>CNY</option></select></label><label class="full">전환 확인<input value="기존 관계와 담당자 이력을 유지합니다." disabled></label>` }
  };

  function networkCompanyDialogTemplate() {
    const editing = state.networkCompanyMode === "edit";
    const selected = editing ? networkCompany(state.selectedNetworkCompanyId) : null;
    const currentUser = data.users.find((item) => item.id === state.currentUserId);
    const memberLocked = state.role === "member";
    const ownerLocked = editing || memberLocked;
    const typeLocked = editing || memberLocked;
    const company = selected || { code: "P-", name: "", country: "대한민국", industry: "", lifecycleType: "prospect", isOem: false, ownerId: currentUser.id, status: "active", address: "", note: "" };
    const lifecycle = company.isOem ? "oem" : company.lifecycleType;
    const typeField = typeLocked
      ? `<input value="${lifecycle === "prospect" ? "잠재 협력사" : lifecycle === "customer" ? "정식 거래처" : "원청"}" disabled><input id="network-company-type" type="hidden" value="${lifecycle}">`
      : `<select id="network-company-type"><option value="prospect" ${lifecycle === "prospect" ? "selected" : ""}>잠재 협력사</option><option value="customer" ${lifecycle === "customer" ? "selected" : ""}>정식 거래처</option><option value="oem" ${lifecycle === "oem" ? "selected" : ""}>원청사</option></select>`;
    const ownerOptions = data.users.map((item) => `<option value="${item.id}" ${item.id === company.ownerId ? "selected" : ""}>${item.name}</option>`).join("");
    return {
      eyebrow: "NETWORK COMPANY",
      title: editing ? "영업망 기업 수정" : "영업망 기업 등록",
      body: `<label>기업 코드<input id="network-company-code" value="${company.code}"></label><label>구분${typeField}</label><label class="full">기업명<input id="network-company-name" value="${company.name}" placeholder="정식 회사명"></label><label>국가<input id="network-company-country" value="${company.country}"></label><label>업종<input id="network-company-industry" value="${company.industry}" placeholder="업종"></label><label>당사 담당자<select id="network-company-owner" ${ownerLocked ? "disabled" : ""}>${ownerOptions}</select><small>${editing ? "담당자 변경은 ‘당사 담당 이력’ 탭에서 진행합니다." : ""}</small></label><label>상태<select id="network-company-status"><option value="active" ${company.status === "active" ? "selected" : ""}>활성</option><option value="inactive" ${company.status === "inactive" ? "selected" : ""}>비활성</option></select></label><label class="full">주소<input id="network-company-address" value="${company.address}" placeholder="주소"></label><label class="full">관리 메모<input id="network-company-note" value="${company.note}" placeholder="영업 메모"></label>`
    };
  }

  function openDialog(type) {
    state.dialogType = type;
    const template = type === "networkCompany" ? networkCompanyDialogTemplate() : (dialogTemplates[type] || dialogTemplates.shipment);
    $("#dialog-eyebrow").textContent = template.eyebrow;
    $("#dialog-title").textContent = template.title;
    $("#dialog-body").innerHTML = template.body;
    $("#dialog-backdrop").classList.remove("is-hidden");
    const quantity = $("#dialog-quantity");
    if (quantity) quantity.addEventListener("input", () => $("#dialog-total").textContent = won.format((Number(quantity.value) || 0) * 142));
  }

  function closeDialog() { $("#dialog-backdrop").classList.add("is-hidden"); state.dialogType = null; }

  function openProspectConversion() {
    const company = networkCompany(state.selectedNetworkCompanyId);
    if (!company || state.role !== "manager" || company.lifecycleType !== "prospect") return;
    openDialog("networkConversion");
  }

  function completeProspectConversion() {
    const company = networkCompany(state.selectedNetworkCompanyId);
    const code = $("#network-convert-code").value.trim();
    const currency = $("#network-convert-currency").value;
    if (!company || !code) { showToast("거래처 코드를 입력하세요."); return; }
    company.lifecycleType = "customer";
    company.customerId = `mock-${code}`;
    company.code = code;
    company.defaultCurrency = currency;
    data.networkHistory.unshift({ companyId: company.id, at: "2026-10-06 14:00", user: "이재학", action: "정식 거래처 전환", detail: `${code} / ${currency}로 전환` });
    closeDialog();
    renderNetworkTree();
    renderNetworkCompanyDetail();
    showToast(`${company.name}을 정식 거래처로 전환했습니다. (목업)`);
  }

  function completeNetworkCompany() {
    const editing = state.networkCompanyMode === "edit";
    const existing = editing ? networkCompany(state.selectedNetworkCompanyId) : null;
    const currentUser = data.users.find((item) => item.id === state.currentUserId);
    const name = $("#network-company-name").value.trim();
    if (!name) { showToast("기업명을 입력하세요."); return; }
    const type = $("#network-company-type").value;
    const ownerId = existing ? existing.ownerId : (state.role === "member" ? state.currentUserId : $("#network-company-owner").value);
    const owner = data.users.find((item) => item.id === ownerId)?.name || currentUser.name;
    const values = { code: $("#network-company-code").value.trim(), name, country: $("#network-company-country").value.trim(), industry: $("#network-company-industry").value.trim(), lifecycleType: type === "oem" ? "customer" : type, isOem: type === "oem", ownerId, owner, status: $("#network-company-status").value, address: $("#network-company-address").value.trim(), note: $("#network-company-note").value.trim() };
    let company = existing;
    if (company) Object.assign(company, values);
    else {
      company = { id: `nc-new-${data.networkCompanies.length + 1}`, ...values };
      data.networkCompanies.push(company);
      data.networkAssignments.push({ id: `na-new-${data.networkAssignments.length + 1}`, companyId: company.id, ownerId, owner, validFrom: "2026-10-06", validTo: "-", reason: "신규 등록", current: true });
      data.networkHistory.push({ companyId: company.id, at: "2026-10-06 14:00", user: currentUser.name, action: "잠재 협력사 등록", detail: `${owner} 담당으로 등록` });
    }
    closeDialog();
    selectNetworkCompany(company.id);
    showToast(`${company.name} 정보를 반영했습니다. (목업)`);
  }

  function showToast(message) {
    $("#toast-message").textContent = message;
    $("#toast").classList.remove("is-hidden");
    window.clearTimeout(showToast.timer);
    showToast.timer = window.setTimeout(() => $("#toast").classList.add("is-hidden"), 2600);
  }

  function updateImportStep(nextStep) {
    state.importStep = Math.max(1, Math.min(5, nextStep));
    $$(".import-step").forEach((step) => step.classList.toggle("active", Number(step.dataset.importStep) === state.importStep));
    $$("#import-stepper > div").forEach((step, index) => step.classList.toggle("active", index + 1 <= state.importStep));
    $$("#import-stepper > i").forEach((line, index) => line.classList.toggle("done", index + 1 < state.importStep));
    $("#import-prev").disabled = state.importStep === 1;
    const nextButton = $("#import-next");
    nextButton.textContent = state.importStep === 5 ? "최종 반영" : "다음";
    nextButton.disabled = state.importStep === 5 && !$("#confirm-import").checked;
  }

  $("#login-form").addEventListener("submit", (event) => { event.preventDefault(); login($("#login-id").value === "manager" ? "manager" : "member"); });
  $$('[data-demo-role]').forEach((button) => button.addEventListener("click", () => login(button.dataset.demoRole)));
  $("#toggle-password").addEventListener("click", () => { const input = $("#login-password"); input.type = input.type === "password" ? "text" : "password"; });
  $$(".nav-item").forEach((button) => button.addEventListener("click", () => showView(button.dataset.view)));
  $$('[data-view-link]').forEach((button) => button.addEventListener("click", () => showView(button.dataset.viewLink)));
  $$('[data-role-switch]').forEach((button) => button.addEventListener("click", () => { state.role = button.dataset.roleSwitch; state.currentUserId = state.role === "manager" ? "u1" : "u2"; applyRole(); showToast(`${state.role === "manager" ? "팀장" : "팀원"} 권한 화면으로 전환했습니다.`); }));
  $("#menu-toggle").addEventListener("click", () => $("#sidebar").classList.toggle("open"));
  $$('[data-action="open-dialog"]').forEach((button) => button.addEventListener("click", () => openDialog(button.dataset.dialog)));
  $("#dialog-close").addEventListener("click", closeDialog);
  $("#dialog-cancel").addEventListener("click", closeDialog);
  $("#dialog-backdrop").addEventListener("click", (event) => { if (event.target === event.currentTarget) closeDialog(); });
  $("#dialog-save").addEventListener("click", () => {
    if (state.dialogType === "networkConversion") { completeProspectConversion(); return; }
    if (state.dialogType === "networkCompany") { completeNetworkCompany(); return; }
    closeDialog();
    showToast("목업에서는 저장 결과만 표시합니다.");
  });
  $$('[data-action="refresh"]').forEach((button) => button.addEventListener("click", () => showToast("선택한 조건으로 현황을 갱신했습니다.")));
  $$('[data-action="export"]').forEach((button) => button.addEventListener("click", () => showToast("보고서 내보내기는 Front-end 단계에서 연결합니다.")));
  $$('[data-tab]').forEach((button) => button.addEventListener("click", () => { $$('[data-tab]').forEach((item) => item.classList.remove("active")); $$(".tab-content").forEach((item) => item.classList.remove("active")); button.classList.add("active"); $(`#tab-${button.dataset.tab}`).classList.add("active"); }));
  $$('[data-admin-tab]').forEach((button) => button.addEventListener("click", () => { $$('[data-admin-tab]').forEach((item) => item.classList.remove("active")); $$(".admin-content").forEach((item) => item.classList.remove("active")); button.classList.add("active"); $(`#admin-${button.dataset.adminTab}`).classList.add("active"); }));
  $("#choose-file").addEventListener("click", () => updateImportStep(2));
  $("#import-prev").addEventListener("click", () => updateImportStep(state.importStep - 1));
  $("#import-next").addEventListener("click", () => { if (state.importStep < 5) updateImportStep(state.importStep + 1); else showToast("125건을 반영할 준비가 완료되었습니다. (목업)"); });
  $("#confirm-import").addEventListener("change", () => updateImportStep(5));
  $("#drop-zone").addEventListener("dragover", (event) => { event.preventDefault(); event.currentTarget.classList.add("dragging"); });
  $("#drop-zone").addEventListener("dragleave", (event) => event.currentTarget.classList.remove("dragging"));
  $("#drop-zone").addEventListener("drop", (event) => { event.preventDefault(); event.currentTarget.classList.remove("dragging"); updateImportStep(2); });
  $("#network-root-filter").addEventListener("change", (event) => {
    state.networkRootId = event.target.value;
    state.selectedNetworkCompanyId = event.target.value;
    renderNetworkTree();
    renderNetworkCompanyDetail();
  });
  ["#network-lifecycle-filter", "#network-owner-filter", "#network-status-filter"].forEach((selector) => $(selector).addEventListener("change", renderNetworkTree));
  $("#network-search").addEventListener("input", renderNetworkTree);
  $("#view-network").addEventListener("click", (event) => {
    const node = event.target.closest("[data-network-node]");
    const toggle = event.target.closest("[data-network-toggle]");
    const tab = event.target.closest("[data-network-tab]");
    const dialogButton = event.target.closest("[data-network-dialog]");
    if (toggle) { toggleNetworkBranch(toggle.dataset.networkToggle); return; }
    if (node) selectNetworkCompany(node.dataset.networkNode);
    if (tab) setNetworkTab(tab.dataset.networkTab);
    if (dialogButton && !dialogButton.disabled) {
      state.networkCompanyMode = dialogButton.dataset.networkCompanyMode || state.networkCompanyMode;
      openDialog(dialogButton.dataset.networkDialog);
    }
  });
  $("#convert-prospect").addEventListener("click", openProspectConversion);
  $("#research-search").addEventListener("input", (event) => { state.researchSearch = event.target.value; renderResearchCandidates(); });
  $("#research-region-filter").addEventListener("change", (event) => { state.researchRegion = event.target.value; renderResearchCandidates(); });
  $("#research-status-filter").addEventListener("change", (event) => { state.researchStatus = event.target.value; renderResearchCandidates(); });
  $("#research-owner-filter").addEventListener("change", (event) => { state.researchOwner = event.target.value; renderResearchCandidates(); });
  $("#research-run-now").addEventListener("click", startResearchRun);
  $("#research-import-open").addEventListener("click", () => openProspectImport(true));
  $("#research-import-close").addEventListener("click", () => openProspectImport(false));
  $("#research-import-apply").addEventListener("click", () => { openProspectImport(false); showToast("정상 후보 10건을 검토함에 반영했습니다. (목업)"); });
  $("#view-research").addEventListener("click", (event) => {
    const row = event.target.closest("[data-research-candidate]");
    const action = event.target.closest("[data-research-action]");
    const duplicate = event.target.closest("[data-duplicate-action]");
    if (row) selectResearchCandidate(row.dataset.researchCandidate);
    if (action && !action.disabled) reviewResearchCandidate(action.dataset.researchAction);
    if (duplicate) resolveResearchDuplicate(duplicate.dataset.duplicateAction);
  });
  $("#view-research").addEventListener("change", (event) => { if (event.target.id === "research-owner-select") assignResearchCandidate(event.target.value); });
  document.addEventListener("keydown", (event) => { if (event.key === "Escape") closeDialog(); });

  updateImportStep(1);
})();
