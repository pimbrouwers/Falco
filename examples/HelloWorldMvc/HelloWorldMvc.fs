namespace HelloWorldMvc

open Falco
open Falco.Markup
open Microsoft.AspNetCore.Builder

module Model =
    type NameGreeting =
        { Name : string }

    type Greeting =
        { Message : string }

/// Routes templates
module Route =
    let index = "/"
    let greetPlainText = "/greet/text/{name}"
    let greetJson = "/greet/json/{name}"
    let greetHtml = "/greet/html/{name}"

/// URL factories
module Url =
    let greetPlainText name = Route.greetPlainText.Replace("{name}", name)
    let greetJson name = Route.greetJson.Replace("{name}", name)
    let greetHtml name = Route.greetHtml.Replace("{name}", name)

module View =
    open Model

    let layout content =
        Templates.html5 "en"
            [ Elem.link [ Attr.href "/style.css"; Attr.rel "stylesheet" ] ]
            content

    module GreetingView =
        /// HTML view for /greet/html
        let detail greeting =
            layout [
                Text.h1 $"Hello {greeting.Name} from /html"
                Elem.hr []
                Text.p "Greet other ways:"
                Elem.nav [] [
                    Elem.a  [ Attr.href (Url.greetPlainText greeting.Name) ] [ Text.raw "Greet in text"]
                    Text.raw " | "
                    Elem.a [ Attr.href (Url.greetJson greeting.Name) ] [ Text.raw "Greet in JSON " ]
                ]
            ]

/// Static error page(s)
module ErrorPage =
    let notFound : HttpHandler =
        Response.withStatusCode 404 >>
        Response.ofHtml (View.layout [ Text.h1 "Not Found" ])

    let serverException : HttpHandler =
        Response.withStatusCode 500 >>
        Response.ofHtml (View.layout [ Text.h1 "Server Error" ])

module Controller =
    open Model
    open View

    module GreetingController =
        /// GET /
        let index : HttpHandler =
            Response.ofPlainText "Hello world"

        /// A helper to project the name into an HttpHandler
        let private mapRouteToNameGreeting next =
            Request.mapRoute 
                (fun route -> { Name = route.GetString "name" }) // <-- almost feels like a dynamic 
                next

        /// GET /greet/{name}
        let plainTextDetail : HttpHandler =
            mapRouteToNameGreeting (fun greeting ->
            let message = $"Hello {greeting.Name}"
            Response.ofPlainText message)

        /// GET /greet/json
        let jsonDetail : HttpHandler = // <-- Continuation-style HttpHandler
            mapRouteToNameGreeting (fun greeting ->
                let message = { Message = $"Hello {greeting.Name} from /json" }
                Response.ofJson message)

        /// GET /greet/html
        let htmlDetail : HttpHandler =
            mapRouteToNameGreeting (fun greeting ->
                greeting
                |> GreetingView.detail
                |> Response.ofHtml)

module Program =
    open Controller

    /// By defining an explicit entry point, we gain access to the command line
    /// arguments which when passed into Falco are used as the creation arguments
    /// for the internal WebApplicationBuilder.
    [<EntryPoint>]
    let main args =
        let wapp = WebApplication.Create(args)

        let isDevelopment = wapp.Environment.EnvironmentName = "Development"

        let endpoints = 
            [ get Route.index GreetingController.index
              get Route.greetPlainText GreetingController.plainTextDetail
              get Route.greetJson GreetingController.jsonDetail
              get Route.greetHtml GreetingController.htmlDetail ]

        wapp.UseIf(isDevelopment, DeveloperExceptionPageExtensions.UseDeveloperExceptionPage)
            .UseIf(not(isDevelopment), FalcoExtensions.UseFalcoExceptionHandler ErrorPage.serverException)
            .Use(StaticFileExtensions.UseStaticFiles)
            .UseFalco(endpoints)
            .FalcoNotFound(ErrorPage.notFound)
            .Run()

        0