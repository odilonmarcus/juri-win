# Conselho Jurídico IA — Windows

Aplicativo Windows nativo em **C# + .NET 10 + WinUI 3** que coloca GPT e Claude em um debate jurídico adversarial e produz um parecer consolidado.

## Funcionalidades incluídas

- Questão jurídica em texto livre.
- Importação de PDF, TXT e RTF.
- Debate visível entre GPT e Claude.
- Profundidade rápida (2 rodadas), completa (4) e profunda (6).
- GPT como Advogado da Tese, revisor/reforço e relator.
- Claude como Contraditório, auditor e stress test.
- Parecer com: resumo executivo, argumentos favoráveis, vulnerabilidades, provas, riscos, validações oficiais, estratégias alternativas e próximas providências.
- Copiar parecer.
- Exportar PDF.
- Apagar sessão sem manter histórico local.
- Configuração de OpenAI e Anthropic.
- Teste de conexão com as duas APIs.
- Chaves armazenadas no Credential Locker do Windows.
- OpenAI chamada com `store: false`.
- Comunicação direta do computador para os provedores; sem servidor intermediário do aplicativo.

## Tecnologia

- .NET 10 LTS
- WinUI 3 / Windows App SDK 2.5.1
- Windows SDK Build Tools 10.0.28000.2705
- PDFsharp-GDI para geração do PDF
- PdfPig para extração local de texto de PDFs
- Inno Setup para gerar um instalador `.exe`

## Build pelo GitHub Actions

O workflow está em `.github/workflows/build-windows.yml`.

Ao executar `Build Windows Installer`, o GitHub Actions gera um artifact chamado **ConselhoJuridicoIA-Windows** contendo:

- `ConselhoJuridicoIA-Setup-x64.exe` — instalador principal.
- `ConselhoJuridicoIA-portable-x64.zip` — versão portátil para diagnóstico/teste.

O usuário final não precisa de Python, terminal, .NET ou Windows App SDK instalado: o publish é self-contained.

## Observação sobre assinatura

O Setup gerado nesta fase não é assinado digitalmente. Ele funciona, mas o Windows SmartScreen pode exibir um aviso por o editor ainda ser desconhecido. Para distribuição definitiva, adicione assinatura de código ao pipeline (certificado EV/OV ou serviço de assinatura compatível) antes de disponibilizar ao advogado.

## Modelos padrão

- OpenAI: `gpt-5.6-sol`
- Anthropic: `claude-sonnet-5`

Os dois nomes ficam editáveis na tela de configurações para não exigir recompilação quando os provedores lançarem novos modelos.

## Privacidade

O aplicativo não grava questões, documentos, debate ou parecer em banco de dados. Documentos são lidos localmente e enviados como contexto para as APIs durante a sessão. Apenas configurações não sensíveis são persistidas em `%LOCALAPPDATA%`; chaves ficam no Credential Locker do Windows.
