const PREFIX = { orcamento: 'ORC', recibo: 'REC' };
export class NumberingService {
  getNextNumber(type, documents = []) {
    const prefix = PREFIX[type] || 'DOC';
    const max = documents.filter((doc) => doc.type === type).reduce((highest, doc) => {
      const match = String(doc.number || '').match(/(\d+)$/);
      return Math.max(highest, match ? Number(match[1]) : 0);
    }, 0);
    return `${prefix}-${String(max + 1).padStart(6, '0')}`;
  }
}
