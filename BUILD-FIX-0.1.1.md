# Build fix 0.1.1

Correção para o erro `NETSDK1140` observado com o SDK .NET 10.0.401 no GitHub Actions.

Alterações:

- `TargetFramework`: `net10.0-windows10.0.28000.0` -> `net10.0-windows10.0.26100.0`
- adicionado `SupportedOSPlatformVersion=10.0.19041.0`
- `Microsoft.Windows.SDK.BuildTools`: `10.0.28000.2705` -> `10.0.26100.9169`
- removida a referência explícita a `System.Text.Encoding.CodePages`, que é redundante no .NET 10 e gerava `NU1510`
- versão interna do projeto: `0.1.1`

O Windows App SDK permanece em `2.5.1`.
