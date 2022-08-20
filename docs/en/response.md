# Response Writing

The `Response` module contains a series of functions that provide support for the most common response types.

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

// Automatically protect against XSS attacks
let secureHtmlHandler : HttpHandler =
    let html token =
        Elem.html [] [
            Elem.body [] [
                Elem.form [ Attr.method "post" ] [
                    Elem.input [ Attr.name "first_name" ]

                    Elem.input [ Attr.name "last_name" ]

                    // using the CSRF HTML helper
                    Xss.antiforgeryInput token

                    Elem.input [ Attr.type' "submit"; Attr.value "Submit" ]
                ]
            ]
        ]

    Response.ofHtmlCsrf html
```

Alternatively, if you're using an external view engine and want to return an HTML response from a string literal, then you can use `Response.ofHtmlString`.

```fsharp
let htmlHandler : HttpHandler =
    Response.ofHtmlString "<html>...</html>"
```

## JSON responses

These handlers uses the .NET built-in `System.Text.Json.JsonSerializer`.

```fsharp
type Person =
    { First : string
      Last  : string }

let jsonHandler : HttpHandler =
    let name = { First = "John"; Last = "Doe" }
    Response.ofJson name

let jsonOptionsHandler : HttpHandler =
    let options = JsonSerializerOptions()
    options.IgnoreNullValues <- true
    let name = { First = "John"; Last = "Doe" }
    Response.ofJson options name
```

## Redirect (301/302) Response

```fsharp
let oldUrlHandler : HttpHandler =
    Response.redirectPermanently "/new-url" // HTTP 301

let redirectUrlHandler : HttpHandler =
    Response.redirectTemporarily "/new-url" // HTTP 302
```

## Content Disposition

```fsharp
let inlineBinaryHandler : HttpHandler =
    let contentType = "image/jpeg"
    let headers = [ HeaderNames.CacheControl,  "no-store, max-age=0" ]
    let bytes = // ... binary data
    Response.ofBinary contentType headers bytes

let attachmentHandler : HttpHandler =
    let filename = "profile.jpg"
    let contentType = "image/jpeg"
    let headers = [ HeaderNames.CacheControl,  "no-store, max-age=0" ]
    let bytes = // ... binary data
    Response.ofAttachment filename contentType headers bytes
```

## Response Modifiers

Response modifiers can be thought of as the in-and-out modification of the `HttpResponse`. A preamble to writing and returning. Since these functions receive the `Httpcontext` as input and return it as the only output, they can take advantage of function compoistion.

### Set the status code of the response

```fsharp
let notFoundHandler : HttpHandler =
    Response.withStatusCode 404
    >> Response.ofPlainText "Not found"
```

### Add a header(s) to the response

```fsharp
let handlerWithHeader : HttpHandler =
    Response.withHeader "Content-Language" "en-us"
    >> Response.ofPlainText "Hello world"

let handlerWithHeaders : HttpHandler =
    Response.withHeaders [ "Content-Language" "en-us" ]
    >> Response.ofPlainText "Hello world"
```


### Add a cookie to the response

> IMPORTANT: *Do not* use this for authentication. Instead use the `Auth.signIn` and `Auth.signOut` functions found in the [Authentication](security.md) module.

```fsharp
let handlerWithCookie : HttpHandler =
    Response.withCookie "greeted" "1"
    >> Response.ofPlainText "Hello world"

let handlerWithCookieOptions : HttpHandler =
    let options = CookieOptions()
    options.Expires <- DateTime.Now.Minutes(15)
    Response.withCookie options "greeted" "1"
    >> Response.ofPlainText "Hello world"
```
