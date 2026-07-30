# Política de preservação nas transições de plano

> **Planos controlam acesso e capacidade. Eles não apagam os dados.**

## Retorno ao Grátis

O plano escolhido permanece registrado e o `IPlanAccessService` calcula o plano disponível agora. Quando o acesso pago termina, a aplicação invalida o cache de autorização, registra atividade, auditoria e notificação e apresenta uma ação de regularização. A transição não altera a situação do provedor de pagamento.

Clientes, serviços, documentos, PDFs já gerados, recibos, revisões, links públicos, decisões, acompanhamentos, ordens, agenda, membros, convites, auditoria, branding, logo, modelos, histórico, configurações e notificações permanecem com os mesmos identificadores. Nenhum fluxo de plano pode executar exclusão física, `DeleteRange`, `TRUNCATE` ou limpeza de arquivos.

## Uso acima do limite

Registros existentes continuam consultáveis, exportáveis e passíveis de desativação ou exclusão voluntária. O backend impede novos registros e reativações enquanto o uso estiver acima do limite carregado do catálogo. A mensagem informa a quantidade preservada e aponta a regularização.

## Artefatos e configurações

- PDFs antigos permanecem imutáveis. Novos PDFs obedecem ao plano disponível agora.
- Branding, logo, cores e modelos premium permanecem armazenados, ficam pausados e são reaplicados depois da restauração.
- Links públicos criados antes da pausa continuam usando o snapshot congelado até expiração ou revogação.
- Membros não são excluídos; os excedentes ficam pausados pelo limite, mantendo vínculo, autoria e auditoria.

## Restauração

Após a confirmação real do pagamento ou liberação temporária válida, o acesso é recalculado e o cache invalidado. Benefícios e configurações voltam a ser aplicados sobre os mesmos registros: não existe recriação, cópia ou duplicação de dados.
