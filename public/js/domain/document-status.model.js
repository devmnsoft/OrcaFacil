export const BUDGET_STATUS = Object.freeze(['rascunho', 'emitido', 'enviado', 'visualizado', 'aprovado', 'recusado', 'cancelado', 'convertido']);
export const RECEIPT_STATUS = Object.freeze(['rascunho', 'emitido', 'cancelado']);

export const DOCUMENT_STATUS_META = Object.freeze({
  rascunho: { label: 'Rascunho', badge: 'secondary' },
  emitido: { label: 'Emitido', badge: 'primary' },
  enviado: { label: 'Enviado', badge: 'info' },
  visualizado: { label: 'Visualizado', badge: 'warning' },
  aprovado: { label: 'Aprovado', badge: 'success' },
  recusado: { label: 'Recusado', badge: 'danger' },
  cancelado: { label: 'Cancelado', badge: 'dark' },
  convertido: { label: 'Convertido em recibo', badge: 'success' }
});

export const DOCUMENT_STATUS_TRANSITIONS = Object.freeze({
  orcamento: {
    rascunho: ['emitido', 'cancelado'],
    emitido: ['enviado', 'visualizado', 'cancelado'],
    enviado: ['visualizado', 'cancelado'],
    visualizado: ['aprovado', 'recusado', 'cancelado'],
    aprovado: ['convertido', 'cancelado'],
    recusado: [],
    cancelado: [],
    convertido: []
  },
  recibo: {
    rascunho: ['emitido', 'cancelado'],
    emitido: ['cancelado'],
    cancelado: []
  }
});

export function getDocumentStatusMeta(status = 'rascunho') {
  return DOCUMENT_STATUS_META[status] || DOCUMENT_STATUS_META.rascunho;
}

export function getAllowedStatuses(type = 'orcamento') {
  return type === 'recibo' ? RECEIPT_STATUS : BUDGET_STATUS;
}

export function canTransitionDocumentStatus(type, from, to) {
  if (from === to) return true;
  return Boolean(DOCUMENT_STATUS_TRANSITIONS[type]?.[from || 'rascunho']?.includes(to));
}
