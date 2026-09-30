using ConselhoJuridicoIA.Models;
using ConselhoJuridicoIA.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ConselhoJuridicoIA;

public sealed partial class SettingsDialog : ContentDialog
{
    private readonly SettingsService _settingsService;
    private readonly CredentialService _credentialService;
    private readonly OpenAIClient _openAIClient;
    private readonly AnthropicClient _anthropicClient;
    private AppSettings _settings;

    public SettingsDialog(
        SettingsService settingsService,
        CredentialService credentialService,
        OpenAIClient openAIClient,
        AnthropicClient anthropicClient)
    {
        InitializeComponent();
        _settingsService = settingsService;
        _credentialService = credentialService;
        _openAIClient = openAIClient;
        _anthropicClient = anthropicClient;
        _settings = _settingsService.Load();

        OpenAIModelBox.Text = _settings.OpenAIModel;
        AnthropicModelBox.Text = _settings.AnthropicModel;
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var openAIModel = OpenAIModelBox.Text.Trim();
        var anthropicModel = AnthropicModelBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(openAIModel) || string.IsNullOrWhiteSpace(anthropicModel))
        {
            args.Cancel = true;
            OpenAITestStatus.Text = "Informe os nomes dos dois modelos.";
            return;
        }

        if (!string.IsNullOrWhiteSpace(OpenAIKeyBox.Password))
            _credentialService.SaveOpenAIKey(OpenAIKeyBox.Password);
        if (!string.IsNullOrWhiteSpace(AnthropicKeyBox.Password))
            _credentialService.SaveAnthropicKey(AnthropicKeyBox.Password);

        _settings.OpenAIModel = openAIModel;
        _settings.AnthropicModel = anthropicModel;
        _settingsService.Save(_settings);
    }

    private async void TestOpenAI_Click(object sender, RoutedEventArgs e)
    {
        TestOpenAIButton.IsEnabled = false;
        OpenAITestStatus.Text = "Testando...";
        try
        {
            var key = !string.IsNullOrWhiteSpace(OpenAIKeyBox.Password)
                ? OpenAIKeyBox.Password
                : _credentialService.GetOpenAIKey();

            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("Informe a chave da OpenAI.");

            await _openAIClient.TestAsync(key, OpenAIModelBox.Text.Trim(), CancellationToken.None);
            OpenAITestStatus.Text = "Conexão OK";
            OpenAITestStatus.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SuccessBrush"];
        }
        catch (Exception ex)
        {
            OpenAITestStatus.Text = ex.Message;
            OpenAITestStatus.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["DangerBrush"];
        }
        finally
        {
            TestOpenAIButton.IsEnabled = true;
        }
    }

    private async void TestAnthropic_Click(object sender, RoutedEventArgs e)
    {
        TestAnthropicButton.IsEnabled = false;
        AnthropicTestStatus.Text = "Testando...";
        try
        {
            var key = !string.IsNullOrWhiteSpace(AnthropicKeyBox.Password)
                ? AnthropicKeyBox.Password
                : _credentialService.GetAnthropicKey();

            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("Informe a chave da Anthropic.");

            await _anthropicClient.TestAsync(key, AnthropicModelBox.Text.Trim(), CancellationToken.None);
            AnthropicTestStatus.Text = "Conexão OK";
            AnthropicTestStatus.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SuccessBrush"];
        }
        catch (Exception ex)
        {
            AnthropicTestStatus.Text = ex.Message;
            AnthropicTestStatus.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["DangerBrush"];
        }
        finally
        {
            TestAnthropicButton.IsEnabled = true;
        }
    }
}
