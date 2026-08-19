# OrçaFácil ASP.NET — Release final V1

**Release:** 1.0.0
**Data:** 19/08/2026
**Estado inicial:** congelada para validação final; a aprovação depende de todos os gates de `RELEASE_CHECKLIST.md`.

## Conteúdo

A release reúne os módulos público, cadastro/autenticação, onboarding, comercial, operacional, financeiro, configurações e administração protegida relacionados em `docs/GO-LIVE-V1.md`. Funcionalidades sem operação completa não são expostas no menu. SMTP e gateway de pagamento são opcionais e permanecem desativados até receberem configuração válida no ambiente.

## Pendências

- **P0/P1 bloqueantes:** nenhuma pode permanecer na assinatura final.
- **Não bloqueantes:** integrações opcionais devem ser homologadas somente quando contratadas; métricas e alertas devem ser observados durante a operação assistida.

## Riscos conhecidos

Diferenças de PostgreSQL, IIS, certificado, SMTP, permissões de arquivo e rede do destino exigem staging equivalente. Checks estáticos não substituem restore real nem homologação em navegador. O operador deve conservar o pacote anterior, backup verificável e chaves de Data Protection.

## Gates da release

- [ ] Restore, builds Debug/Release, testes Debug/Release e publish aprovados.
- [ ] Checks npm finais aprovados.
- [ ] Banco novo e atualização de banco existente aprovados sem perda de dados.
- [ ] Homologação por perfil, segurança, navegador e responsividade aprovadas.
- [ ] Backup e rollback ensaiados.
- [ ] Artefato e checksums identificados; decisão registrada por responsável humano.

## Credencial de exemplo e primeiro acesso

Use somente um endereço reservado, como `admin.homologacao@example.invalid`, e uma senha temporária gerada fora do Git. Execute o seed idempotente documentado, altere a senha no primeiro login, conclua o onboarding e revogue a credencial de homologação antes do go-live.
