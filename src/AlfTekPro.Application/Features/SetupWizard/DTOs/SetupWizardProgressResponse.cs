namespace AlfTekPro.Application.Features.SetupWizard.DTOs;

public class SetupWizardProgressResponse
{
    public bool IsComplete { get; set; }
    public int CompletedSteps { get; set; }
    public int TotalSteps { get; set; }
    public decimal PercentComplete { get; set; }
    public List<SetupStep> Steps { get; set; } = new();
}

public class SetupStep
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
    public string NavigateTo { get; set; } = string.Empty;
    public int Order { get; set; }
}
