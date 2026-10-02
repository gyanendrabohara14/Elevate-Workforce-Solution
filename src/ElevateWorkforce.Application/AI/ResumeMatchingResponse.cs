namespace ElevateWorkforce.Application.AI;

public class ResumeMatchingResponse
{
    public int MatchScore { get; set; }
    public List<string> MatchingSkills { get; set; } = new();
    public List<string> MissingSkills { get; set; } = new();
    public string RelevantExperience { get; set; } = string.Empty;
    public List<string> Strengths { get; set; } = new();
    public List<string> PotentialGaps { get; set; } = new();
    public string Recommendation { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Error { get; set; }
}
