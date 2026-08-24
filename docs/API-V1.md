# API pública V1

A API real está disponível sob `/api/v1` e aceita exclusivamente API keys da conta. Listas usam `page` e `pageSize` (máximo 100). Os recursos implementados nesta entrega são `me`, clientes (listar, consultar, criar e atualizar), serviços (listar e consultar) e catálogo de eventos de webhook.

```http
GET /api/v1/clients?page=1&pageSize=20
Authorization: Bearer of_live_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
```

A resposta paginada contém `items`, `page`, `pageSize`, `totalItems` e `totalPages`. Identificadores sempre são filtrados pela conta autenticada; um identificador de outra conta é respondido como não encontrado.
