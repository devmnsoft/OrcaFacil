export class AuthService {
  constructor(adapter) { this.adapter = adapter; }
  login(email, password) { return this.adapter.login(email, password); }
  register(email, password, name) { return this.adapter.register(email, password, name); }
  logout() { return this.adapter.logout(); }
  onAuthChanged(callback) { return this.adapter.onAuth(callback); }
  getCurrentUser() { return this.adapter.user || null; }
}
