import { readFile } from 'node:fs/promises';

const catalog = await readFile('src/OrcaFacil.Application/Security/PermissionCodes.cs', 'utf8');
const program = await readFile('src/OrcaFacil.Web/Program.cs', 'utf8');
const service = await readFile('src/OrcaFacil.Web/Security/PermissionAuthorization.cs', 'utf8');
const required = ['Dashboard.View','Clients.View','Clients.Manage','Services.View','Services.Manage','Documents.View',
  'Documents.Create','Documents.Edit','Documents.GeneratePublicLink','Documents.ConvertToWorkOrder','WorkOrders.View',
  'WorkOrders.Manage','Payments.View','Payments.Manage','Receipts.View','Receipts.Manage','Reports.View','Reports.Export',
  'Settings.View','Settings.Manage','Users.Manage','Plan.Manage','Admin.Access','Diagnostics.View','Logs.View','Audit.View'];
const missing = required.filter(permission => !catalog.includes(`"${permission}"`));
if (missing.length) throw new Error(`Permissões ausentes: ${missing.join(', ')}`);
if (!program.includes('PermissionCodes.All') || !program.includes('PermissionAuthorizationHandler'))
  throw new Error('Policies de permissão não estão registradas no backend.');
if (!service.includes('currentAccount.HasPermissionAsync') || !service.includes('AuthorizationHandler<PermissionRequirement>'))
  throw new Error('A autorização não consulta o vínculo real da conta.');
console.log(`${required.length} permissões e enforcement no backend validados.`);
