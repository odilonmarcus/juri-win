$ErrorActionPreference = "Stop"
$required = @(
  ".\ConselhoJuridicoIA\ConselhoJuridicoIA.csproj",
  ".\ConselhoJuridicoIA\App.xaml",
  ".\ConselhoJuridicoIA\MainWindow.xaml",
  ".\ConselhoJuridicoIA\MainWindow.xaml.cs",
  ".\ConselhoJuridicoIA\SettingsDialog.xaml",
  ".\Installer.iss",
  ".\.github\workflows\build-windows.yml"
)
foreach ($file in $required) {
  if (!(Test-Path $file)) { throw "Arquivo obrigatório ausente: $file" }
}
Write-Host "Estrutura básica OK."
