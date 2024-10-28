# Cross-site Scripting (XSS) Attacks

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
                Xsrf.antiforgeryInput token
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

[Next: Authentication](authentication.md)
