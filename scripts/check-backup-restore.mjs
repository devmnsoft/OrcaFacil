import { existsSync, readFileSync } from 'node:fs';
const files=['database/backup_postgres.ps1','database/restore_postgres.ps1','scripts/windows/backup-db.ps1','scripts/windows/restore-db.ps1','BACKUP.md'];
for (const file of files) if (!existsSync(file)) throw new Error(`Ausente: ${file}`);
const backup=files.filter(x=>x.includes('backup')&&x.endsWith('.ps1')).map(x=>readFileSync(x,'utf8')).join('\n');
const restore=files.filter(x=>x.includes('restore')&&x.endsWith('.ps1')).map(x=>readFileSync(x,'utf8')).join('\n');
for (const marker of ['pg_dump','yyyyMMdd','HHmmss']) if (!backup.includes(marker)) throw new Error(`Backup sem ${marker}`);
if (!/confirm|ShouldProcess|CONFIRMAR/i.test(restore)) throw new Error('Restore sem confirmação forte.');
if (/PGPASSWORD\s*=\s*['"][^'"]+['"]/i.test(backup+restore)) throw new Error('Senha fixa detectada nos scripts.');
console.log('Backup versionado por data/hora e restore protegido por confirmação OK.');
