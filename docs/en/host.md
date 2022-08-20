## Host Builder

[Kestrel][1] is the web server at the heart of ASP.NET. It's performant, secure, and maintained by incredibly smart people. Getting it up and running is usually done using `Host.CreateDefaultBuilder(args)`, but it can grow verbose quickly.

To make things more expressive, Falco exposes an optional computation expression. Below is an example using the builder taken from the [Configure Host][21] sample.

```fsharp
[<EntryPoint>]
let main args =
    webHost args {
        use_ifnot FalcoExtensions.IsDevelopment HstsBuilderExtensions.UseHsts
        use_https
        use_compression
        use_static_files

        use_if    FalcoExtensions.IsDevelopment DeveloperExceptionPageExtensions.UseDeveloperExceptionPage
        use_ifnot FalcoExtensions.IsDevelopment (FalcoExtensions.UseFalcoExceptionHandler exceptionHandler)

        endpoints [
            get "/greet/{name:alpha}"
                handleGreeting

            get "/json"
                handleJson

            get "/html"
                handleHtml

            get "/"
                handlePlainText
        ]
    }
    0
```

### Fully Customizing the Host

To assume full control over configuring your `IHost` use the `configure` custom operation. It expects a function with the signature of `HttpEndpoint list -> IWebHostBuilder -> IWebHostBuilder` and assumes you will register and activate Falco (i.e., `AddFalco()` and `UseFalco(endpoints)`).

```fsharp
[<EntryPoint>]
let main args =
    let configureServices : IServiceCollection -> unit =
      fun services -> services.AddFalco() |> ignore

    let configureApp : HttpEndpoint list -> IApplicationBuilder -> unit =
       fun endpoints app -> app.UseFalco(endpoints) |> ignore

    let configureWebHost : HttpEndpoint list -> IWebHostBuilder =
      fun endpoints webHost ->
          webHost.ConfigureLogging(configureLogging)
                 .ConfigureServices(configureServices)
                 .Configure(configureApp endpoints)

    webHost args {
      configure configureWebHost
      endpoints []
    }
```
