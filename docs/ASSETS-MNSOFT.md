# Assets MNSOFT

Este PR não adiciona arquivos binários. A logo oficial da MNSOFT deve ser instalada manualmente fora do versionamento.

Para usar a logo oficial da MNSOFT, copie manualmente o arquivo da logo para:

`src/OrcaFacil.Web/wwwroot/img/branding/mnsoft-logo.png`

O layout já está preparado para verificar esse caminho em tempo de execução. Se o PNG não existir, o sistema exibe um fallback textual elegante:

**MNSOFT**  
Consultorias e soluções em TI.

Assim não há imagem quebrada em produção, homologação ou desenvolvimento.
