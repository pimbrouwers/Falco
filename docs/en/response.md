# Response Writing

## Plain Text responses

```fsharp
let textHandler : HttpHandler =
    Response.ofPlainText "hello world"
```

## HTML responses

```fsharp
let htmlHandler : HttpHandler =
    let html =
        Elem.html [ Attr.lang "en" ] [
            Elem.head [] []
            Elem.body [] [
                Elem.h1 [] [ Text.raw "Sample App" ]
            ]
        ]

    Response.ofHtml html
```

Alternatively, if you're using an external view engine and want to return an HTML response from a string literal, then you can use `Response.ofHtmlString`.

```fsharp
let htmlHandler : HttpHandler =
    Response.ofHtmlString "<html>...</html>"
```

## JSON responses

> IMPORTANT: This handler uses the default `System.Text.Json.JsonSerializer`. See [JSON](#json) section below for further information.

```fsharp
type Person =
    { First : string
      Last  : string }

let jsonHandler : HttpHandler =
    let name = { First = "John"; Last = "Doe" }
    Response.ofJson name
```

## Redirect (301/302) Response

```fsharp
let oldUrlHandler : HttpHandler =
    Response.redirectPermanently "/new-url" // HTTP 301

let redirectUrlHandler : HttpHandler =
    Response.redirectTemporarily "/new-url" // HTTP 302
```

## Response Modifiers

Response modifiers can be thought of as the in-and-out modification of the `HttpResponse`. A preamble to writing and returning. Since these functions receive the `Httpcontext` as input and return it as the only output, they can take advantage of [function compoistion](22).

### Set the status code of the response

```fsharp
let notFoundHandler : HttpHandler =
    Response.withStatusCode 404
    >> Response.ofPlainText "Not found"
```

### Add a header to the response

```fsharp
let handlerWithHeader : HttpHandler =
    Response.withHeader "Content-Language" "en-us"
    >> Response.ofPlainText "Hello world"
```


### Add a cookie to the response

```fsharp
let handlerWithHeader : HttpHandler =
    Response.withCookie "greeted" "1"
    >> Response.ofPlainText "Hello world"
```

> IMPORTANT: *Do not* use this for authentication. Instead use the `Auth.signIn` and `Auth.signOut` functions found in the [Authentication](#authentication) module.
