# Status do projeto Windows

## Implementado

- Aplicação nativa C# / .NET 10 / WinUI 3.
- Windows App SDK 2.5.1.
- Modelos padrão `gpt-5.6-sol` e `claude-sonnet-5`, editáveis na interface.
- OpenAI Responses API com `store: false`.
- Anthropic Messages API.
- Chaves no Credential Locker do Windows.
- Questão jurídica + anexos PDF/TXT/RTF.
- Debate visível em 2, 4 ou 6 rodadas.
- GPT: tese, revisão/reforço e relatoria.
- Claude: contraditório, auditoria e stress test.
- Parecer consolidado nas oito seções definidas.
- Copiar parecer, exportar PDF, cancelar e limpar sessão.
- Sem banco de histórico da análise.
- GitHub Actions para gerar executável self-contained, ZIP portátil e Setup `.exe`.
- Inno Setup configurado para instalação por usuário, sem exigir privilégios administrativos.

## Verificações realizadas neste ambiente

- XML/XAML/csproj parseados sem erro estrutural.
- Verificação de presença das funcionalidades essenciais.
- Busca por chaves de API hard-coded: nenhuma encontrada.
- Pacotes/versões principais conferidos em documentação/NuGet atual.

## Verificação que depende do GitHub Actions

A compilação WinUI 3 e a geração do instalador precisam rodar em Windows. O workflow incluído usa `windows-latest` e falha automaticamente se `ConselhoJuridicoIA.exe` ou o Setup não forem gerados.
