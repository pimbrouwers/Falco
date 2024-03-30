# Getting Started

## Using `dotnet new`

The easiest way to get started with Falco is by installing the `Falco.Template` package, which adds a new template to your `dotnet new` command line tool:

```shell
> dotnet new -i "Falco.Template::*"
```

Afterwards you can create a new Falco application by running:

```shell
> dotnet new falco -o HelloWorldApp
> cd HelloWorldApp
> dotnet run
```

## Manually installing

Create a new F# web project:

```shell
> dotnet new web -lang F# -o HelloWorldApp
> cd HelloWorldApp
```

Install the nuget package:

```shell
> dotnet add package Falco
```

Remove any `*.fs` files created automatically, create a new file named `Program.fs` and set the contents to the following:

```fsharp
open Falco
open Microsoft.AspNetCore.Builder

let wapp = WebApplication.Create()

wapp.UseFalco()
    .FalcoGet("/", Response.ofPlainText "hello world")
    .Run()
```

Run the application:

```shell
> dotnet run
```

And there you have it, an industrial-strength [Hello World](https://github.com/pimbrouwers/Falco/tree/master/examples/HelloWorld) web app. Pretty sweet!

## Sample Applications

Code is worth a thousand words. For the most up-to-date usage, the [examples](https://github.com/pimbrouwers/Falco/tree/master/examples/) directory contains a few sample applications.

[Next: Routing](routing.md)