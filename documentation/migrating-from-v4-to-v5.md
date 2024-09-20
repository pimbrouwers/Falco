# Migrating from v4.x to v5.x

With Falco v5.x the main objective was to simplify the API and improve the overall devlopment experience.

This document will attempt to cover the anticipated transformations necessary to upgrade from v4.x to v5.x. Pull requests are welcome for missing scenarios, thank you in advance for your help.

## `webHost` expression

Perhaps the most significant change is the removal of the `webHost` expression, which attempted to make web application server construction more pleasant. Microsoft has made really nice strides in this area (i.e., `WebApplication`) and it's been difficult at times to stay sync with the breaking changes to the underlying interfaces. As such, we elected to remove it altogether.

Below demonstrates how to migrate a "hello world" app from v4 to v5 by replacing the `webHost` expression with the Microsoft provided `WebApplicationBuilder`.

<table>
<tr>
<td>

```fsharp
// Falco v4.x
open Falco

webHost args {

    use_static_files

    endpoints [
        get "/"
            (Response.ofPlainText "hello world")
    ]
}
```

</td>
<td>

```fsharp
// Falco v5.x
open Falco
open Microsoft.AspNetCore.Builder

let wapp = WebApplication.Create()

wapp.UseFalco([
        get "/" (Response.ofPlainText "Hello World!")
    ])
    .Run()

```

</td>
</tr>
</table>

## `StringCollectionReader` replaced by `RequestData`

For the most part, this upgrade won't require any changes for the end user. Especially if the continuation-style functions in the `Request` module were used.

Explicit references to: `CookieCollectionReader`, `HeaderCollectionReader`, `RouteCollectionReader`, `QueryCollectionReader` will need to be updated to `RequestData`. `FormCollectionReader` has been replaced by `FormData`.

## Form Streaming

Falco now automatically detects whether the form is transmiting `multipart/form-data`, which means deprecating the `Request` module streaming functions.

- `Request.streamForm` becomes -> `Request.mapForm`
- `Request.streamFormSecure` becomes -> `Request.mapFormSecure`
- `Request.mapFormStream`  becomes -> `Request.mapForm`
- `Request.mapFormStreamSecure` becomes -> `Request.mapFormSecure`
