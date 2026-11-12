namespace Dewi;

internal static class CareerRoles
{
    public const string SoftwareDeveloperIntern = "Software Developer Intern";
    public const string AiAutomationIntern = "AI & Automation Intern";
    public const string SoftwareEngineer = "Software Engineer";
    public const string GeneralInterest = "General Interest";

    public static string? ApplicationType(string role) => role switch
    {
        SoftwareDeveloperIntern or AiAutomationIntern => "Internship",
        SoftwareEngineer => "Full-time",
        GeneralInterest => GeneralInterest,
        _ => null
    };
}
