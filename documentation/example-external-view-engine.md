# Example - External View Engine

Falco comes packaged with a [built-in view engine](markup.md). But if you'd prefer to write your own templates, or use an external view engine, that is entirely possible as well.

In this example we'll integrate with the amazing template engine [scriban](https://github.com/scriban/scriban) by [xoofx](https://github.com/xoofx).

The code for this example can be found [here](https://github.com/pimbrouwers/Falco/tree/master/examples/ExternalViewEngine).

## Creating the Application Manually

```shell
> dotnet new falco -o ExternalViewEngineApp
> cd ExternalViewEngineApp
> dotnet add package Scriban
```

## Implementing a Custom View Engine

```fsharp
```
