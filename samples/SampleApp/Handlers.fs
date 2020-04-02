module SampleApp.Handlers

open Falco
open Microsoft.AspNetCore.Http
open SampleApp.UI

let notFoundHandler : HttpHandler =
    setStatusCode 404 >=> textOut "Not found"

let helloHandler : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->        
        let name = ctx.TryGetRouteValue "name" |> Option.defaultValue "someone"
        textOut (sprintf "hi %s" name) next ctx

let myHtmlOutHandler : HttpHandler =
    htmlOut homeView

let myJsonOutHandler : HttpHandler =
    jsonOut { First = "Pim"; Last = "Brouwers" }
   
let newUserViewHandler : HttpHandler =
    htmlOut newUserView

let newUserHandler : HttpHandler = 
    tryBindForm<Person>
        jsonOut
        jsonOut

let searchViewHandler : HttpHandler =
    htmlOut searchView

let searchResultsHandler : HttpHandler =
    tryBindQuery<SearchQuery>
        jsonOut
        jsonOut
