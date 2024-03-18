# Falco Hello World 

The goal of this program is to demonstrate the absolute bare bones hello world
application, using as little code as possible.

```fsharp
Falco ()
|> Falco.get "/" (Response.ofPlainText "hello world")
|> Falco.run
```