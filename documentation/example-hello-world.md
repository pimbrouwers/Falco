# Example - Hello World 

The goal of this program is to demonstrate the absolute bare bones hello world application, so that we can focus on the key elements when initiating a new web application.

## Code Overview 

```fsharp
open Falco
open Microsoft.AspNetCore.Builder // <-- this import adds many useful extensions

let wapp = WebApplication.Create()

wapp.UseFalco() // <-- activate Falco endpoint source
    .FalcoGet("/", Response.ofPlainText "hello world") // <-- associate GET / to HttpHandler
    .Run()
```

First, we open the required namespaces. `Falco` bring into scope the ability to activate the library and some other extension methods to make the fluent API more user-friendly. 

`Microsoft.AspNetCore.Builder` enables us to create web applications in a number of ways, we're using `WebApplication.Create()` above. It also adds many other useful extension methods, that you'll see later.

After creating the web application, we:

- Activate Falco using `wapp.UseFalco()`. This enables us to create endpoints.
- Register `GET /` endpoint to a handler which responds with "hello world".
- Run the app.

[Next: Example - Hello World MVC](sample-hello-world-mvc.md)