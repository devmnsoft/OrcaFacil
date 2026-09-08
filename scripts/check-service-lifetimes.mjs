import { readdir, readFile } from "node:fs/promises";
import path from "node:path";

const files = [];
async function walk(directory) {
  for (const entry of await readdir(directory, { withFileTypes: true })) {
    if (["bin", "obj"].includes(entry.name)) continue;
    const file = path.join(directory, entry.name);
    if (entry.isDirectory()) await walk(file); else if (file.endsWith(".cs")) files.push(file);
  }
}
await walk("src");
const source = (await Promise.all(files.map(file => readFile(file, "utf8")))).join("\n");
const protectedServices = [
  "CommercialWorkspaceQueryService",
  "DashboardExperienceService",
  "QualityGateService",
  "IDatabaseSchemaContractService",
  "DatabaseSchemaContractService"
];
const violations = protectedServices.flatMap(name => {
  const singleton = new RegExp(`(?:Add|TryAdd)Singleton\\s*<[^>]*${name}[^>]*>`).test(source);
  const scoped = new RegExp(`(?:Add|TryAdd)Scoped\\s*<[^>]*${name}[^>]*>`).test(source);
  return [...(singleton ? [`${name} is registered as Singleton`] : []), ...(!scoped ? [`${name} has no scoped registration`] : [])];
});
if (/(?:Add|TryAdd)Singleton\s*<[^>]*(?:DbContext|OrcaFacilDbContext)[^>]*>/.test(source))
  violations.push("A DbContext is registered as Singleton");
if (/ValidateOnBuild\s*=\s*false/.test(source)) violations.push("ValidateOnBuild is disabled");
if (/ValidateScopes\s*=\s*false/.test(source)) violations.push("ValidateScopes is disabled");
if (violations.length) { console.error(violations.join("\n")); process.exit(1); }
console.log("Scoped service lifetime check passed.");
