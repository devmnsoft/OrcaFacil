import { complete, read, requireCheck } from './sprint17-check-utils.mjs';
const script = read('src/OrcaFacil.Web/wwwroot/js/offline-status.js');
requireCheck(script.includes("addEventListener('submit'") && script.includes('event.preventDefault()'), 'POST não é bloqueado offline.');
requireCheck(!script.includes('localStorage') && !script.includes('indexedDB'), 'Política offline não deve persistir dados operacionais.');
complete('política offline honesta');
