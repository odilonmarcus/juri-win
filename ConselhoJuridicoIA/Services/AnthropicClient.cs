using System.Text;
using System.Text.Json;

namespace ConselhoJuridicoIA.Services;

public sealed class AnthropicClient
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("https://api.anthropic.com/"),
        Timeout = TimeSpan.FromMinutes(8)
    };

    public async Task<string> GenerateAsync(
        string apiKey,
        string model,
        string systemPrompt,
        string userPrompt,
        int maxOutputTokens,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/messages");
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        var payload = new
        {
            model,
            max_tokens = maxOutputTokens,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = userPrompt }
            }
        };

        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Anthropic retornou {(int)response.StatusCode}: {ExtractError(body)}");

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var parts = new List<string>();

        if (root.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in content.EnumerateArray())
            {
                if (item.TryGetProperty("type", out var type) && type.GetString() == "text" &&
                    item.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                {
                    parts.Add(text.GetString() ?? string.Empty);
                }
            }
        }

        var combined = string.Join("\n", parts).Trim();
        if (combined.Length == 0)
            throw new InvalidOperationException("A Anthropic respondeu sem texto utilizável.");

        return combined;
    }

    public async Task TestAsync(string apiKey, string model, CancellationToken cancellationToken)
    {
        _ = await GenerateAsync(
            apiKey,
            model,
            "Responda apenas OK.",
            "Teste de conexão.",
            32,
            cancellationToken);
    }

    private static string ExtractError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var error) &&
                error.TryGetProperty("message", out var message))
                return message.GetString() ?? body;
        }
        catch
        {
            // Mantém a resposta bruta abaixo.
        }

        return body.Length > 800 ? body[..800] : body;
    }
}
