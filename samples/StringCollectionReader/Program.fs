module HelloWorld.Program

open Falco
open Falco.Markup
open Falco.Routing
open Falco.Security
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

module ErrorController =     
    /// 400 Bad Request handler
    let badRequest =
        Response.withStatusCode 400 // modify response in-place
        >> Response.ofPlainText "Bad Request"

module GreetingView =
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

module GreetingController  = 
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

    /// GET /form
    let handleForm : HttpHandler =
        // Render HTML form, automatically injecting antiforgery token
        Response.ofHtmlCsrf (GreetingView.form [])

    /// POST /form =
    let handleFormPost : HttpHandler =
        // read form values as Option, using continuation
        Request.mapFormSecure
            (fun f -> f.TryGetStringNonEmpty "name") // retrieve name, if not null or whitespace
            (fun name ->
                match name with
                | None -> Response.ofHtmlCsrf (GreetingView.form [ "Invalid name" ])
                | Some name -> Response.ofJson {| Name = name |})
            ErrorController.badRequest // handle invalid token, in this case return 400 Bad Request

type App() = 
    member _.Endpoints = 
        seq {
            get "/" GreetingController.handlePlainText
            get "/greet/{name?}" GreetingController.handleGreet
            all "/form" [
                GET, GreetingController.handleForm
                POST, GreetingController.handleFormPost ]
        }

    member _.NotFound = 
        Response.withStatusCode 404 
        >> Response.ofPlainText "Not Found"

[<EntryPoint>]
let main args =
    let bldr = WebApplication.CreateBuilder(args)
    
    bldr.Services
        .AddAntiforgery() 
        |> ignore    

    let app = App()

    let wapp = bldr.Build()
    
    wapp.UseFalco(app.Endpoints)    
        .Run(app.NotFound) 
        |> ignore
    
    wapp.Run()
    0 // Exit code
    