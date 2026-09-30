using System.Collections.ObjectModel;
using ConselhoJuridicoIA.Models;
using ConselhoJuridicoIA.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace ConselhoJuridicoIA;

public sealed partial class MainWindow : Window
{
    private readonly ObservableCollection<DocumentAttachment> _documents = new();
    private readonly ObservableCollection<DebateEntry> _debateEntries = new();

    private readonly SettingsService _settingsService = new();
    private readonly CredentialService _credentialService = new();
    private readonly OpenAIClient _openAIClient = new();
    private readonly AnthropicClient _anthropicClient = new();
    private readonly DocumentService _documentService = new();
    private readonly PdfExportService _pdfExportService = new();
    private readonly DebateOrchestrator _orchestrator;

    private CancellationTokenSource? _debateCts;
    private bool _firstLoadHandled;

    public MainWindow()
    {
        InitializeComponent();
        _orchestrator = new DebateOrchestrator(_openAIClient, _anthropicClient);

        DocumentsListView.ItemsSource = _documents;
        DebateListView.ItemsSource = _debateEntries;

        Title = "Conselho Jurídico IA";
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        appWindow.Resize(new Windows.Graphics.SizeInt32(1500, 900));

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
        if (File.Exists(iconPath))
            appWindow.SetIcon(iconPath);
    }

    private async void MainRoot_Loaded(object sender, RoutedEventArgs e)
    {
        if (_firstLoadHandled)
            return;

        _firstLoadHandled = true;
        if (!_credentialService.HasOpenAIKey() || !_credentialService.HasAnthropicKey())
        {
            StatusText.Text = "Configure as chaves das APIs para iniciar.";
            await ShowSettingsAsync();
        }
    }

    private async void Settings_Click(object sender, RoutedEventArgs e) => await ShowSettingsAsync();

    private async Task ShowSettingsAsync()
    {
        var dialog = new SettingsDialog(_settingsService, _credentialService, _openAIClient, _anthropicClient)
        {
            XamlRoot = MainRoot.XamlRoot
        };
        await dialog.ShowAsync();
    }

    private async void AddDocuments_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker
        {
            ViewMode = PickerViewMode.List,
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };
        picker.FileTypeFilter.Add(".pdf");
        picker.FileTypeFilter.Add(".txt");
        picker.FileTypeFilter.Add(".rtf");

        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        var files = await picker.PickMultipleFilesAsync();
        if (files.Count == 0)
            return;

