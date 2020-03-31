namespace Falco

[<AutoOpen>]
module Routing =    
    open Microsoft.AspNetCore.Builder
    open Microsoft.AspNetCore.Routing
    
    let mapHttpEndpoints (r : IEndpointRouteBuilder) (endPoints : HttpEndpoint list) =
        for e in endPoints do            
            let rd = createRequestDelete e.Handler

            match e.Verb with
            | GET    -> r.MapGet(e.Pattern, rd)
            | POST   -> r.MapPost(e.Pattern, rd)
            | PUT    -> r.MapPut(e.Pattern, rd)
            | DELETE -> r.MapDelete(e.Pattern, rd)
            | _      -> r.Map(e.Pattern, rd)
            |> ignore

        r
    
    type IApplicationBuilder with
        member this.UseHttpEndPoints(endPoints : HttpEndpoint list) =
            let useEndPoints (r : IEndpointRouteBuilder) =
                mapHttpEndpoints r endPoints |> ignore

            this.UseRouting()
                .UseEndpoints(fun r -> useEndPoints r)
    
    let route method pattern handler = 
        { 
            Pattern = pattern
            Verb  = method
            Handler = handler
        }

    let any pattern handler    = route ANY pattern handler
    let get pattern handler    = route GET pattern handler
    let post pattern handler   = route POST pattern handler
    let put pattern handler    = route PUT pattern handler
    let delete pattern handler = route DELETE pattern handler
    