namespace ElevateHire.Application.AI;

public class JobDescriptionRequest
{
    public string Position { get; set; } = string.Empty;
    public string Experience { get; set; } = string.Empty;
    public string Skills { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? AdditionalNotes { get; set; }
}
