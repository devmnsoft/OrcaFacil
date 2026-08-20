import { complete, read, requireCheck } from './sprint17-check-utils.mjs';
const page = read('src/OrcaFacil.Web/Pages/WorkOrders/Details.cshtml');
const model = read('src/OrcaFacil.Web/Pages/WorkOrders/Details.cshtml.cs');
for (const handler of ['Start','Pause','Resume','Complete','Cancel','ToggleChecklist']) requireCheck(page.includes(`asp-page-handler="${handler}"`) && model.includes(`OnPost${handler}Async`), `Handler real ausente: ${handler}.`);
requireCheck(model.includes('x.AccountId == account.AccountId'), 'Isolamento por conta ausente.');
complete('OS mobile operacional');
