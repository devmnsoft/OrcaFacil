export const brl = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
export const toNumber = (value) => Number(String(value ?? '0').replace(/\./g, '').replace(',', '.')) || 0;
