# Perfis e permissões

Perfis de plataforma: `SuperAdministrator`, `PlatformSupport`, `PlatformFinance` e `PlatformAuditor`. Perfis de conta: `Owner`, `Administrator`, `Collaborator` e `Viewer`. As permissões são registros parametrizados e as páginas administrativas usam policies, não testes dispersos de role.

O primeiro SuperAdministrador somente é criado quando `ORCAFACIL_SUPERADMIN_EMAIL` e `ORCAFACIL_SUPERADMIN_PASSWORD` estão presentes no ambiente.
