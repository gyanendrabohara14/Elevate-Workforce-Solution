using ElevateHire.Application.AI;

namespace ElevateHire.Application.Interfaces;

public interface IAIService
{
    Task<JobDescriptionResponse> GenerateJobDescriptionAsync(JobDescriptionRequest request);
    Task<string> GenerateCoverLetterAsync(string candidateProfile, string jobDescription);
    Task<ResumeMatchingResponse> GetCandidateInsightsAsync(ResumeMatchingRequest request);
    bool IsEnabled { get; }
}
