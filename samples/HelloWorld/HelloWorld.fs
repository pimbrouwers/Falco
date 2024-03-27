open Falco

Falco.newApp ()
|> Falco.get "/" (Response.ofPlainText "Hello World")
|> Falco.run