namespace ElevateHire.Domain.Enums;

public enum JobType
{
    FullTime,
    PartTime,
    Contract,
    Internship,
    Remote
}

public enum JobStatus
{
    PendingVerification,
    Active,
    Rejected,
    Closed,
    Expired,
    Removed
}

public enum JobCategory
{
    SoftwareDevelopment,
    Design,
    Data,
    Marketing,
    Sales,
    HumanResources,
    Finance,
    Support,
    Operations,
    Other
}

public enum ExperienceLevel
{
    Entry,
    Junior,
    MidLevel,
    Senior,
    Lead
}