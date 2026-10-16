# Marca comercial — Fechô

> Documento de referência para manutenção da identidade visual e verbal do produto.
> **Marca percebida pelo usuário:** Fechô · **Slogan:** *Orçou. Enviou. Fechô.* · **Assinatura:** *Um produto MNSOFT*.
> **Identidade técnica interna permanece `OrcaFacil`** (namespaces, assemblies, banco `orcafacil`, schema, rotas, contratos, claims, variáveis de ambiente).

## 1. Princípios

- A marca de produto é **Fechô**. Nunca usar o sufixo "360".
- O nome é sempre escrito **Fechô** (com acento circunflexo) nos 4 locales: `pt-BR`, `en-US`, `es-ES`, `es-419`.
- A controladora legal é **MNSOFT** (CNPJ 18.160.057/0001-13). A assinatura institucional é **"Um produto MNSOFT"**.
- Nunca reescrever nomes técnicos: `OrcaFacil.*`, DB `orcafacil`, schema/tabelas, rotas, permissões, chaves de configuração/ambiente.
- Em documentos do cliente, a marca do **emitente** tem prioridade; a marca Fechô aparece apenas como marca d'água do plano gratuito e no rodapé institucional.

## 2. Paleta oficial

| Token | Hex | Uso |
| --- | --- | --- |
| `--of-deep-green` | `#0B3835` | Institucional, cabeçalhos, fundos escuros |
| `--of-petrol` | `#0F766E` | Botões, links, interações primárias |
| `--of-mint` | `#B9F1D1` | Destaques da marca, acentos sobre fundo escuro |
| `--of-amber` | `#D6A34F` | Alertas e destaques pontuais |
| `--of-page` (ice) | `#F5F8F7` | Fundo de página |
| `--of-surface` | `#FFFFFF` | Superfícies e cartões |
| `--of-text` (graphite) | `#172B2A` | Texto principal |
| `--of-text-secondary` (soft grey) | `#536B67` | Texto secundário |

- Tipografia de interface: **Inter**.
- Contraste alvo: **WCAG AA** (sem texto escuro sobre fundo escuro nem claro sobre claro).

## 3. Camada central de branding

Os textos de apresentação ficam centralizados em:

- `src/OrcaFacil.Shared/Branding/CommercialBrand.cs`

Use essa classe para nome, slogan, assinatura, textos de hero/CTA, paleta e caminhos de assets, em vez de duplicar strings em layouts, e-mails e PDFs.

## 4. Assets

Assets comerciais em `src/OrcaFacil.Web/wwwroot/img/brand/`:

- `fecho-logo.svg` (horizontal, fundo claro)
- `fecho-logo-white.svg` (horizontal, fundo escuro)
- `fecho-logo-dark.svg` (horizontal, fundo claro alternativo)
- `fecho-logo-mono.svg` (monocromático)
- `fecho-symbol.svg` (símbolo/ícone)
- `fecho-compact.svg` (compacto)
- `fecho-pdf.svg` (cabeçalho de PDF)
- `fecho-favicon.svg` e `wwwroot/favicon.svg`

Os arquivos legados `orcafacil-*.svg` foram **mantidos** (não referenciados) para evitar quebra de dependências; não devem ser usados em novas telas.

## 5. Voz e conteúdo

- Landing: headline **"Seu trabalho vale muito. Seu orçamento também."**, subheadline **"Crie propostas profissionais, organize seus clientes e acompanhe seus negócios em um só lugar."**, CTAs **"Começar grátis"** e **"Conhecer o Fechô"**.
- Login: **"Bem-vindo de volta ao Fechô."**
- Cadastro: **"Comece a organizar seus negócios com o Fechô."**
- Marca d'água do plano gratuito: **"Gerado com Fechô — Um produto MNSOFT"**. Planos pagos seguem sem marca d'água.
- Não inventar métricas, depoimentos ou certificações.

## 6. PWA e SEO

- `wwwroot/site.webmanifest`: `name`/`short_name` = "Fechô", `theme_color` `#0b3835`, `background_color` `#f5f8f7`.
- `_PublicLayout` publica `og:*`, `twitter:*` e JSON-LD (`Organization`) com `name: Fechô` e `legalName: MNSOFT`.
- `wwwroot/sw.js`: cache versionado (`orcafacil-public-v1.9.0`) para invalidar assets antigos mantendo o prefixo técnico para limpeza.

## 7. O que NÃO muda (identidade técnica)

Namespaces e assemblies `OrcaFacil.*`, solução `OrcaFacil.sln`, banco `orcafacil`, schema/tabelas/colunas/constraints, migrations, entidades/DTOs/commands, repositórios, rotas/endpoints, claims/permissões/papéis, identificadores de configuração e variáveis de ambiente (`ORCAFACIL_SUPERADMIN_*`), integrações de terceiros/IA/pagamento, identificadores de plano, regras de negócio e preços, URLs/tokens de aprovação, chaves de localização.

## 8. Pendências conhecidas

- Domínio público permanece `orcafacil.com.br` (`PublicSite:BaseUrl`); qualquer troca de domínio depende de decisão de negócio (DNS/SSL/hospedagem fora do escopo).
- Valores de seed/migração em `database/*.sql` (ex.: catálogo de marketplace) ainda citam a marca antiga; alteração exige script de dados autorizado (não criar migration sem aprovação).
- Documentação interna (`*.md`, `docs/`) e scripts de build/deploy mantêm referências históricas por descreverem o sistema interno.
