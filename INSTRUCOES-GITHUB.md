# Gerar o instalador Windows pelo GitHub Actions

O projeto já contém o workflow `.github/workflows/build-windows.yml`. Ele compila a aplicação WinUI 3 em um runner Windows e cria o instalador com Inno Setup.

## Passo a passo

1. Crie um repositório novo no GitHub (pode ser privado).
2. Envie **o conteúdo desta pasta** para a raiz do repositório. A pasta `.github` precisa ser enviada também.
3. Abra a aba **Actions** do repositório.
4. Selecione **Build Windows Installer**.
5. Clique em **Run workflow** e execute na branch `main`.
6. Quando o job ficar verde, abra a execução concluída.
7. Em **Artifacts**, baixe `ConselhoJuridicoIA-Windows`.
8. Dentro do artifact haverá:
   - `ConselhoJuridicoIA-Setup-x64.exe` — instalador para entregar ao advogado.
   - `ConselhoJuridicoIA-portable-x64.zip` — versão portátil para diagnóstico/teste.

## Primeiro teste

1. Execute `ConselhoJuridicoIA-Setup-x64.exe`.
2. Abra **Conselho Jurídico IA**.
3. Na configuração inicial, informe as chaves OpenAI e Anthropic.
4. Mantenha inicialmente os modelos padrão:
   - OpenAI: `gpt-5.6-sol`
   - Anthropic: `claude-sonnet-5`
5. Use **Testar conexão** para os dois provedores.
6. Faça um teste sem documentos.
7. Faça um teste com um PDF textual.
8. Faça um teste de exportação do parecer para PDF.

## SmartScreen

Nesta primeira fase o instalador não é assinado digitalmente. O Windows pode exibir aviso de editor desconhecido/SmartScreen. Isso não é erro de compilação. Para distribuição definitiva, assine o instalador com certificado de assinatura de código.

## Privacidade

- Questões, documentos, debate e parecer não são persistidos em banco local pelo aplicativo.
- As chaves ficam no Credential Locker do Windows.
- OpenAI é chamada com `store: false`.
- As chamadas são feitas diretamente do computador para OpenAI e Anthropic; não existe backend intermediário do Conselho Jurídico IA.
- As políticas de tratamento/retenção das APIs dos provedores continuam aplicáveis.
