const SENSITIVE_KEYS = ['password','senha','token','secret','apikey','apiKey','authorization','privateKey','accessToken','refreshToken'];
export const DEFAULT_LOGGING_CONFIG = { loggingEnabled:true, logLevel:'info', saveDebugLogs:false, maxClientLogsPerSession:200, maxPendingLogsBeforeLogin:50, dedupeWindowMs:3000, persistBootLogsAfterLogin:true, criticalErrorsAlwaysPersist:true };
const order = ['debug','info','success','warning','error','critical'];
export function sanitize(value, seen = new WeakSet()) {
  if (value == null || typeof value !== 'object') return value;
  if (seen.has(value)) return '[circular]';
  seen.add(value);
  if (Array.isArray(value)) return value.map(v => sanitize(v, seen));
  const out = {};
  for (const [key, val] of Object.entries(value)) {
    const sensitive = SENSITIVE_KEYS.some(k => key.toLowerCase().includes(k.toLowerCase()));
    out[key] = sensitive ? '[removido]' : sanitize(val, seen);
  }
  return out;
}
export function canPersistRemote(userContext, demoMode = false) { return Boolean(userContext?.uid) && !demoMode; }
export function shouldPersistLevel(level, cfg = DEFAULT_LOGGING_CONFIG, environment = '') {
  if (level === 'critical' && cfg.criticalErrorsAlwaysPersist !== false) return true;
  if (!cfg.loggingEnabled) return false;
  if (level === 'debug' && !cfg.saveDebugLogs && !['localhost','Node local'].includes(environment)) return false;
  return order.indexOf(level) >= order.indexOf(cfg.logLevel || 'info');
}
export function shouldSkipDuplicatedLog(recentLogKeys, type, level, windowMs = 3000, now = Date.now()) {
  const dedupeTypes = new Set(['APP_AUTH_STATE_CHANGED','AUTH_STATE_CHANGED','ENVIRONMENT_DETECTED','APP_BOOT_START','LOGGER_PERMISSION_DENIED','TELEGRAM_QUEUE_FAILED','FIRESTORE_PERMISSION_DENIED']);
  if (!dedupeTypes.has(type)) return false;
  const key = `${level}:${type}`;
  const hadKey = recentLogKeys.has(key);
  const last = recentLogKeys.get(key) || 0;
  recentLogKeys.set(key, now);
  return hadKey && now - last < windowMs;
}
export function addPendingLog(pendingLogs, item, max = 50) { pendingLogs.push(item); while (pendingLogs.length > max) pendingLogs.shift(); return pendingLogs.length; }
export function isPermissionDenied(error) { return error?.code === 'permission-denied' || String(error?.message || '').includes('permission-denied'); }
export function isRecentEnough(item, maxAgeMs = 30 * 60 * 1000, now = Date.now()) { const at = Date.parse(item?.data?.createdAtLocal || item?.createdAtLocal || new Date(now).toISOString()); return Number.isFinite(at) ? now - at <= maxAgeMs : true; }