        BusyRing.IsActive = true;
        PhaseText.Text = "Lendo documentos...";
        try
        {
            foreach (var file in files)
            {
                if (_documents.Any(x => string.Equals(x.FilePath, file.Path, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var loaded = await _documentService.LoadAsync(file.Path, CancellationToken.None);
                _documents.Add(loaded);
            }
            StatusText.Text = $"{_documents.Count} documento(s) anexado(s).";
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Não foi possível importar um dos documentos", ex.Message);
        }
        finally
        {
            BusyRing.IsActive = false;
            PhaseText.Text = "Pronto";
        }
    }

    private void RemoveDocument_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: DocumentAttachment document })
            _documents.Remove(document);
    }

    private async void StartDebate_Click(object sender, RoutedEventArgs e)
    {
        if (_debateCts is not null)
            return;

        var question = QuestionTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(question))
        {
            await ShowMessageAsync("Questão jurídica", "Descreva a questão ou estratégia antes de iniciar o debate.");
            return;
        }

        var openAIKey = _credentialService.GetOpenAIKey();
        var anthropicKey = _credentialService.GetAnthropicKey();
        if (string.IsNullOrWhiteSpace(openAIKey) || string.IsNullOrWhiteSpace(anthropicKey))
        {
            await ShowSettingsAsync();
            openAIKey = _credentialService.GetOpenAIKey();
            anthropicKey = _credentialService.GetAnthropicKey();
            if (string.IsNullOrWhiteSpace(openAIKey) || string.IsNullOrWhiteSpace(anthropicKey))
                return;
        }

        var rounds = DepthComboBox.SelectedIndex switch
        {
            0 => 2,
            2 => 6,
            _ => 4
        };

        var settings = _settingsService.Load();
        _debateEntries.Clear();
        OpinionTextBox.Text = string.Empty;
        CopyButton.IsEnabled = false;
        ExportButton.IsEnabled = false;

        _debateCts = new CancellationTokenSource();
        SetBusy(true, "Conselho em debate...");
        StatusText.Text = $"Executando {rounds} rodadas + relatoria. A sessão existe apenas em memória.";

        try
        {
            var opinion = await _orchestrator.RunAsync(
                question,
                _documents.ToList(),
                rounds,
                settings,
                openAIKey,
                anthropicKey,
                async entry =>
                {
                    await DispatcherQueue.EnqueueAsync(() =>
                    {
                        _debateEntries.Add(entry);
                        DebateListView.ScrollIntoView(entry);
                        PhaseText.Text = $"Concluído: {entry.Role}";
                    });
                },
                _debateCts.Token);

            OpinionTextBox.Text = opinion;
            CopyButton.IsEnabled = true;
            ExportButton.IsEnabled = true;
            StatusText.Text = "Debate concluído. Revise e valide as referências antes do uso profissional.";
            PhaseText.Text = "Concluído";
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Debate cancelado. O conteúdo já gerado permanece visível nesta sessão.";
            PhaseText.Text = "Cancelado";
        }
        catch (Exception ex)
        {
            StatusText.Text = "O debate foi interrompido por erro.";
            PhaseText.Text = "Erro";
            await ShowMessageAsync("Erro durante o debate", ex.Message);
        }
        finally
        {
            _debateCts.Dispose();
            _debateCts = null;
            SetBusy(false, PhaseText.Text);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => _debateCts?.Cancel();

    private void CopyOpinion_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(OpinionTextBox.Text))
            return;

        var package = new DataPackage();
        package.SetText(OpinionTextBox.Text);
        Clipboard.SetContent(package);
        Clipboard.Flush();
        StatusText.Text = "Parecer copiado para a área de transferência.";
    }

    private async void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(OpinionTextBox.Text))
            return;

        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = $"Parecer-Conselho-Juridico-{DateTime.Now:yyyyMMdd-HHmm}"
        };
        picker.FileTypeChoices.Add("Documento PDF", new List<string> { ".pdf" });
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));

        var file = await picker.PickSaveFileAsync();
        if (file is null)
            return;

        try
        {
            _pdfExportService.Export(file.Path, QuestionTextBox.Text.Trim(), OpinionTextBox.Text);
            StatusText.Text = $"PDF exportado: {file.Name}";
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Não foi possível exportar o PDF", ex.Message);
        }
    }

    private async void ClearSession_Click(object sender, RoutedEventArgs e)
    {
        if (_debateCts is not null)
        {
            await ShowMessageAsync("Sessão em andamento", "Cancele o debate antes de apagar a sessão.");
            return;
        }

        var dialog = new ContentDialog
        {
            XamlRoot = MainRoot.XamlRoot,
            Title = "Apagar sessão?",
            Content = "A questão, os documentos carregados, o debate e o parecer serão removidos da sessão ativa. O aplicativo não mantém histórico dessas sessões. As chaves e preferências permanecem salvas.",
            PrimaryButtonText = "Apagar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Close
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            return;

        QuestionTextBox.Text = string.Empty;
        OpinionTextBox.Text = string.Empty;
        _documents.Clear();
        _debateEntries.Clear();
        CopyButton.IsEnabled = false;
        ExportButton.IsEnabled = false;
        StatusText.Text = "Sessão limpa. Nenhum histórico local da análise foi mantido.";
        PhaseText.Text = "Pronto";
    }

    private void SetBusy(bool busy, string phase)
    {
        BusyRing.IsActive = busy;
        StartButton.IsEnabled = !busy;
        CancelButton.IsEnabled = busy;
        SettingsButton.IsEnabled = !busy;
        DepthComboBox.IsEnabled = !busy;
        PhaseText.Text = phase;
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = MainRoot.XamlRoot,
            Title = title,
            Content = message,
            CloseButtonText = "OK"
        };
        await dialog.ShowAsync();
    }
}

internal static class DispatcherQueueExtensions
{
    public static Task EnqueueAsync(this Microsoft.UI.Dispatching.DispatcherQueue queue, Action action)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!queue.TryEnqueue(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }))
        {
            tcs.SetException(new InvalidOperationException("Não foi possível atualizar a interface."));
        }
        return tcs.Task;
    }
}
