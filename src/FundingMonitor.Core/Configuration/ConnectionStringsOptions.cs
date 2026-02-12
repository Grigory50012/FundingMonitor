namespace FundingMonitor.Core.Configuration;

public class ConnectionStringsOptions
{
    public const string SectionName = "ConnectionStrings";
    
    public string DefaultConnection { get; set; } = string.Empty;
}