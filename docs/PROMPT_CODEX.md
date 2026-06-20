# Prompt completo para Codex — OrçaFácil

Você é um desenvolvedor full stack sênior. Evolua o projeto OrçaFácil mantendo simplicidade, performance e código claro.

## Contexto

O OrçaFácil é um SaaS freemium para autônomos, MEIs e pequenas empresas gerarem orçamentos e recibos profissionais em PDF em poucos segundos. O MVP usa Bootstrap 5, JavaScript ES Modules, Firebase Authentication, Firestore, jsPDF e servidor local Node/Fastify.

## Diretrizes técnicas

- Não usar frameworks pesados no front-end.
- Manter compatibilidade mobile-first.
- Priorizar UX simples: preencher, gerar PDF e salvar.
- Manter modo demonstração localStorage quando Firebase não estiver configurado.
- Não quebrar os fluxos existentes.
- Validar inputs críticos antes de salvar.
- Tratar erros com mensagens amigáveis.
- Separar responsabilidades por arquivos: UI, serviços, PDF e utilitários.

## Tarefas prioritárias

1. Revisar e fortalecer a numeração sequencial no Firestore usando transação/counter por usuário.
2. Implementar status do orçamento: rascunho, enviado, aprovado, recusado e convertido.
3. Criar botão “Converter orçamento em recibo”.
4. Melhorar o PDF com layout mais premium e assinatura no recibo.
5. Implementar busca avançada no histórico por cliente, número, período e tipo.
6. Preparar integração de cobrança recorrente, mantendo campo `plan` no perfil do usuário.
7. Criar página pública de venda com SEO para “modelo de orçamento”, “gerador de recibo” e “orçamento para MEI”.
8. Adicionar exportação de backup JSON por usuário.
9. Implementar testes básicos de funções utilitárias.
10. Documentar deploy no Firebase Hosting.

## Critérios de aceite

- `npm start` deve subir o servidor em `http://localhost:8095`.
- Usuário deve conseguir testar sem Firebase configurado.
- Usuário com Firebase configurado deve conseguir cadastrar, logar, salvar perfil, criar documentos, reemitir PDFs e ver histórico.
- PDF gratuito deve exibir marca OrçaFácil.
- PDF Pro não deve exibir marca.
- Recibo deve mostrar valor por extenso.
- Layout deve funcionar bem em celular.
