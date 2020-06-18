module SampleApp.Handlers

open Falco
open Microsoft.AspNetCore.Http
open SampleApp.Model
open SampleApp.UI

let exceptionThrowingHandler : HttpHandler =
    fun _ _ ->        
        failwith "Fake Exception"

let helloHandler : HttpHandler =
    fun next ctx ->        
        let name = ctx.TryGetRouteValue "name" |> Option.defaultValue "someone"
        textOut (sprintf "hi %s" name) next ctx

let myHtmlOutHandler : HttpHandler =
    htmlOut homeView

let myJsonInHandler : HttpHandler = 
    bindJson<Person> jsonOut

let myJsonOutHandler : HttpHandler =
    jsonOut { First = "Pim"; Last = "Brouwers" }
   
let newUserViewHandler : HttpHandler =
    htmlOut newUserView

let newUserHandler : HttpHandler = 
    tryBindForm Person.FromReader jsonOut jsonOut

let searchViewHandler : HttpHandler =
    htmlOut searchView

let searchResultsHandler : HttpHandler =
    tryBindQuery SearchQuery.FromReader jsonOut jsonOut

