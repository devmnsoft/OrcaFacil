# Ambientes do OrçaFácil

## Local
- Roda com `npm start` na porta padrão `8095`.
- Pode usar Firebase real, emuladores ou modo demonstração com `localStorage`.
- Use `.env` local não versionado para variáveis locais; nunca commite segredos.

### Pepper técnico no Windows/Visual Studio

O perfil `Development` contém somente um valor explícito e identificável como local, para que a aplicação abra sem armazenar um segredo real. Para substituir esse valor na sua máquina (recomendado), use o Secret Manager:

```powershell
dotnet user-secrets init --project src/OrcaFacil.Web
dotnet user-secrets set "Security:TechnicalFingerprintPepper" "use-um-valor-local-longo-e-aleatorio" --project src/OrcaFacil.Web
```

Ou configure a variável de ambiente do Windows e reinicie o Visual Studio:

```powershell
setx Security__TechnicalFingerprintPepper "use-um-valor-local-longo-e-aleatorio"
```

`Testing` usa exclusivamente seu fallback fixo quando não há configuração. Em `Staging`, `Production` e qualquer outro ambiente, não existe fallback: `Security:TechnicalFingerprintPepper` é obrigatório e a inicialização falha sem ele. Nunca reutilize o valor de desenvolvimento em produção.

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
