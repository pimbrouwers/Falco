# Host Builder

[Kestrel][1] is the web server at the heart of ASP.NET. It's performant, secure, and maintained by incredibly smart people. To make things more expressive, Falco exposes an optional computation expression. Below is an example using the expression, taken from the [Configure Host](https://github.com/pimbrouwers/Falco/tree/master/samples/ConfigureHost) sample.

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
            get "/greet/{name:alpha}" handleGreeting

            get "/json" handleJson

            get "/html" handleHtml

            any "/" handlePlainText
        ]
    }
    0
```

## Built-in Operations

The following built-in custom operations are available to make registering services and activating middleware simpler.

### Registering Services

| Operation | Description |
| --------- | ----------- |
| add_antiforgery | Add Antiforgery support into the IServiceCollection. |
| add_cookie | Add default cookie authentication into the IServiceCollection. |
| add_conf_cookies | Add configured cookie(s) authentication into the IServiceCollection. |
| add_authorization | Add default Authorization into the IServiceCollection. |
| add_data_protection | Add file system based data protection. |
| add_http_client | Add IHttpClientFactory into the IServiceCollection |

### Activating Middleware


| Operation | Description |
| --------- | ----------- |
| use_middleware | Use the specified middleware. |
| use_if | Use the specified middleware if the provided predicate is "true". |
| use_ifnot | Use the specified middleware if the provided predicate is "true". |
| use_authentication | /// Use authorization middleware. Call before any middleware that depends on users being authenticated. |
| use_authorization | Register authorization service and enable middleware |
| use_cachine | Register HTTP Response caching service and enable middleware. |
| use_compression | Register Brotli + GZip HTTP Compression service and enable middleware. |
| use_hsts | Use automatic HSTS middleware (adds strict-transport-policy header). |
| use_https | Use automatic HTTPS redirection. |
| use_static_files | Use Static File middleware. |

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
