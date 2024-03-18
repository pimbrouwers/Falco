open Falco

Falco ()
|> Falco.get "/" (Response.ofPlainText "Hello World")
|> Falco.run