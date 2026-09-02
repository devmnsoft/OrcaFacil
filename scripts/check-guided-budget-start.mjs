import { read, requireAll, ok } from './schema-drift-check-utils.mjs';
const source = read('src/OrcaFacil.Persistence/Services/GuidedBudgetStartService.cs');
requireAll(source, ['AsNoTracking()', 'x.AccountId == accountId', '!x.IsDeleted && x.IsActive', '"clients"', '"/Clients/Create"'], 'GuidedBudgetStartService');
ok('início guiado isola conta, cliente ativo e empty state');
