namespace Falco

[<AutoOpen>]
module Routing =    
    open Microsoft.AspNetCore.Builder
    open Microsoft.AspNetCore.Routing
    open Microsoft.Extensions.DependencyInjection
    
    let rec mapHttpEndpoints (r : IEndpointRouteBuilder) (endPoints : HttpEndpoint list) =
        if endPoints.Length > 0 then 
            let e = endPoints.Head
            let rd = createRequestDelete e.Handler

            match e.Verb with
            | GET    -> r.MapGet(e.Pattern, rd)
            | POST   -> r.MapPost(e.Pattern, rd)
            | PUT    -> r.MapPut(e.Pattern, rd)
            | DELETE -> r.MapDelete(e.Pattern, rd)
            | _      -> r.Map(e.Pattern, rd)
            |> ignore

            mapHttpEndpoints r endPoints.Tail
        else 
            r
    
    type IApplicationBuilder with
        member this.UseHttpEndPoints(endPoints : HttpEndpoint list) =
            let useEndPoints (r : IEndpointRouteBuilder) =
                mapHttpEndpoints r endPoints |> ignore

            this.UseRouting()
                .UseEndpoints(fun r -> useEndPoints r)
    
    let createRoute method pattern handler = 
        { 
            Pattern = pattern
            Verb  = method
            Handler = handler
        }

    let route pattern handler  = createRoute ALL pattern handler
    let get pattern handler    = createRoute GET pattern handler
    let post pattern handler   = createRoute POST pattern handler
    let put pattern handler    = createRoute PUT pattern handler
    let delete pattern handler = createRoute DELETE pattern handler
    