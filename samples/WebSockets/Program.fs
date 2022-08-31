// Example from:
// https://github.com/dotnet/AspNetCore.Docs/tree/main/aspnetcore/fundamentals/websockets/samples/6.x/WebSocketsSample

module HelloWorld.Program

open System
open System.Net.WebSockets
open System.Threading
open System.Threading.Tasks
open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.AspNetCore.Builder


let echo (webSocket : WebSocket) : Task =
    let buffer = Array.zeroCreate (1024 * 4)

    let rec loop (result : WebSocketReceiveResult) =
        task {
            if result.CloseStatus.HasValue then
                return result
            else
                do! webSocket.SendAsync(
                    ArraySegment<byte>(buffer, 0, result.Count),
                    result.MessageType,
                    result.EndOfMessage,
                    CancellationToken.None)

                let! nextResult =
                    webSocket.ReceiveAsync(
                        ArraySegment<byte>(buffer),
                        CancellationToken.None)

                return! loop nextResult
        }

    task {
        let! initialResult =
            webSocket.ReceiveAsync(
                ArraySegment<byte>(buffer),
                CancellationToken.None)

        let! finalResult = loop initialResult

        do! webSocket.CloseAsync(
            finalResult.CloseStatus.Value,
            finalResult.CloseStatusDescription,
            CancellationToken.None)
    }

let webSocketHandler : HttpHandler = fun ctx ->
    task {
        if ctx.WebSockets.IsWebSocketRequest then
            use! webSocket = ctx.WebSockets.AcceptWebSocketAsync()
            do! echo webSocket
        else
            return! (Response.withStatusCode 400 >> Response.ofEmpty) ctx
    }

let webSocketMiddleware (app : IApplicationBuilder) =
    let o = WebSocketOptions()
    o.KeepAliveInterval <- TimeSpan.FromMinutes(2)
    app.UseWebSockets(o)

webHost [||] {
    use_middleware webSocketMiddleware
    use_default_files
    use_static_files

    endpoints [
        get "/ws" webSocketHandler
    ]
}
