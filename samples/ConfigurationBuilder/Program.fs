module ConfigurationBuilder.Program

open Falco
open Falco.Markup
open Falco.Routing
open Falco.HostBuilder
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging

let config : IConfiguration = configuration [||] {
    required_json "appsettings.json"
    optional_json "appsettings.Development.json"
}

let handleConfigRead : HttpHandler = fun ctx ->
    let response =
        // colon-separated to access nested values
        {| LogLevel = config.GetValue<string>("Logging:LogLevel")
           ConnectionString = config.GetConnectionString("default") |}

    Response.ofJson response ctx

[<EntryPoint>]
let main args =
    webHost args {
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

    0 // Exit code
