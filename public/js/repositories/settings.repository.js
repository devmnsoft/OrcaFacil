export class PlaceholderRepository {
  constructor(storageKey = 'orcafacil:placeholder') { this.storageKey = storageKey; }
  async list() { return JSON.parse(localStorage.getItem(this.storageKey) || '[]'); }
  async save(item) { const all = await this.list(); all.unshift(item); localStorage.setItem(this.storageKey, JSON.stringify(all)); return item; }
}
