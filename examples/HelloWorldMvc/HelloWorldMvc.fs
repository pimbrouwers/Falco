namespace HelloWorldMvc

open Falco
open Falco.Markup
open Falco.Routing
open Microsoft.AspNetCore.Builder

module Model =
    type NameGreeting =
        { Name : string }

    type Greeting =
        { Message : string }

module Route =
    let index = "/"
    let greetPlainText = "/greet/text/{name?}"
    let greetJson = "/greet/json/{name?}"
    let greetHtml = "/greet/html/{name?}"

module Url =
    let greetPlainText name = Route.greetPlainText.Replace("{name?}", name)
    let greetJson name = Route.greetJson.Replace("{name?}", name)
    let greetHtml name = Route.greetHtml.Replace("{name?}", name)

module View =
    open Model

    let layout content =
        Templates.html5 "en"
            [ Elem.link [ Attr.href "/style.css"; Attr.rel "stylesheet" ] ]
            content

    module GreetingView =
        let detail greeting =
            layout [
                Text.h1 $"Hello {greeting.Name} using HTML"
                Elem.hr []
                Text.p "Greet other ways:"
                Elem.nav [] [
                    Elem.a
                        [ Attr.href (Url.greetHtml greeting.Name) ]
                        [ Text.raw "Greet in HTML"]
                    Text.raw " | "
                    Elem.a
                        [ Attr.href (Url.greetPlainText greeting.Name) ]
                        [ Text.raw "Greet in plain text"]
                    Text.raw " | "
                    Elem.a
                        [ Attr.href (Url.greetJson greeting.Name) ]
                        [ Text.raw "Greet in JSON " ]
                ]
            ]

module Controller =
    open Model
    open View

    /// Error page(s)
    module ErrorController =
        let notFound : HttpHandler =
            Response.withStatusCode 404 >>
            Response.ofHtml (layout [ Text.h1 "Not Found" ])

        let serverException : HttpHandler =
            Response.withStatusCode 500 >>
            Response.ofHtml (layout [ Text.h1 "Server Error" ])

    module GreetingController =
        let index name =
            { Name = name }
            |> GreetingView.detail
            |> Response.ofHtml

        let plainTextDetail name =
            Response.ofPlainText $"Hello {name} using plain text"

        let jsonDetail name =
            let message = { Message = $"Hello {name} using JSON" }
            Response.ofJson message

        let endpoints =
            let mapRoute (r : RequestData) =
                r?name.AsStringNonEmpty("you")

            [ mapGet Route.index mapRoute index
              mapGet Route.greetPlainText mapRoute plainTextDetail
              mapGet Route.greetJson mapRoute jsonDetail
              mapGet Route.greetHtml mapRoute index ]

module App =
    open Controller

    let endpoints =
        GreetingController.endpoints

module Program =
    open Controller

    [<EntryPoint>]
    let main args =
        let wapp = WebApplication.Create(args)

        let isDevelopment = wapp.Environment.EnvironmentName = "Development"

        wapp.UseIf(isDevelopment, DeveloperExceptionPageExtensions.UseDeveloperExceptionPage)
            .UseIf(not(isDevelopment), FalcoExtensions.UseFalcoExceptionHandler ErrorController.serverException)
            .Use(StaticFileExtensions.UseStaticFiles)
            .UseRouting()
            .UseFalco(App.endpoints)
            .UseFalcoNotFound(ErrorController.notFound)
            .Run()

        0
