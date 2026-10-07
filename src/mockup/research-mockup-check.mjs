import { readFileSync } from "node:fs";

const read = (name) => readFileSync(new URL(name, import.meta.url), "utf8");
const html = read("index.html");
const css = read("styles.css");
const data = read("mock-data.js");
const app = read("app.js");

const requiredHtmlMarkers = [
  'data-view="research"',
  'id="research-view"',
  'id="research-kpi-grid"',
  'id="research-target-profile"',
  'id="research-candidate-table"',
  'id="research-candidate-detail"',
  'id="research-duplicate-review"',
  'id="research-run-panel"',
  'id="research-import-panel"',
];

for (const marker of requiredHtmlMarkers) {
  if (!html.includes(marker)) throw new Error(`Missing research HTML marker: ${marker}`);
}

for (const status of ["collected", "enriching", "reviewPending", "duplicateSuspected", "onHold", "excluded", "approved", "converted"]) {
  if (!html.includes(`value="${status}"`)) throw new Error(`Missing research status filter: ${status}`);
}

for (const marker of [
  "researchTargetProfiles",
  "researchRuns",
  "prospectCandidates",
  "researchEvidence",
  "대한민국",
  "독일",
  "미국",
  "중국",
  "인도",
  "베트남",
]) {
  if (!data.includes(marker)) throw new Error(`Missing research sample data: ${marker}`);
}

for (const marker of [
  "renderResearchDashboard",
  "renderResearchCandidates",
  "selectResearchCandidate",
  "assignResearchCandidate",
  "reviewResearchCandidate",
  "renderDuplicateReview",
  "startResearchRun",
  "openProspectImport",
]) {
  if (!app.includes(marker)) throw new Error(`Missing research interaction: ${marker}`);
}

for (const marker of [".research-layout", ".research-kpi-grid", ".research-candidate-table", ".research-detail", ".research-score-ring"]) {
  if (!css.includes(marker)) throw new Error(`Missing research style: ${marker}`);
}

console.log("RESEARCH_MOCKUP_CHECK=PASS");
