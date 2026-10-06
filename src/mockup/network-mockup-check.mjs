import { readFileSync } from "node:fs";

const read = (name) => readFileSync(new URL(name, import.meta.url), "utf8");
const html = read("index.html");
const css = read("styles.css");
const data = read("mock-data.js");
const app = read("app.js");

const requiredHtmlMarkers = [
  'data-view="network"',
  'id="network-view"',
  'id="network-tree"',
  'id="network-company-detail"',
  'id="network-contact-tab"',
  'id="network-assignment-tab"',
  'id="network-history-tab"',
  'id="convert-prospect"',
];

for (const marker of requiredHtmlMarkers) {
  if (!html.includes(marker)) throw new Error(`Missing HTML marker: ${marker}`);
}

for (const marker of ["networkCompanies", "networkRelationships", "networkContacts", "networkAssignments", "삼성전자", "LG전자"]) {
  if (!data.includes(marker)) throw new Error(`Missing network sample data: ${marker}`);
}

for (const marker of ["renderNetworkTree", "renderNetworkCompanyDetail", "selectNetworkCompany", "setNetworkTab"]) {
  if (!app.includes(marker)) throw new Error(`Missing network interaction: ${marker}`);
}

for (const marker of ['id="network-status-filter"', '<option value="oem">원청</option>', 'id="network-company-count"']) {
  if (!html.includes(marker)) throw new Error(`Missing approved network filter/count: ${marker}`);
}

for (const marker of ["networkCollapsedIds", "toggleNetworkBranch", "networkLevel", "openProspectConversion", "network-convert-code", "network-convert-currency", "networkCompanyDialogTemplate"]) {
  if (!app.includes(marker)) throw new Error(`Missing approved network workflow: ${marker}`);
}

for (const marker of ["ownerLocked", "typeLocked", "network-relationship-end", "network-relationship-status", "network-contact-end", 'company.isOem ? "원청"']) {
  if (!app.includes(marker)) throw new Error(`Missing history-preserving workflow: ${marker}`);
}

if (!data.includes("productIds")) throw new Error("Network relationships must reference existing product IDs");
if (/\btier\s*:/.test(data)) throw new Error("Hierarchy tier must be computed, not stored");
if (!/id: "nc-d", name: "한빛오토텍"[^\n]+customerId: "c4"/.test(data)) throw new Error("Customer c4 network identity is inconsistent");

for (const marker of [".network-layout", ".network-tree", ".network-detail", "@media (max-width: 820px)"]) {
  if (!css.includes(marker)) throw new Error(`Missing network style: ${marker}`);
}

console.log("NETWORK_MOCKUP_CHECK=PASS");
