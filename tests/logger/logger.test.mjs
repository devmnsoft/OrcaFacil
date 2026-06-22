import assert from 'node:assert/strict';
import { addPendingLog, canPersistRemote, isPermissionDenied, sanitize, shouldSkipDuplicatedLog } from '../../public/js/services/logger-core.mjs';

assert.deepEqual(sanitize({ password:'x', nested:{ token:'y', ok:1 }, list:[{ apiKey:'z' }] }), { password:'[removido]', nested:{ token:'[removido]', ok:1 }, list:[{ apiKey:'[removido]' }] });
assert.equal(canPersistRemote({ uid:'u1' }, false), true);
assert.equal(canPersistRemote({ uid:'u1' }, true), false);
assert.equal(canPersistRemote(null, false), false);
const map = new Map();
assert.equal(shouldSkipDuplicatedLog(map, 'APP_BOOT_START', 'info', 3000, 1000), false);
assert.equal(shouldSkipDuplicatedLog(map, 'APP_BOOT_START', 'info', 3000, 2000), true);
assert.equal(shouldSkipDuplicatedLog(map, 'APP_BOOT_START', 'info', 3000, 5001), false);
const pending = [];
for (let i=0;i<55;i++) addPendingLog(pending, { id:i }, 50);
assert.equal(pending.length, 50);
assert.equal(pending[0].id, 5);
assert.equal(isPermissionDenied({ code:'permission-denied' }), true);
console.log('logger core tests passed');
