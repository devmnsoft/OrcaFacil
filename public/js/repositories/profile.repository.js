export class ProfileRepository {
  constructor(adapter) { this.adapter = adapter; }
  get() { return this.adapter.getProfile(); }
  save(profile) { return this.adapter.saveProfile(profile); }
}
