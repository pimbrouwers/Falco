namespace Falco

[<AutoOpen>]
module Response =
    open System.Text.Json
    open Microsoft.AspNetCore.Http

    let json (obj : 'a) : HttpHandler =
        fun (_ : HttpFunc) (ctx : HttpContext) ->   
        ctx.SetContentType "application/json; charset=utf-8"
        ctx.WriteString (JsonSerializer.Serialize(obj))

    let text (str : string) : HttpHandler =    
        fun (_ : HttpFunc) (ctx : HttpContext) ->
            ctx.SetContentType "text/plain; charset=utf-8"
            ctx.WriteString str