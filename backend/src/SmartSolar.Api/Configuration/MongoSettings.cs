// ---------------------------------------------------------------------------
//  File        : MongoSettings.cs
//  Project     : Smart Solar Microgrid Trading System - Web API
//  Module      : Configuration
//  Description : Strongly typed MongoDB settings bound from configuration.
//                The connection string is supplied by appsettings.Development.json
//                or an environment variable and is never committed.
//  Author      : <member name - M2>
//  Created     : 2026-09-13
// ---------------------------------------------------------------------------

namespace SmartSolar.Api.Configuration;

public class MongoSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "smartsolar";
}
