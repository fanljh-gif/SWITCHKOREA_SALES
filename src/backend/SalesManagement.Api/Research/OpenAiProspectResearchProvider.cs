using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SalesManagement.Core.Research;

namespace SalesManagement.Api.Research;

public sealed class OpenAiProspectResearchProvider(HttpClient httpClient, IOptions<ResearchProviderOptions> options) : IProspectResearchProvider
{
    private readonly ResearchProviderOptions _options = options.Value;

    public async IAsyncEnumerable<ResearchProviderResult> SearchAsync(ResearchQuery query, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey)) throw new InvalidOperationException("Research API key is not configured.");
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = JsonContent.Create(CreatePayload(query));
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var outputText = document.RootElement.GetProperty("output").EnumerateArray()
            .Where(x => x.TryGetProperty("type", out var type) && type.GetString() == "message")
            .SelectMany(x => x.GetProperty("content").EnumerateArray())
            .First(x => x.TryGetProperty("type", out var type) && type.GetString() == "output_text").GetProperty("text").GetString();
        if (string.IsNullOrWhiteSpace(outputText)) yield break;
        using var result = JsonDocument.Parse(outputText);
        foreach (var item in result.RootElement.GetProperty("companies").EnumerateArray())
        {
            var parsed = new ResearchProviderResult(item.GetProperty("sourceId").GetString()!, item.GetProperty("companyName").GetString()!, item.GetProperty("country").GetString()!, item.GetProperty("website").GetString()!, item.GetProperty("industry").GetString()!, item.GetProperty("summary").GetString()!, item.GetProperty("evidenceUrl").GetString()!, DateOnly.Parse(item.GetProperty("observedDate").GetString()!));
            if (!string.IsNullOrWhiteSpace(parsed.EvidenceUrl)) yield return parsed;
        }
    }

    public object CreatePayload(ResearchQuery query) => new
    {
        model = _options.Model,
        tools = new[] { new { type = "web_search" } },
        input = $"Find up to {query.Limit} public B2B company prospects. Regions: {string.Join(", ", query.Regions)}. Industries: {string.Join(", ", query.Industries)}. Product keywords: {string.Join(", ", query.Keywords)}. Return only publicly verifiable company facts with source URLs and observation dates.",
        text = new { format = new { type = "json_schema", name = "prospect_companies", strict = true, schema = Schema } }
    };

    private static readonly object Schema = new { type = "object", additionalProperties = false, required = new[] { "companies" }, properties = new { companies = new { type = "array", items = new { type = "object", additionalProperties = false, required = new[] { "sourceId", "companyName", "country", "website", "industry", "summary", "evidenceUrl", "observedDate" }, properties = new { sourceId = new { type = "string" }, companyName = new { type = "string" }, country = new { type = "string" }, website = new { type = "string" }, industry = new { type = "string" }, summary = new { type = "string" }, evidenceUrl = new { type = "string" }, observedDate = new { type = "string" } } } } } };
}
