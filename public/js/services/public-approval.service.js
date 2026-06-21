export class PublicApprovalService {
  constructor(repository, pdfService) { this.repository = repository; this.pdfService = pdfService; }
  loadByToken(token) { return this.repository.getByToken(token); }
  approve(token, note = '') { return this.repository.decide?.(token, 'aprovado', note); }
  reject(token, note = '') { return this.repository.decide?.(token, 'recusado', note); }
  async downloadPdf(token) { const data = await this.loadByToken(token); return this.pdfService?.generate(data.document, data.profile); }
}
