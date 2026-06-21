export function createSystemLog(data = {}) {
  return {
    id: data.id || '',
    level: data.level || 'info',
    type: data.type || '',
    title: data.title || data.type || '',
    message: data.message || '',
    uid: data.uid || '',
    userEmail: data.userEmail || '',
    userName: data.userName || '',
    role: data.role || 'user',
    metadata: data.metadata || {},
    errorMessage: data.errorMessage || '',
    errorStack: data.errorStack || '',
    errorCode: data.errorCode || '',
    source: data.source || 'frontend',
    environment: data.environment || '',
    userAgent: data.userAgent || '',
    url: data.url || '',
    createdAt: data.createdAt || new Date().toISOString()
  };
}
