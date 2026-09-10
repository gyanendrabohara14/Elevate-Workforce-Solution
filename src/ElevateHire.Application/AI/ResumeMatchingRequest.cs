namespace ElevateHire.Application.AI;

public class ResumeMatchingRequest
{
    public string CandidateProfile { get; set; } = string.Empty;
    public string JobRequirements { get; set; } = string.Empty;
    public string JobDescription { get; set; } = string.Empty;
}
