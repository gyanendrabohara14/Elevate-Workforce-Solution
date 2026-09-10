using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ElevateHire.Application.AI;
using ElevateHire.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ElevateHire.Infrastructure.AI;

public sealed class GeminiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
    public string Model { get; set; } = "gemini-3.6-flash";
}

public class GeminiService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiService> _logger;

    public GeminiService(HttpClient httpClient, IOptions<GeminiOptions> options, ILogger<GeminiService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsEnabled => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<JobDescriptionResponse> GenerateJobDescriptionAsync(JobDescriptionRequest request)
    {
        if (!IsEnabled)
            return Unavailable<JobDescriptionResponse>();

        try
        {
            var prompt = BuildJobDescriptionPrompt(request);
            var text = await CallGeminiAsync(prompt);

            if (string.IsNullOrWhiteSpace(text))
                return Unavailable<JobDescriptionResponse>();

            var sections = ParseSections(text);
            return new JobDescriptionResponse
            {
                Description = sections.GetValueOrDefault("Description", text),
                Responsibilities = sections.GetValueOrDefault("Responsibilities", string.Empty),
                Requirements = sections.GetValueOrDefault("Requirements", string.Empty),
                Qualifications = sections.GetValueOrDefault("Qualifications", string.Empty),
                Skills = sections.GetValueOrDefault("Skills", string.Empty),
                Benefits = sections.GetValueOrDefault("Benefits", string.Empty),
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini job description generation failed.");
            return Unavailable<JobDescriptionResponse>(ex.Message);
        }
    }

    public async Task<string> GenerateCoverLetterAsync(string candidateProfile, string jobDescription)
    {
        if (!IsEnabled)
            return string.Empty;

        try
        {
            var prompt = $"""
                You are a professional career coach helping a job seeker in Nepal.
                Using the candidate profile and job description below, write a professional cover letter (200-260 words).
                The candidate will review and edit it themselves. Do not invent facts, and clearly placeholders like [Your Name] where a name must be inserted.

                CANDIDATE PROFILE:
                {candidateProfile}

                JOB DESCRIPTION:
                {jobDescription}

                COVER LETTER:
                """;
            return (await CallGeminiAsync(prompt)).Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini cover letter generation failed.");
            return string.Empty;
        }
    }

    public async Task<ResumeMatchingResponse> GetCandidateInsightsAsync(ResumeMatchingRequest request)
    {
        if (!IsEnabled)
            return Unavailable<ResumeMatchingResponse>();

        try
        {
            var prompt = $$"""
                You are an experienced technical recruiter reviewing a candidate for a job.
                Compare the candidate's profile, skills, and experience against the job requirements and description.

                CANDIDATE PROFILE:
                {{request.CandidateProfile}}

                JOB REQUIREMENTS:
                {{request.JobRequirements}}

                JOB DESCRIPTION:
                {{request.JobDescription}}

                Respond ONLY with valid JSON matching this exact schema:
                {
                  "matchScore": 0,
                  "matchingSkills": [],
                  "missingSkills": [],
                  "relevantExperience": "",
                  "strengths": [],
                  "potentialGaps": [],
                  "recommendation": "",
                  "explanation": ""
                }
                matchScore must be an integer between 0 and 100. This is advisory only.
                """;

            var text = await CallGeminiAsync(prompt);
            var json = ExtractJson(text);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new ResumeMatchingResponse
            {
                MatchScore = Math.Clamp(root.GetProperty("matchScore").GetInt32(), 0, 100),
                MatchingSkills = ReadStringArray(root, "matchingSkills"),
                MissingSkills = ReadStringArray(root, "missingSkills"),
                RelevantExperience = ReadString(root, "relevantExperience"),
                Strengths = ReadStringArray(root, "strengths"),
                PotentialGaps = ReadStringArray(root, "potentialGaps"),
                Recommendation = ReadString(root, "recommendation"),
                Explanation = ReadString(root, "explanation"),
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini candidate insights generation failed.");
            return Unavailable<ResumeMatchingResponse>(ex.Message);
        }
    }

    private async Task<string> CallGeminiAsync(string prompt)
    {
        var url = $"{_options.Endpoint}/models/{_options.Model}:generateContent?key={_options.ApiKey}";

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = prompt } }
                }
            },
            generationConfig = new { temperature = 0.7, maxOutputTokens = 4096 }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Gemini API returned {Status}: {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"Gemini API error: {response.StatusCode}");
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var candidates = doc.RootElement.GetProperty("candidates");
        if (candidates.GetArrayLength() == 0)
            throw new InvalidOperationException("Gemini returned no candidates.");

        var parts = candidates[0].GetProperty("content").GetProperty("parts");
        var text = new StringBuilder();
        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("text", out var t))
                text.Append(t.GetString());
        }
        return text.ToString();
    }

    private static T Unavailable<T>(string? message = null) where T : class, new()
    {
        var result = new T();
        if (result is JobDescriptionResponse job)
        {
            job.Success = false;
            job.Error = message ?? "AI assistance is temporarily unavailable.";
        }
        else if (result is ResumeMatchingResponse match)
        {
            match.Success = false;
            match.Error = message ?? "AI assistance is temporarily unavailable.";
        }
        return result;
    }

    private static Dictionary<string, string> ParseSections(string text)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string[] headers = { "Description", "Responsibilities", "Requirements", "Qualifications", "Skills", "Benefits" };
        var positions = new List<(string Name, int Index)>();

        foreach (var h in headers)
        {
            int idx = text.IndexOf(h, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0) positions.Add((h, idx));
        }
        positions = positions.OrderBy(p => p.Index).ToList();

        for (int i = 0; i < positions.Count; i++)
        {
            int start = positions[i].Index + positions[i].Name.Length;
            int end = i + 1 < positions.Count ? positions[i + 1].Index : text.Length;
            var value = text[start..end].Trim().TrimStart(':', ' ', '\n', '\r', '-');
            result[positions[i].Name] = value;
        }

        if (result.Count == 0)
            result["Description"] = text;

        return result;
    }

    private static string ExtractJson(string text)
    {
        int start = text.IndexOf('{');
        int end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
            throw new InvalidOperationException("Gemini response contained no JSON.");
        return text[start..(end + 1)];
    }

    private static string ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() ?? string.Empty : string.Empty;

    private static List<string> ReadStringArray(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.Array)
            return new List<string>();
        return el.EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.String)
            .Select(e => e.GetString() ?? string.Empty)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    private static string BuildJobDescriptionPrompt(JobDescriptionRequest request) =>
        $"""
        You are an expert hiring specialist. Write a complete, professional job posting for a {request.Position} role
        at {(string.IsNullOrWhiteSpace(request.CompanyName) ? "an anonymous company" : request.CompanyName)} based in {request.Location}.

        Experience required: {request.Experience}
        Relevant skills: {request.Skills}
        Job type: {request.JobType}
        Additional context: {request.AdditionalNotes ?? "None"}

        Return the result exactly in the following numbered sections. Use plain text, no markdown headers:
        Description
        Responsibilities
        Requirements
        Qualifications
        Skills
        Benefits

        Write naturally, avoid generic marketing phrases, and keep each section concise and genuinely useful.
        """;
}