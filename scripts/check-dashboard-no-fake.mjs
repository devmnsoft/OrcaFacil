import { readFile } from "node:fs/promises";

const files = [
  "src/OrcaFacil.Web/Pages/Dashboard/Index.cshtml",
  "src/OrcaFacil.Web/Pages/Dashboard/Index.cshtml.cs",
  "src/OrcaFacil.Web/Services/DashboardExperienceService.cs",
  "src/OrcaFacil.Web/wwwroot/css/dashboard.css"
];
const violations = [];
for (const file of files) {
  const source = await readFile(file, "utf8");
  if (/Math\.random\s*\(/i.test(source)) violations.push(`${file}: Math.random is forbidden in dashboard KPIs`);
  if (/\b(?:mock(?:ed)?|fake|placeholder)(?:Data|Kpi|Metric|Dashboard)\b/i.test(source)) violations.push(`${file}: fake or placeholder dashboard data detected`);
}
if (violations.length) { console.error(violations.join("\n")); process.exit(1); }
console.log("Dashboard real-data check passed.");
