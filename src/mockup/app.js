(function () {
  "use strict";

  const data = window.SALES_MOCK_DATA;
  const state = { role: "manager", currentUserId: "u1", view: "dashboard", importStep: 1 };
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
    user: { eyebrow: "NEW USER", title: "사용자 추가", body: `<label>성명<input placeholder="성명"></label><label>직위<input placeholder="직위"></label><label>로그인 ID<input placeholder="영문 ID"></label><label>역할<select><option>팀원</option><option>팀장</option></select></label><label class="full">초기 비밀번호<input type="password" value="Temp!2026"></label>` }
  };

  function openDialog(type) {
    const template = dialogTemplates[type] || dialogTemplates.shipment;
    $("#dialog-eyebrow").textContent = template.eyebrow;
    $("#dialog-title").textContent = template.title;
    $("#dialog-body").innerHTML = template.body;
    $("#dialog-backdrop").classList.remove("is-hidden");
    const quantity = $("#dialog-quantity");
    if (quantity) quantity.addEventListener("input", () => $("#dialog-total").textContent = won.format((Number(quantity.value) || 0) * 142));
  }

  function closeDialog() { $("#dialog-backdrop").classList.add("is-hidden"); }

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
  $("#dialog-save").addEventListener("click", () => { closeDialog(); showToast("목업에서는 저장 결과만 표시합니다."); });
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
  document.addEventListener("keydown", (event) => { if (event.key === "Escape") closeDialog(); });

  updateImportStep(1);
})();
