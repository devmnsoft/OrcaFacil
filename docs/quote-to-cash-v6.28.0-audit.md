# Quote-to-cash v6.28.0 audit

## Baseline

- Reference commit validated locally: `06cad9d07979ca8e1343275688337f556027963d`.
- The workspace already contained local `.vs/` changes before this increment; they were not modified intentionally.

## Implemented in this increment

- Centralized contract-origin selection in `CommercialContractOriginSelector`.
- Document balance no longer changes to a newer current revision when an existing work order is the active contractual origin.
- Quote workspace now uses the balance service as the canonical source for selected work order instead of duplicating origin selection.
- Payments with incompatible `DocumentId` + selected `WorkOrderId` are excluded from balance math and exposed as blocking divergences.
- Manual payment registration now rejects new receipts while a blocking financial-link divergence exists.
- Approved quotes that are paid, overpaid, or blocked route to finance review/history instead of always suggesting another payment.

## Still pending

- Database-level backfill/report for historical incompatible links.
- SQL-level optimization for financial min/max filters in quote workspace; current code still materializes when value filters require computed contracted totals.
- End-to-end Playwright evidence for the visual list/editor/detail finance flow.
- Broader localization/AI/trial-plan work outside this stability slice.

## Suggested verification

- Unit tests: `dotnet test OrcaFacil.sln -c Release`.
- SQL plan review after realistic tenant data is available for quote-workspace financial filters.
