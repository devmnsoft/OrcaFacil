import { readFile } from "node:fs/promises";

const commercialPath = "src/OrcaFacil.Persistence/Services/CommercialWorkspaceQueryService.cs";
const experiencePath = "src/OrcaFacil.Web/Services/DashboardExperienceService.cs";
const [commercial, experience] = await Promise.all([readFile(commercialPath, "utf8"), readFile(experiencePath, "utf8")]);
const violations = [];
for (const [file, source] of [[commercialPath, commercial], [experiencePath, experience]]) {
  if (/Task\.WhenAll\s*\(|Task\.Run\s*\(/.test(source)) violations.push(`${file}: parallel task composition is unsafe for scoped dashboard data`);
}
if (!/AccountId\s*==\s*AccountId/.test(commercial)) violations.push(`${commercialPath}: dashboard queries must retain the account boundary`);
if (!/AsNoTracking\s*\(\)/.test(commercial)) violations.push(`${commercialPath}: dashboard reads must be no-tracking`);
if (/public\s+(?:async\s+)?(?:Task<)?IQueryable\b/.test(commercial + experience)) violations.push("Dashboard service exposes IQueryable outside its data boundary");
if (violations.length) { console.error(violations.join("\n")); process.exit(1); }
console.log("Dashboard DbContext safety check passed.");
