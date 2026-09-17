namespace SmartSolar.Api.Configuration;

public class MongoSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "smartsolar";
}
