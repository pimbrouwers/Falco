module HelloWorld.Program

open Falco
open Falco.Markup
open Falco.HostBuilder
open Falco.Routing
open Falco.Security

/// GET /
let handlePlainText : HttpHandler = fun ctx ->
    // read query values manually
    let route = Request.getQuery ctx
    let name = route.Get ("name", "world!") // retrieve name or, use default value
    let greeting = sprintf "Hello %s" name
    Response.ofPlainText greeting ctx

/// GET /greet/{name?}
let handleGreet : HttpHandler =
    // read route values, using continuation
    Request.mapRoute (fun r ->
        let name = r.Get "name" // retrieve name, "" if null
        sprintf "Hello there %s" name)
        Response.ofPlainText

/// Define a CSRF protected HTML5 form, with error output
let form errors antiforgeryToken =
    Templates.html5 "en" [] [
        Elem.div [] [
            for e in errors do
                Elem.div [ Attr.style "color: red" ] [ Text.rawf "&bull; %s" e ]
                Elem.br [] ]

        Elem.form [ Attr.method "post" ] [
            Elem.label [] [ Text.raw "Please enter your name " ]
            Elem.input [ Attr.name "name" ]
            Xss.antiforgeryInput antiforgeryToken
            Elem.input [ Attr.type' "submit" ]] ]

/// GET /form
let handleForm : HttpHandler =
    // Render HTML form, automatically injecting antiforgery token
    Response.ofHtmlCsrf (form [])

/// Define a 400 Bad Request handler
let badRequest =
    Response.withStatusCode 400 // modify response in-place
    >> Response.ofPlainText "Bad Request"

/// POST /form =
let handleFormPost : HttpHandler =
    // read form values as Option, using continuation
    Request.mapFormSecure
        (fun f -> f.TryGetStringNonEmpty "name") // retrieve name, if not null or whitespace
        (fun name ->
            match name with
            | None -> Response.ofHtmlCsrf (form [ "Invalid name" ])
            | Some name -> Response.ofJson {| Name = name |})
        badRequest // handle invalid token, in this case return 400 Bad Request

[<EntryPoint>]
let main args =
    webHost args {
        add_antiforgery // add built-in CSRF protection

        endpoints [
            get "/" handlePlainText
            get "/greet/{name?}" handleGreet
            all "/form" [
                GET, handleForm
                POST, handleFormPost ]
        ]
    }

    0 // Exit code