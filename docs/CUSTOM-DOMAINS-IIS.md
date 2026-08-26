# Domínios customizados no IIS/proxy

O OrçaFácil **não emite nem instala certificados automaticamente**. Antes de ativar um domínio, publique o registro TXT exibido pelo produto, execute a verificação real e configure o binding HTTPS no IIS ou proxy.

1. Crie o registro DNS solicitado e aguarde a propagação do provedor; ela não é instantânea.
2. Execute `Resolve-DnsName -Type TXT _orcafacil-verification.seudominio.com` e compare o valor no painel, sem registrar o token em logs.
3. Instale um certificado emitido por autoridade confiável no repositório da máquina. Nunca copie chave privada para o Git ou banco.
4. No IIS, adicione binding HTTPS com SNI e o host exato; mantenha HTTP apenas para redirecionamento controlado.
5. Execute os scripts de leitura em `scripts/windows`. Eles não alteram DNS, bindings ou certificados.
6. Configure `PublicBaseUrl` com HTTPS no ambiente Production. O `Host` recebido nunca deve ser usado para gerar links sem resolução ativa do tenant.

Se não houver automação de certificado, o status esperado é `ManualRequired` até uma checagem TLS real confirmar validade, host e expiração.
