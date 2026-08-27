import { execFileSync } from 'node:child_process';

const output = execFileSync('git', ['diff', '--check'], { encoding: 'utf8' });
if (output.trim()) {
  console.error(output);
  process.exit(1);
}
console.log('Git safety: nenhum marcador de conflito ou erro de whitespace no diff.');
