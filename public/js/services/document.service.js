export class DocumentService {
  constructor(repository) { this.repository = repository; }
  create(document) { return this.repository.save(document); }
  update(document) { return this.repository.save(document); }
  save(document) { return this.repository.save(document); }
  delete(id) { return this.repository.delete(id); }
  async duplicate(id) { const doc = await this.getById(id); return this.create({ ...doc, id: '', number: '' }); }
  list(filters = {}) { return this.repository.list(filters); }
  getById(id) { return this.repository.getById(id); }
  async changeStatus(id, status) { const doc = await this.getById(id); return this.save({ ...doc, status }); }
  generatePublicLink(id) { return this.repository.generatePublicLink?.(id); }
  disablePublicLink(id) { return this.repository.disablePublicLink?.(id); }
}
