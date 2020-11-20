module Falco.Host

open Microsoft.AspNetCore.Hosting    
open Microsoft.Extensions.Hosting

/// Specifies the process of configuring the IWebHost builder
type ConfigureWebHost = IWebHostBuilder -> IWebHostBuilder

/// Create and start a new IHost (Alias for Host.ConfigureWebHostDefaults(args).Build().Run())
let startWebHost =
    fun (args : string[]) (webHostBuilder : ConfigureWebHost) ->          
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(fun webhost -> webHostBuilder webhost |> ignore)
        .Build()
        .Run()
