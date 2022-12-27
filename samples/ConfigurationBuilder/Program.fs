module ConfigurationBuilder.Program

open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging

/// App configuration, loaded on startup
let config = configuration [||] {
    required_json "appsettings.json"
    optional_json "appsettings.Development.json"
}

/// GET /
let handleConfigRead : HttpHandler =
    // Note: colon-separated to access nested values
    Response.ofJson {|
        LogLevel = config.GetValue<string>("Logging:LogLevel")
        ConnectionString = config.GetConnectionString("default") |}

webHost [||] {
    logging (fun logging ->
        logging
            .ClearProviders()
            .AddSimpleConsole()
            .AddConfiguration(config)
    )

    endpoints [
        get "/" handleConfigRead
    ]
}
