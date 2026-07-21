# Conexão PostgreSQL local

Connection string esperada em desenvolvimento:

```text
Host=localhost;Port=5432;Database=orcafacil;Username=orcafacil_user;Password=123456
```

## Criar ou corrigir usuário local

Execute como `postgres`/superuser:

```bash
psql -h localhost -p 5432 -U postgres -f database/corrigir_usuario_local.sql
```

O script cria ou altera o usuário `orcafacil_user` com senha `123456` e cria o banco `orcafacil` quando ele não existir.

## Criar schema e tabelas

Depois do usuário/banco, execute o script completo do projeto, se disponível:

```bash
psql -h localhost -p 5432 -U orcafacil_user -d orcafacil -f database/script_completop.sql
```

## Testar conexão

```bash
psql -h localhost -p 5432 -U orcafacil_user -d orcafacil
```

## Erro 28P01

`28P01` significa falha de autenticação no PostgreSQL: usuário ou senha incorretos. Confira `ConnectionStrings:DefaultConnection` ou a variável `ConnectionStrings__DefaultConnection`.
