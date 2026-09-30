using ConselhoJuridicoIA.Models;

namespace ConselhoJuridicoIA.Services;

public sealed class DebateOrchestrator
{
    private readonly OpenAIClient _openAI;
    private readonly AnthropicClient _anthropic;

    public DebateOrchestrator(OpenAIClient openAI, AnthropicClient anthropic)
    {
        _openAI = openAI;
        _anthropic = anthropic;
    }

    public async Task<string> RunAsync(
        string question,
        IReadOnlyCollection<DocumentAttachment> documents,
        int rounds,
        AppSettings settings,
        string openAIKey,
        string anthropicKey,
        Func<DebateEntry, Task> onEntry,
        CancellationToken cancellationToken)
    {
        var context = PromptFactory.BuildCaseContext(question, documents);
        var transcript = new List<DebateEntry>();

        var thesis = await _openAI.GenerateAsync(
            openAIKey,
            settings.OpenAIModel,
            PromptFactory.GroundingRules,
            PromptFactory.Thesis(context),
            settings.MaxOutputTokens,
            cancellationToken);
        var thesisEntry = NewEntry("GPT", "Advogado da Tese", settings.OpenAIModel, thesis);
        transcript.Add(thesisEntry);
        await onEntry(thesisEntry);

        var contradiction = await _anthropic.GenerateAsync(
            anthropicKey,
            settings.AnthropicModel,
            PromptFactory.GroundingRules,
            PromptFactory.Contradiction(context, thesis),
            settings.MaxOutputTokens,
            cancellationToken);
        var contradictionEntry = NewEntry("Claude", "Advogado do Contraditório", settings.AnthropicModel, contradiction);
        transcript.Add(contradictionEntry);
        await onEntry(contradictionEntry);

        string? revision = null;
        string? audit = null;

        if (rounds >= 4)
        {
            revision = await _openAI.GenerateAsync(
                openAIKey,
                settings.OpenAIModel,
                PromptFactory.GroundingRules,
                PromptFactory.Revision(context, thesis, contradiction),
                settings.MaxOutputTokens,
                cancellationToken);
            var revisionEntry = NewEntry("GPT", "Revisão da Tese", settings.OpenAIModel, revision);
            transcript.Add(revisionEntry);
            await onEntry(revisionEntry);

            audit = await _anthropic.GenerateAsync(
                anthropicKey,
                settings.AnthropicModel,
                PromptFactory.GroundingRules,
                PromptFactory.Audit(context, revision, contradiction),
                settings.MaxOutputTokens,
                cancellationToken);
            var auditEntry = NewEntry("Claude", "Auditoria Final", settings.AnthropicModel, audit);
            transcript.Add(auditEntry);
            await onEntry(auditEntry);
        }

        if (rounds >= 6)
        {
            revision ??= thesis;
            audit ??= contradiction;

            var reinforcement = await _openAI.GenerateAsync(
                openAIKey,
                settings.OpenAIModel,
                PromptFactory.GroundingRules,
                PromptFactory.Reinforcement(context, revision, audit),
                settings.MaxOutputTokens,
                cancellationToken);
            var reinforcementEntry = NewEntry("GPT", "Reforço Estratégico", settings.OpenAIModel, reinforcement);
            transcript.Add(reinforcementEntry);
            await onEntry(reinforcementEntry);

            var stressTest = await _anthropic.GenerateAsync(
                anthropicKey,
                settings.AnthropicModel,
                PromptFactory.GroundingRules,
                PromptFactory.StressTest(context, reinforcement, audit),
                settings.MaxOutputTokens,
                cancellationToken);
            var stressEntry = NewEntry("Claude", "Stress Test Final", settings.AnthropicModel, stressTest);
            transcript.Add(stressEntry);
            await onEntry(stressEntry);
        }

        var report = await _openAI.GenerateAsync(
            openAIKey,
            settings.OpenAIModel,
            PromptFactory.GroundingRules,
            PromptFactory.Report(context, transcript),
            settings.MaxOutputTokens,
            cancellationToken);

        var reporterEntry = NewEntry("GPT", "Relator", settings.OpenAIModel, report);
        transcript.Add(reporterEntry);
        await onEntry(reporterEntry);

        return report;
    }

    private static DebateEntry NewEntry(string speaker, string role, string model, string content) => new()
    {
        Speaker = speaker,
        Role = role,
        Model = model,
        Content = content
    };
}
