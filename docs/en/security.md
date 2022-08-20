## Authentication

ASP.NET Core has amazing built-in support for authentication. Review the [docs][13] for specific implementation details. Falco includes some authentication utilities.

> To use the authentication helpers, ensure the service has been registered (`AddAuthentication()`) with the `IServiceCollection` and activated (`UseAuthentication()`) using the `IApplicationBuilder`.

Prevent user from accessing secure endpoint:

```fsharp
open Falco.Security

let secureResourceHandler : HttpHandler =
    let handleAuth : HttpHandler =
        "hello authenticated user"
        |> Response.ofPlainText

    let handleInvalid : HttpHandler =
        Response.withStatusCode 403
        >> Response.ofPlainText "Forbidden"

    Request.ifAuthenticated handleAuth handleInvalid
```

Prevent authenticated user from accessing anonymous-only end-point:

```fsharp
let anonResourceOnlyHandler : HttpHandler =
    let handleAnon : HttpHandler =
        Response.ofPlainText "hello anonymous"

    let handleInvalid : HttpHandler =
        Response.withStatusCode 403
        >> Response.ofPlainText "Forbidden"

    Request.ifNotAuthenticated handleAnon handleInvalid
```

Allow only user's from a certain group to access endpoint"

```fsharp
let secureResourceHandler : HttpHandler =
    let handleAuthInRole : HttpHandler =
        Response.ofPlainText "hello admin"

    let handleInvalid : HttpHandler =
        Response.withStatusCode 403
        >> Response.ofPlainText "Forbidden"

    let rolesAllowed = [ "Admin" ]

    Request.ifAuthenticatedInRole rolesAllowed handleAuthInRole handleInvalid
```

Allow only user's with a certain scope to access endpoint"

```fsharp
let secureResourceHandler : HttpHandler =
    let handleAuthHasScope : HttpHandler =
        Response.ofPlainText "user1, user2, user3"

    let handleInvalid : HttpHandler =
        Response.withStatusCode 403
        >> Response.ofPlainText "Forbidden"

    let issuer = "https://oauth2issuer.com"
    let scope = "read:users"

    Request.ifAuthenticatedWithScope issuer scope handleAuthHasScope handleInvalid
```

End user session (sign out):

```fsharp
let logOut : HttpHandler =
    let authScheme = "..."
    let redirectTo = "/login"

    Response.signOutAndRedirect authScheme redirectTo
```

## Security

Cross-site scripting attacks are extremely common since they are quite simple to carry out. Fortunately, protecting against them is as easy as performing them.

The [Microsoft.AspNetCore.Antiforgery][14] package provides the required utilities to easily protect yourself against such attacks.

Falco provides a few handlers via `Falco.Security.Xss`:

> To use the Xss helpers, ensure the service has been registered (`AddAntiforgery()`) with the `IServiceCollection` and activated (`UseAntiforgery()`) using the `IApplicationBuilder`.

```fsharp
open Falco.Markup
open Falco.Security

let formView token =
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

// A handler that demonstrates obtaining a
// CSRF token and applying it to a view
let csrfViewHandler : HttpHandler =
    formView
    |> Response.ofHtmlCsrf

// A handler that demonstrates validating
// the request's CSRF token
let mapFormSecureHandler : HttpHandler =
    let mapPerson (form : FormCollectionReader) =
        { FirstName = form.GetString "first_name" "John" // Get value or return default value
          LastName = form.GetString "first_name" "Doe" }

    let handleInvalid : HttpHandler =
        Response.withStatusCode 400
        >> Response.ofEmpty

    Request.mapFormSecure mapPerson Response.ofJson handleInvalid
```

### Crytography

Many sites have the requirement of a secure log in and sign up (i.e. registering and maintaining a user's database). Thus, generating strong hashes and random salts is important.

Falco helpers are accessed by importing `Falco.Auth.Crypto`.

```fsharp
open Falco.Security

// Generating salt,
// using System.Security.Cryptography.RandomNumberGenerator,
// create a random 16 byte salt and base 64 encode
let salt = Crypto.createSalt 16

// Generate random int for iterations
let iterations = Crypto.randomInt 10000 50000

// Pbkdf2 Key derivation using HMAC algorithm with SHA256 hashing function
let password = "5upe45ecure"
let hashedPassword = password |> Crypto.sha256 iterations 32 salt
```