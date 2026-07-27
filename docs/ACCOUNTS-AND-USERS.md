# Contas, usuários e clientes cadastrados

`BusinessAccount` é o negócio que contrata um plano; `AccountMember` liga um login à conta; `Client` continua sendo a pessoa física ou jurídica atendida. Durante a migração, `UserId` permanece disponível e `AccountId` é aditivo. Cada usuário legado deve receber uma conta e um vínculo `Owner` antes de tornar `AccountId` obrigatório.

Estados da conta e do membro são independentes. Bloquear uma conta não apaga dados; desabilitar um membro preserva sua autoria. Nunca remova o último `Owner` ativo.
