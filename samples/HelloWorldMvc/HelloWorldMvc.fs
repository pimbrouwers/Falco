module HelloWorld.Program

open System
open Falco
open Falco.Markup
open Falco.Routing
open Microsoft.AspNetCore.Builder // <-- this import adds many useful extensions

/// Application routes
module Route =
    let index = "/"
    let greetPlainText = "/greet/text/{name}"
    let greetJson = "/greet/json/{name}"
    let greetHtml = "/greet/html/{name}"

/// URL factories
module Url =
    let greatPlainText name = Route.greetPlainText.Replace("{name}", name)
    let greatJson name = Route.greetJson.Replace("{name}", name)

module GreetingView =
    /// HTML view for /greet/html
    let detail name =
        Templates.html5 "en"
            [ Elem.link [ Attr.href "/style.css"; Attr.rel "stylesheet" ] ]
            [
                Elem.h1 [] [ Text.raw $"Hello {name} from /html" ]
                Elem.hr []
                Elem.p [] [ Text.raw "Greet other ways:" ]
                Elem.nav [] [
                    Elem.a  [ Attr.href (Url.greatPlainText "Gru") ] [ Text.raw "Greet in text"]
                    Text.raw " | "
                    Elem.a [ Attr.href (Url.greatJson "Dru") ] [ Text.raw "Greet in JSON " ]
                ]
            ]

module GreetingController =
    /// GET /
    let index : HttpHandler =
        Response.ofPlainText "Hello world"

    /// A helper to get the name from the route
    let private getNameFromRoute (route : RequestData) =
        route.GetString "name"

    /// GET /greet/{name}
    let plainTextDetail : HttpHandler = fun ctx -> // <-- explicit HttpContext input to access request data
        let name = Request.getRoute ctx |> getNameFromRoute
        let greeting = $"Hello {name}"
        Response.ofPlainText greeting ctx

    /// A helper to project the name into an HttpHandler
    let private mapRouteToName next =
        Request.mapRoute getNameFromRoute next

    /// GET /greet/json
    let jsonDetail : HttpHandler = // <-- Continuation-style HttpHandler
        mapRouteToName (fun name ->
            let message = {| Message = $"Hello {name} from /json" |}
            Response.ofJson message)

    /// GET /greet/html
    let htmlDetail : HttpHandler =
        mapRouteToName (fun name ->
            name
            |> GreetingView.detail
            |> Response.ofHtml )

/// Our application definitions
module App =
    let endpoints = [
        get Route.index GreetingController.index
        get Route.greetPlainText GreetingController.plainTextDetail
        get Route.greetJson GreetingController.jsonDetail
        get Route.greetHtml GreetingController.htmlDetail
    ]

/// By defining an explicit entry point, we gain access to the command line
/// arguments which when passed into Falco are used as the creation arguments
/// for the internal WebApplicationBuilder.
[<EntryPoint>]
let main args =
    let isDevelopment = true // <-- should come environment
    
    args
    |> Falco.newApp 
    |> Falco.Middleware.addIf isDevelopment DeveloperExceptionPageExtensions.UseDeveloperExceptionPage // <-- pretty error output and stack tracers
    |> Falco.Middleware.add StaticFileExtensions.UseStaticFiles // <-- useful extension from Microsoft.AspNetCore.Builder
    |> Falco.endpoints App.endpoints
    |> Falco.run
    
    0
