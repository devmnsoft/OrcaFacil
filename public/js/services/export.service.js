export class ExportService {
  exportJson(documents) { return JSON.stringify(documents, null, 2); }
  exportCsv(documents) { return [['Número','Tipo','Cliente','Data','Status','Total'], ...documents.map((d) => [d.number, d.type, d.clientName, d.date, d.status, d.total])].map((row) => row.map((value) => `"${String(value ?? '').replace(/"/g, '""')}"`).join(',')).join('\n'); }
}
