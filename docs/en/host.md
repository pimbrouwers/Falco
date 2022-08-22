# Host Configuration

[Kestrel](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel) is the web server at the heart of ASP.NET. It's performant, secure, and maintained by incredibly smart people. To make things more expressive, Falco exposes an optional computation expression. Below is an example using the expression, taken from the [Configure Host](https://github.com/pimbrouwers/Falco/tree/master/samples/ConfigureHost) sample.

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

## Registering Services

| Operation | Description |
| --------- | ----------- |
| [add_antiforgery](#add_antiforgery) | Add Antiforgery support into the `IServiceCollection`. |
| [add_cookie](#add_cookie) | Add default cookie authentication into the `IServiceCollection`. |
| [add_conf_cookies](#add_conf_cookies) | Add configured cookie(s) authentication into the `IServiceCollection`. |
| [add_authorization](#add_authorization) | Add default Authorization into the `IServiceCollection`. |
| [add_data_protection](#add_data_protection) | Add file system based data protection. |
| [add_http_client](#add_http_client) | Add IHttpClientFactory into the `IServiceCollection` |

### `add_antiforgery`

```fsharp
webHost [||] {
    add_antiforgery

    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

### `add_cookie`

```fsharp
webHost [||] {
    add_cookie

    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

### `add_conf_cookies`

```fsharp
let appAuthScheme = "MyApp"

let authConfig
    (scheme : string)
    (opetions : AuthenticationOptions) =
    options.DefaultChallengeScheme <- scheme
    options.DefaultAuthenticateScheme <- scheme
    options.DefaultScheme <- scheme

let cookieConfig
    let cookieOptions
        (scheme : string)
        (options : CookieAuthenticationOptions) =
        options.Cookie.Path <- "/"
        options.Cookie.HttpOnly <- true
        options.Cookie.SameSite <- SameSiteMode.Strict
        options.Cookie.SecurePolicy <- CookieSecurePolicy.Always

    [ appAuthScheme, cookieOptions appAuthScheme ]

webHost [||] {
    endpoints [
        add_conf_cookies authConfig cookieConfig

        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

### `add_authorization`

```fsharp
webHost [||] {
    add_authorization

    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

### `add_data_protection`

```fsharp
webHost [||] {
    add_data_protection "C:\\Data\\Protection\\Dir"

    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

### `add_http_client`

```fsharp
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
| [use_if](#use_if) | Use the specified middleware if the provided predicate is "true". |
| [use_ifnot](#use_ifnot) | Use the specified middleware if the provided predicate is "true". |
| [use_authentication](#use_authentication) | Use authorization middleware. Call before any middleware that depends on users being authenticated. |
| [use_authorization](#use_authorization) | Register authorization service and enable middleware |
| [use_caching](#use_caching) | Register HTTP Response caching service and enable middleware. |
| [use_compression](#use_compression) | Register Brotli + GZip HTTP Compression service and enable middleware. |
| [use_hsts](#use_hsts) | Use automatic HSTS middleware (adds strict-transport-policy header). |
| [use_https](#use_https) | Use automatic HTTPS redirection. |
| [use_static_files](#use_static_files) | Use Static File middleware. |

### `use_if`

```fsharp
webHost [||] {
    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

### `use_ifnot`

```fsharp
webHost [||] {
    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

### `use_authentication`

```fsharp
webHost [||] {
    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

### `use_authorization`

```fsharp
webHost [||] {
    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

### `use_caching`

```fsharp
webHost [||] {
    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

### `use_compression`

```fsharp
webHost [||] {
    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

### `use_hsts`

```fsharp
webHost [||] {
    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

### `use_https`

```fsharp
webHost [||] {
    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

### `use_static_files`

```fsharp
webHost [||] {
    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```


## Other Operations

| Operation | Description |
| --------- | ----------- |
| [logging](#logging) | Configure logging via `ILogger`. |
| [add_service](#add_service) | Add a new service descriptor into the `IServiceCollection`. |
| [use_middleware](#use_middleware) | Use the specified middleware. |
| [not_found](#not_found) | Include a catch-all (i.e., Not Found) HttpHandler (must be added last). |

### `logging`

```fsharp
webHost [||] {
    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

### `add_service`

```fsharp
webHost [||] {
    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

### `use_middleware`

```fsharp
webHost [||] {
    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```

### `not_found`

```fsharp
webHost [||] {
    endpoints [
        get "/" (Response.ofPlainText "Hello world")
    ]
}
```


## Registering Custom Services

## Activating Custom Middleware
