export function createChatbotMessage({ role = 'assistant', text = '', category = '', confidence = 0, blocked = false } = {}) {
  return { id: crypto.randomUUID ? crypto.randomUUID() : String(Date.now()), role, text, category, confidence, blocked, createdAt: new Date().toISOString() };
}
