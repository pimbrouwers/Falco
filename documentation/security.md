# Security

## Authentication

ASP.NET Core has amazing built-in support for authentication. Review the [docs](https://docs.microsoft.com/en-us/aspnet/core/security/authentication) for specific implementation details. Falco includes some authentication utilities.

```fsharp
open Falco

let authScheme = "some.secure.scheme"

// Allow only authenticated access
let secureResourceHandler : HttpHandler =
    let handleAuth : HttpHandler =
        Response.ofPlainText "hello authenticated user"

    Request.ifAuthenticated authScheme handleAuth

// Allow only non-authenticated access
let anonResourceOnlyHandler : HttpHandler =
    let handleAnon : HttpHandler =
        Response.ofPlainText "hello anonymous"

    Request.ifNotAuthenticated authScheme handleAnon

// Allow only authenticated access when in certain role(s)
let secureResourceHandler : HttpHandler =
    let handleAuthInRole : HttpHandler =
        Response.ofPlainText "hello admin"

    let rolesAllowed = [ "Admin" ]

    Request.ifAuthenticatedInRole authScheme rolesAllowed handleAuthInRole

// Allow only authenticated acces with a certain scope
let secureResourceHandler : HttpHandler =
    let handleAuthHasScope : HttpHandler =
        Response.ofPlainText "user1, user2, user3"

    let issuer = "https://oauth2issuer.com"
    let scope = "read:users"

    Request.ifAuthenticatedWithScope authScheme issuer scope handleAuthHasScope

// Terminate authenticated session
let logOut : HttpHandler =
    let authScheme = "..."
    let redirectTo = "/login"

    Response.signOutAndRedirect authScheme redirectTo
```

## Cross-site Scripting (XSS) Attacks

Cross-site scripting attacks are extremely common since they are quite simple to carry out. Fortunately, protecting against them is as easy as performing them.

The [Microsoft.AspNetCore.Antiforgery](https://docs.microsoft.com/en-us/aspnet/core/security/anti-request-forgery) package provides the required utilities to easily protect yourself against such attacks.

Falco provides a few handlers via `Falco.Security.Xss`:

> To use the Xss helpers, ensure that the `Antiforgery` service has been registered.

```fsharp
open Falco.Markup
open Falco.Security

let formView token =
    Elem.html [] [
        Elem.body [] [
            Elem.form [ Attr.methodPost ] [
                // using the CSRF HTML helper, recommended to include as first
                // form element
                Xss.antiforgeryInput token
                Elem.control "first_name" [] [ Text.raw "First Name" ]
                Elem.control "first_name" [] [ Text.raw "First Name" ]
                Elem.input [ Attr.typeSubmit ]
            ]
        ]
    ]

// A handler that demonstrates obtaining a
// CSRF token and applying it to a view
let csrfViewHandler : HttpHandler =
    Response.ofHtmlCsrf formView

// A handler that demonstrates validating
// the request's CSRF token
let mapFormSecureHandler : HttpHandler =
    let mapPerson (form : FormData) =
        { FirstName = form?first_name.AsString()
          LastName = form?last_name.AsString }

    let handleInvalid : HttpHandler =
        Response.withStatusCode 400
        >> Response.ofEmpty

    Request.mapFormSecure mapPerson Response.ofJson handleInvalid
```