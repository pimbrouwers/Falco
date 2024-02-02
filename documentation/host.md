# Host Configuration

The `webHost[||] { ... }` computation aims to provide a consistent methodology for creating .NET servers using [Kestrel](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel). It's performant, secure, and maintained by the incredibly smart people on the ASP.NET Team at Microsoft.

## Config, Logging & Other Operations

| Operation | Description |
| --------- | ----------- |
| [host](#host) | Configure the host via `IHostBuilder`. |
| [web_host](#web_host) | Configure the host via `IHostBuilder`. |
| [logging](#logging) | Configure logging via `ILoggingBuilder`. |
| [not_found](#not_found) | Include a catch-all (i.e., Not Found) HttpHandler (must be added last). |

### `host`

```fsharp
open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.Extensions.Hosting
open Serilog

let configureHost (host : IHostBuilder) =
    host.AddSerilog()
    host

webHost [||] {
    host configureHost

    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

### `web_host`

```fsharp
open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.Extensions.Hosting
open Serilog

let configureWebHost (webHost : IWebHostBuilder) =
    webHost.UseHttpSys()
    webHost

webHost [||] {
    web_host configureWebHost

    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

### `logging`

```fsharp
open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Logging

let configureLogging (log : ILoggingBuilder) =
    log.ClearProviders()
    log.AddConsole()
    log

webHost [||] {
    logging configureLogging

    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

### `not_found`

```fsharp
open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.AspNetCore.Builder

module ErrorPages =
    let unauthorized : HttpHandler =
        Response.withStatusCode 404
        >> Response.ofPlainText "Not Found"

webHost [||] {
    not_found ErrorPages.notFound

    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

## Registering Services

| Operation | Description |
| --------- | ----------- |
| [add_service](#add_service) | Add a new service descriptor into the IServiceCollection. |
| [add_antiforgery](#add_antiforgery) | Add Antiforgery support into the `IServiceCollection`. |
| [add_cookie](#add_cookie) | Add configured cookie into the `IServiceCollection`. |
| [add_cookies](#add_cookies) | Add configured cookie collection into the `IServiceCollection`. |
| [add_authorization](#add_authorization) | Add default Authorization into the `IServiceCollection`. |
| [add_data_protection](#add_data_protection) | Add file system based data protection. |
| [add_http_client](#add_http_client) | Add IHttpClientFactory into the `IServiceCollection` |

### `add_antiforgery`

```fsharp
open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.AspNetCore.Builder

webHost [||] {
    add_antiforgery

    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

### `add_cookie`

```fsharp
open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.AspNetCore.Builder

let cookieScheme = "MyAppScheme"

webHost [||] {
    add_cookie cookieScheme

    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

### `add_cookies`

```fsharp
open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Builder

let cookieScheme = "MyApp"

let authConfig
    (scheme : string)
    (options : AuthenticationOptions) =
    options.DefaultScheme <- scheme

let cookieConfig
    let cookieOptions
        (scheme : string)
        (options : CookieAuthenticationOptions) =
        options.AccessDeniedPath <- "/account/denied"
        options.LoginPath <- "/account/login"
        options.Cookie.Path <- "/"
        options.Cookie.HttpOnly <- true
        options.Cookie.SameSite <- SameSiteMode.Strict
        options.Cookie.SecurePolicy <- CookieSecurePolicy.Always

    [ cookieScheme, cookieOptions cookieScheme ]

webHost [||] {
    endpoints [
        add_cookies authConfig cookieConfig

        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

### `add_authorization`

```fsharp
open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.AspNetCore.Builder

webHost [||] {
    add_authorization

    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

### `add_data_protection`

```fsharp
open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.AspNetCore.Builder

webHost [||] {
    add_data_protection "C:\\Data\\Protection\\Dir"

    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

### `add_http_client`

```fsharp
open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.AspNetCore.Builder

webHost [||] {
    add_http_client

    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

## Activating Middleware

| Operation | Description |
| --------- | ----------- |
| [use_middleware](#use_middleware) | Use the specified middleware. |
| [use_if](#use_if) | Use the specified middleware if the provided predicate is "true". |
| [use_ifnot](#use_ifnot) | Use the specified middleware if the provided predicate is "true". |
| [use_authentication](#use_authentication) | Use authorization middleware. Call before any middleware that depends on users being authenticated. |
| [use_authorization](#use_authorization) | Register authorization service and enable middleware |
| [use_caching](#use_caching) | Register HTTP Response caching service and enable middleware. |
| [use_compression](#use_compression) | Register Brotli + GZip HTTP Compression service and enable middleware. |
| [use_hsts](#use_hsts) | Use automatic HSTS middleware (adds strict-transport-policy header). |
| [use_https](#use_https) | Use automatic HTTPS redirection. |
| [use_cors](#use_cors) | Set CORS (Cross Origin Resource Sharing) options and policy. |
| [use_default_files](#use_default_files) | Use Default Files middleware. |
| [use_static_files](#use_static_files) | Use Static Files middleware. |

### `use_if`

```fsharp
open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.AspNetCore.Builder

webHost [||] {
    use_if    FalcoExtensions.IsDevelopment DeveloperExceptionPageExtensions.UseDeveloperExceptionPage

    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

### `use_ifnot`

```fsharp
open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.AspNetCore.Builder

webHost [||] {
    use_ifnot FalcoExtensions.IsDevelopment HstsBuilderExtensions.UseHsts

    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

### `use_authentication`

> Note: this must be called **before** `use_authorization`, and called **after** `use_hsts`, `use_http`, `use_compression`, `use_static_files`.

```fsharp
open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.AspNetCore.Builder

webHost [||] {
    use_authentication

    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

### `use_authorization`

> Note: this must be called **after** `use_authentication`.

```fsharp
open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.AspNetCore.Builder

webHost [||] {
    use_authorization

    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

### `use_caching`

```fsharp
open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.AspNetCore.Builder

webHost [||] {
    use_caching

    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

### `use_compression`

> Note: this should be called **before** `use_static_files` if compression is desired on static assets.

In addition to the [default MIME types](https://docs.microsoft.com/en-us/aspnet/core/performance/response-compression#mime-types), this enables compression for the following: `image/jpeg`, `image/png`, `image/svg+xml`, `font/woff`, `font/woff2'`.

```fsharp
open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.AspNetCore.Builder

webHost [||] {
    use_compression

    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

### `use_hsts`

> Note: this should be called **before** `use_https`.

```fsharp
open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.AspNetCore.Builder

webHost [||] {
    use_hsts

    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

### `use_https`

```fsharp
open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.AspNetCore.Builder

webHost [||] {
    use_https

    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

### `use_cors`

> Note: Typically, `use_static_files` is called before `use_cors`. But apps that use JavaScript to retrieve static files cross site must call `use_cors` before `use_static_files`.

See [the official docs](https://learn.microsoft.com/en-us/aspnet/core/security/cors) for all the options. Only "[CORS with named policy and Middleware](https://learn.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-7.0#cors-with-named-policy-and-middleware)" is supported.

```fsharp
open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.AspNetCore.Cors.Infrastructure

let corsPolicyName = "MyCorsPolicy"

let corsPolicy (policyBuilder: CorsPolicyBuilder) =
    // Note: This is a very lax setting, but a good fit for local development
    policyBuilder
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
        // Note: The URLs must not end with a /
        .WithOrigins("http://localhost:3000",
                     "http://localhost:5001")
    |> ignore

let corsOptions (options : CorsOptions) =
    options.AddPolicy(corsPolicyName, corsPolicy)

webHost [||] {
    use_cors corsPolicyName corsOptions

    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```


### `use_default_files`

```fsharp
open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.AspNetCore.Builder

webHost [||] {
    use_default_files
    use_static_files

    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

### `use_static_files`

```fsharp
open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.AspNetCore.Builder

webHost [||] {
    use_static_files

    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

## Custom Services and Middleware

| Operation | Description |
| --------- | ----------- |
| [add_service](#add_service) | Add a new service descriptor into the IServiceCollection. |
| [use_middleware](#use_middleware) | Use the specified middleware. |


### `add_service`

> Note the use of the [`ConfigurationBuiler`](#configuration-builder)

```fsharp
open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.AspNetCore.Builder
open Microsoft.Data.Sqlite

type IDbConnectionFactory =
    abstract member CreateConnection : unit -> IDbConnection

let connectToDb : HttpHandler =
    Services.inject<IDbConnectionFactory>(fun db ->
        use conn = db.CreateConnection()
        // code which uses IDbConnection...
    )

type DbConnectionFactory (connectionString : string) =
    interface IDbConnectionFactory with
        member _.CreateConnection () =
            let conn = new SqliteConnection(connectionString)
            conn.TryOpenConnection()
            conn

[<EntryPoint>]
let main args =
    // Using the ConfigurationBuilder
    let config = configuration [||] {
        required_json "appsettings.json"
    }

    // Register our database connection factory service
    let dbConnectionService (svc : IServiceCollection) =
        svc.AddSingleton<IDbConnectionFactory, DbConnectionFactory>(fun _ ->
            // Load default connection string from appsettings.json
            let connectionString = config.GetConnectionString("Default")
            new DbConnectionFactory(connectionString))

    webHost [||] {
        endpoints [
            get "/" (Response.ofPlainText "Hello world")
        ]
    }
    0
```

### `use_middleware`

```fsharp
open System.Globalization
open System.Threading.Tasks
open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http

let displayCulture : HttpHandler = fun ctx ->
    Response.ofPlainText CultureInfo.CurrentCulture.DisplayName ctx

let cultureMiddleware (app : IApplicationBuilder) =
    let middleware (ctx : HttpContext) (next : RequestDelegate) : Task =
        task {
            let query = QueryCollectionReader(ctx.Request.Query)
            match query.TryGet "culture" with
            | Some cultureQuery ->
                let culture = CultureInfo(cultureQuery)
                CultureInfo.CurrentCulture <- culture
                CultureInfo.CurrentUICulture <- culture
            | None -> ()

            return! next.Invoke(ctx)
        }

    app.Use(middleware)

webHost [||] {
    use_middleware cultureMiddleware

    endpoints [
        any "/" displayCulture
    ]
}
```


[Next: Security](security.md)