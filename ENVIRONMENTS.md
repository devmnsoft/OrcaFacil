# Ambientes do OrçaFácil

## Local
- Roda com `npm start` na porta padrão `8095`.
- Pode usar Firebase real, emuladores ou modo demonstração com `localStorage`.
- Use `.env` local não versionado para variáveis locais; nunca commite segredos.

## Homologação
- Recomendado usar Firebase Hosting preview channel ou projeto Firebase separado.
- Firestore deve ser de teste quando houver projeto dedicado.
- Valida release antes de produção com `npm run validate` e artifact `dist`.
- Futuro `.firebaserc`: `staging: orcafacil-hml` quando o projeto existir.

## Produção
- Firebase Hosting oficial no projeto `orcafacil-b771c`.
- Firestore oficial e App Check recomendado.
- Publicação deve usar `dist`, gerado por build minificado/ofuscado.
- `npm run security:check` é obrigatório antes de deploy.
