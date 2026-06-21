import { spawn } from 'node:child_process';
const dir = process.argv[2] || 'dist';
const child = spawn(process.execPath, ['server.js'], { stdio: 'inherit', env: { ...process.env, PUBLIC_DIR: dir } });
child.on('exit', (code, signal) => signal ? process.kill(process.pid, signal) : process.exit(code ?? 0));
