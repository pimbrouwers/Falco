# Falco Hello World app 4 ways

The goal of this program is to demonstrate some of the recommended patterns
using the simplest cases as examples. It is completely overblown on purpose.

You could write the most basic hello world as:

```fsharp
Falco ()
|> Falco.get "/" (Response.ofPlainText "hello world")
|> Falco.run
```