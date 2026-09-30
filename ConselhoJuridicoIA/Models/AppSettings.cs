namespace ConselhoJuridicoIA.Models;

public sealed class AppSettings
{
    public string OpenAIModel { get; set; } = "gpt-5.6-sol";
    public string AnthropicModel { get; set; } = "claude-sonnet-5";
    public int MaxOutputTokens { get; set; } = 10000;
}
