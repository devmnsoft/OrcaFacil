import { readdir, readFile } from "node:fs/promises";
import path from "node:path";

const roots = ["src/OrcaFacil.Persistence", "src/OrcaFacil.Web", "src/OrcaFacil.Infrastructure"];
const files = [];
async function walk(directory) {
  for (const entry of await readdir(directory, { withFileTypes: true })) {
    if (["bin", "obj"].includes(entry.name)) continue;
    const file = path.join(directory, entry.name);
    if (entry.isDirectory()) await walk(file);
    else if (file.endsWith(".cs")) files.push(file);
  }
}
for (const root of roots) await walk(root);

const violations = [];
for (const file of files) {
  const source = await readFile(file, "utf8");
  if (!source.includes("OrcaFacilDbContext")) continue;
  if (/Task\.WhenAll\s*\(/.test(source)) violations.push(`${file}: Task.WhenAll with a DbContext dependency`);
  if (/Task\.Run\s*\(/.test(source)) violations.push(`${file}: Task.Run with a DbContext dependency`);
}
if (violations.length) {
  console.error(violations.join("\n"));
  process.exit(1);
}
console.log(`DbContext concurrency check passed (${files.length} C# files inspected).`);
