import { complete, read, requireCheck } from './sprint17-check-utils.mjs';
const layout = read('src/OrcaFacil.Web/Pages/Shared/_Layout.cshtml');
const page = read('src/OrcaFacil.Web/Pages/Notifications/Index.cshtml');
requireCheck(layout.includes('GetUnreadCountAsync') && layout.includes('/Notifications/Index'), 'Contador/link de notificações ausente.');
requireCheck(/method="post"/i.test(page), 'Notificações não possuem ação POST real.');
complete('notificações mobile');
