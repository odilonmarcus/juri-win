using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ConselhoJuridicoIA.Services;

public sealed class OpenAIClient
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("https://api.openai.com/"),
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
        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new
        {
            model,
            store = false,
            instructions = systemPrompt,
            input = userPrompt,
            max_output_tokens = maxOutputTokens,
            reasoning = new { effort = "medium" }
        };

        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI retornou {(int)response.StatusCode}: {ExtractError(body)}");

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        if (root.TryGetProperty("output_text", out var direct) && direct.ValueKind == JsonValueKind.String)
        {
            var value = direct.GetString();
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        if (root.TryGetProperty("output", out var output) && output.ValueKind == JsonValueKind.Array)
        {
            var parts = new List<string>();
            foreach (var item in output.EnumerateArray())
            {
                if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var part in content.EnumerateArray())
                {
                    if (part.TryGetProperty("type", out var type) && type.GetString() == "output_text" &&
                        part.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                    {
                        parts.Add(text.GetString() ?? string.Empty);
                    }
                }
            }

            var combined = string.Join("\n", parts).Trim();
            if (combined.Length > 0)
                return combined;
        }

        throw new InvalidOperationException("A OpenAI respondeu sem texto utilizável.");
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
