module HelloWorld.Program

open Falco
open Falco.Markup
open Microsoft.AspNetCore.Builder // <-- this import adds many useful extensions

/// Routes templates
module Route =
    let index = "/"
    let greetPlainText = "/greet/text/{name}"
    let greetJson = "/greet/json/{name}"
    let greetHtml = "/greet/html/{name}"

/// URL factories
module Url =
    let greatPlainText name = Route.greetPlainText.Replace("{name}", name)
    let greatJson name = Route.greetJson.Replace("{name}", name)

/// View components
module View =
    let layout content =
        Templates.html5 "en"
            [ Elem.link [ Attr.href "/style.css"; Attr.rel "stylesheet" ] ]
            content

module GreetingView =
    /// HTML view for /greet/html
    let detail name =
        View.layout [
            Text.h1 $"Hello {name} from /html"
            Elem.hr []
            Text.p "Greet other ways:"
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

/// Static error pages
module ErrorPage =
    let notFound : HttpHandler =
        Response.withStatusCode 404 >>
        Response.ofHtml (View.layout [ Text.h1 "Not Found" ])

/// By defining an explicit entry point, we gain access to the command line
/// arguments which when passed into Falco are used as the creation arguments
/// for the internal WebApplicationBuilder.
[<EntryPoint>]
let main args =
    let wapp = WebApplication.Create(args)

    let isDevelopment = wapp.Environment.EnvironmentName = "Development"

    wapp.UseIf(isDevelopment, DeveloperExceptionPageExtensions.UseDeveloperExceptionPage)
        .Use(StaticFileExtensions.UseStaticFiles)
        .UseFalco()
        .FalcoGet(Route.index, GreetingController.index)
        .FalcoGet(Route.greetPlainText, GreetingController.plainTextDetail)
        .FalcoGet(Route.greetJson, GreetingController.jsonDetail)
        .FalcoGet(Route.greetHtml, GreetingController.htmlDetail)
        .FalcoNotFound(ErrorPage.notFound)
        .Run()

    0