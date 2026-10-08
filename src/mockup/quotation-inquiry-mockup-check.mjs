import { readFileSync } from "node:fs";

const read = (name) => readFileSync(new URL(name, import.meta.url), "utf8");
const html = read("index.html");
const css = read("styles.css");
const data = read("mock-data.js");
const app = read("app.js");

const menuLabels = [...html.matchAll(/<button class="nav-item[^>]*data-view="[^"]+"[^>]*>[\s\S]*?<span>([^<]+)<\/span>(?:[\s\S]*?)<\/button>/g)]
  .map((match) => match[1]);
const expectedMenu = [
  "대시보드",
  "거래처 관리",
  "품목·단가 관리",
  "출고·매출 관리",
  "견적·고객문의",
  "환율·목표 관리",
  "영업망 관리",
  "잠재고객 리서치",
  "Excel 가져오기",
  "시스템 관리",
];

if (JSON.stringify(menuLabels) !== JSON.stringify(expectedMenu)) {
  throw new Error(`Menu order mismatch: ${menuLabels.join(" / ")}`);
}

for (const marker of [
  'data-view="quotes"',
  'id="view-quotes"',
  'data-quote-tab="overview"',
  'data-quote-tab="inquiries"',
  'data-quote-tab="quotations"',
  'id="quote-inquiry-table"',
  'id="quotation-table"',
  'id="quote-detail"',
]) {
  if (!html.includes(marker)) throw new Error(`Missing quotation HTML marker: ${marker}`);
}

for (const kpi of ["미답변 문의", "답변기한 초과", "승인 대기", "당월 견적", "수주·실주·보류", "수주율"]) {
  if (!html.includes(kpi)) throw new Error(`Missing quotation KPI: ${kpi}`);
}

for (const status of ["received", "assigned", "responding", "quoteDrafting", "answered", "closed"]) {
  if (!data.includes(`status: "${status}"`)) throw new Error(`Missing inquiry status sample: ${status}`);
}

for (const status of ["draft", "approvalRequested", "approved", "rejected", "dispatched", "won", "lost", "onHold"]) {
  if (!data.includes(`status: "${status}"`)) throw new Error(`Missing quotation status sample: ${status}`);
}

for (const marker of [
  "window.quotationMockData",
  "SWK-202610-001",
  "Rev.0",
  "Rev.1",
  'currency: "KRW"',
  'currency: "USD"',
  'currency: "CNY"',
  'tradeType: "domestic"',
  'tradeType: "export"',
  "Incoterms",
]) {
  if (!data.includes(marker)) throw new Error(`Missing quotation mock data: ${marker}`);
}

for (const action of [
  "select-tab",
  "select-inquiry",
  "add-activity",
  "create-quote",
  "add-line",
  "request-approval",
  "approve",
  "reject",
  "download-pdf-ko",
  "download-pdf-en",
  "record-dispatch",
  "mark-won",
  "mark-lost",
  "mark-on-hold",
]) {
  const interactionSource = `${html}\n${app}`;
  if (!interactionSource.includes(`data-quote-action="${action}"`) && !app.includes(`action === "${action}"`)) {
    throw new Error(`Missing quotation interaction: ${action}`);
  }
}

for (const marker of ["renderQuoteModule", "renderQuoteOverview", "renderInquiryList", "renderQuotationList", "calculateQuoteTotals"]) {
  if (!app.includes(marker)) throw new Error(`Missing quotation workflow function: ${marker}`);
}

for (const marker of [".quote-tabs", ".quote-kpi-grid", ".quote-workspace", ".quote-detail", "@media (max-width: 820px)"]) {
  if (!css.includes(marker)) throw new Error(`Missing quotation style: ${marker}`);
}

console.log("PASS quotation inquiry mockup checks");
