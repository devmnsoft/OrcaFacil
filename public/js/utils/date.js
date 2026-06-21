export const todayISO = () => new Date().toISOString().slice(0, 10);
export const formatDateBR = (value) => value ? new Date(`${String(value).slice(0,10)}T00:00:00`).toLocaleDateString('pt-BR') : '';
