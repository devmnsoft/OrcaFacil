const locks = new Map();
const buckets = new Map();

export async function withTryCatch(actionName, fn, options = {}){
  try { return await fn(); }
  catch (error) {
    options.onError?.(error, actionName);
    if (options.rethrow !== false) throw error;
    return options.fallback;
  }
}

export async function withButtonLoading(button, action){
  if(!button || button.disabled) return;
  const original = button.innerHTML;
  button.disabled = true;
  button.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Processando...';
  try { return await action(); }
  finally { button.disabled = false; button.innerHTML = original; }
}

export function preventDoubleClick(key, timeoutMs = 1500){
  const now = Date.now();
  const until = locks.get(key) || 0;
  if(now < until) return false;
  locks.set(key, now + timeoutMs);
  setTimeout(() => locks.delete(key), timeoutMs + 50);
  return true;
}

export function rateLimit(key, limit = 5, windowMs = 60000){
  const now = Date.now();
  const current = (buckets.get(key) || []).filter(ts => now - ts < windowMs);
  if(current.length >= limit) return false;
  current.push(now);
  buckets.set(key, current);
  return true;
}

export const preventDoubleSubmit = preventDoubleClick;
