export function isPermissionDenied(error) {
  return String(error?.code || '').includes('permission-denied') || /permission|permiss/i.test(String(error?.message || error || ''));
}

export function isNetworkError(error) {
  const code = String(error?.code || '');
  const message = String(error?.message || error || '');
  return code.includes('network') || code.includes('unavailable') || /network|offline|conex|internet|unavailable/i.test(message);
}

function friendlyError(error, fallback, map) {
  const code = String(error?.code || '');
  const message = map[code] || fallback;
  const friendly = new Error(message);
  friendly.code = code || error?.code || '';
  friendly.originalError = error;
  friendly.isFriendly = true;
  return friendly;
}

export function friendlyAuthError(error) {
  return friendlyError(error, error?.message || 'Não foi possível autenticar. Tente novamente.', {
    'auth/invalid-email': 'E-mail inválido.',
    'auth/weak-password': 'Senha fraca. Use pelo menos 6 caracteres.',
    'auth/email-already-in-use': 'Este e-mail já está cadastrado. Use Entrar.',
    'auth/invalid-credential': 'E-mail ou senha incorretos.',
    'auth/wrong-password': 'E-mail ou senha incorretos.',
    'auth/user-not-found': 'E-mail ou senha incorretos.',
    'auth/network-request-failed': 'Erro de conexão. Verifique sua internet.',
    'auth/too-many-requests': 'Muitas tentativas. Aguarde alguns minutos.',
    'auth/operation-not-allowed': 'Login por e-mail/senha não está habilitado no Firebase Authentication.'
  });
}

export function friendlyFirestoreError(error) {
  return friendlyError(error, error?.message || 'Não foi possível acessar os dados agora.', {
    'permission-denied': 'Sem permissão para acessar esses dados. Faça logout e entre novamente.',
    'unavailable': 'Serviço temporariamente indisponível.',
    'deadline-exceeded': 'A conexão demorou demais. Tente novamente.',
    'unauthenticated': 'Sua sessão expirou. Faça login novamente.'
  });
}
