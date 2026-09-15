namespace ElevateWorkforce.Application.AI;

public class JobDescriptionResponse
{
    public string Description { get; set; } = string.Empty;
    public string Responsibilities { get; set; } = string.Empty;
    public string Requirements { get; set; } = string.Empty;
    public string Qualifications { get; set; } = string.Empty;
    public string Skills { get; set; } = string.Empty;
    public string Benefits { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Error { get; set; }
}
