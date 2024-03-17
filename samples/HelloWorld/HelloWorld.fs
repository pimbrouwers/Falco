module HelloWorld.Program

open Falco
open Falco.Markup
open Falco.Routing
open Microsoft.AspNetCore.Builder // <-- this import adds many useful extensions

/// The application routes 
module Route = 
    let index = "/"
    let greetPlainText = "/greet/text/{name}"
    let greetJson = "/greet/json/{name}"
    let greetHtml = "/greet/html/{name}"

module GreetingView = 
    let detail name =
        Templates.html5 "en"
            [ Elem.link [ Attr.href "/style.css"; Attr.rel "stylesheet" ] ]
            [ Elem.h1 [] [ Text.raw $"Hello {name} from /html" ] ]

module GreetingController = 
    /// GET /
    let index : HttpHandler =
        Response.ofPlainText "Hello world"

    // a helper to get the name from the route
    let getNameFromRoute (route : RouteCollectionReader) =
        route.Get "name"

    /// GET /greet/{name}
    let plainTextDetail : HttpHandler = fun ctx -> // <-- explicit HttpContext input to access request data
        let name = Request.getRoute ctx |> getNameFromRoute
        let greeting = $"Hello {name}"
        Response.ofPlainText greeting ctx

    // a helper to project the name into an HttpHandler
    let mapRouteToName next =
        Request.mapRoute getNameFromRoute next

    /// GET /greet/json
    let jsonDetail : HttpHandler =        
        mapRouteToName (fun name -> // <-- Continuation-style HttpHandler
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
    Falco args
    |> Falco.Middleware.add StaticFileExtensions.UseStaticFiles // <-- useful extension from Microsoft.AspNetCore.Builder
    |> Falco.endpoints App.endpoints    
    |> Falco.run
    0 
