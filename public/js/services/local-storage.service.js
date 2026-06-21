export class LocalStorageService {
  get(key, fallback = null) { const raw = localStorage.getItem(key); return raw ? JSON.parse(raw) : fallback; }
  set(key, value) { localStorage.setItem(key, JSON.stringify(value)); return value; }
  remove(key) { localStorage.removeItem(key); }
}
