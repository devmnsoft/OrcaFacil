export class ProfileService {
  constructor(repository) { this.repository = repository; }
  get() { return this.repository.get(); }
  save(profile) { return this.repository.save(profile); }
}
