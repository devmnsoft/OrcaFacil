export const AppEvents = new EventTarget();
export const emit = (name, detail = {}) => AppEvents.dispatchEvent(new CustomEvent(name, { detail }));
export const on = (name, callback) => AppEvents.addEventListener(name, callback);
