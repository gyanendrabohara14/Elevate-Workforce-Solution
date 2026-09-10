using ElevateHire.Application.AI;
using ElevateHire.Application.Interfaces;

namespace ElevateHire.Application.Services;

/// <summary>
/// Facade over the AI provider that guarantees the application never crashes
/// when the AI provider is unavailable or misbehaves.
/// </summary>
public class AIService : IAIService
{
    private readonly IAIService _inner;

    public AIService(IAIService inner) => _inner = inner;

    public bool IsEnabled => _inner.IsEnabled;

    public async Task<JobDescriptionResponse> GenerateJobDescriptionAsync(JobDescriptionRequest request)
    {
        var result = await _inner.GenerateJobDescriptionAsync(request);
        return result is { Success: false }
            ? UnavailableDescription()
            : result;
    }

    public async Task<string> GenerateCoverLetterAsync(string candidateProfile, string jobDescription)
    {
        try
        {
            return await _inner.GenerateCoverLetterAsync(candidateProfile, jobDescription);
        }
        catch
        {
            return string.Empty;
        }
    }

    public async Task<ResumeMatchingResponse> GetCandidateInsightsAsync(ResumeMatchingRequest request)
    {
        var result = await _inner.GetCandidateInsightsAsync(request);
        return result is { Success: false }
            ? UnavailableInsights()
            : result;
    }

    private static JobDescriptionResponse UnavailableDescription() => new()
    {
        Success = false,
        Error = "AI assistance is temporarily unavailable. You can continue manually."
    };

    private static ResumeMatchingResponse UnavailableInsights() => new()
    {
        Success = false,
        Error = "AI assistance is temporarily unavailable. You can continue manually."
    };
}