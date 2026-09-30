using Windows.Security.Credentials;

namespace ConselhoJuridicoIA.Services;

public sealed class CredentialService
{
    private const string UserName = "api-key";
    private readonly PasswordVault _vault = new();

    public string? GetOpenAIKey() => GetSecret("ConselhoJuridicoIA.OpenAI");
    public string? GetAnthropicKey() => GetSecret("ConselhoJuridicoIA.Anthropic");

    public void SaveOpenAIKey(string value) => SaveSecret("ConselhoJuridicoIA.OpenAI", value);
    public void SaveAnthropicKey(string value) => SaveSecret("ConselhoJuridicoIA.Anthropic", value);

    public bool HasOpenAIKey() => !string.IsNullOrWhiteSpace(GetOpenAIKey());
    public bool HasAnthropicKey() => !string.IsNullOrWhiteSpace(GetAnthropicKey());

    private string? GetSecret(string resource)
    {
        try
        {
            var credential = _vault.Retrieve(resource, UserName);
            credential.RetrievePassword();
            return credential.Password;
        }
        catch
        {
            return null;
        }
    }

    private void SaveSecret(string resource, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        try
        {
            var existing = _vault.Retrieve(resource, UserName);
            _vault.Remove(existing);
        }
        catch
        {
            // Não havia credencial anterior.
        }

        _vault.Add(new PasswordCredential(resource, UserName, value.Trim()));
    }
}
