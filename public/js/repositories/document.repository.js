export class DocumentRepository {
  constructor(adapter) { this.adapter = adapter; }
  list(filters) { return this.adapter.listDocuments(filters); }
  async getById(id) { return (await this.list()).find((doc) => doc.id === id) || null; }
  save(document) { return this.adapter.saveDocument(document); }
  delete(id) { return this.adapter.deleteDocument(id); }
}
