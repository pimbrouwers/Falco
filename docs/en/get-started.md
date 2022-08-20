# Getting Started

## Using `dotnet new`

The easiest way to get started with Falco is by installing the `Falco.Template` package, which adds a new template to your `dotnet new` command line tool:

```cmd
dotnet new -i "Falco.Template::*"
```

Afterwards you can create a new Falco application by running:

```cmd
dotnet new falco -o HelloWorldApp
```

## Manually installing

Create a new F# web project:

```cmd
dotnet new web -lang F# -o HelloWorldApp
```

Install the nuget package:

```cmd
dotnet add package Falco
```

Remove the `Startup.fs` file and save the following in `Program.fs` (if following the manual install path):

```fsharp
module HelloWorld.Program

open Falco
open Falco.Routing
open Falco.HostBuilder

[<EntryPoint>]
let main args =
    webHost args {
        endpoints [
            get "/" (Response.ofPlainText "Hello World")
        ]
    }
    0
```

Run the application:

```cmd
dotnet run
```

There you have it, an industrial-strength [Hello World](https://github.com/pimbrouwers/Falco/tree/master/samples/) web app, achieved using only base ASP.NET Core libraries. Pretty sweet!

# Sample Applications

Code is worth a thousand words. For the most up-to-date usage, the [/samples][(https://github.com/pimbrouwers/Falco/tree/master/samples/) directory contains a few sample applications.
