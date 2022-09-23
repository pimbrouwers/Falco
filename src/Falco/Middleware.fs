module Falco.Middleware

open Microsoft.AspNetCore.Http

let httpPipe
    (prepare : HttpContextAccessor<'a>)
    (next : 'a -> HttpHandler) : HttpHandler = fun ctx ->
    next (prepare ctx) ctx

let httpPipeTask
    (prepare : AsyncHttpContextAccessor<'a>)
    (next : 'a -> HttpHandler) : HttpHandler = fun ctx ->
    task {
        let! x = prepare ctx
        return next x ctx
    }

let withService<'a> next : HttpHandler =
    httpPipe Extensions.getService<'a> next

let withLogger name next : HttpHandler =
    httpPipe (Extensions.getLogger "test") next
