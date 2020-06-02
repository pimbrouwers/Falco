module HelloWorldApp 

open Falco

webApp {        
    get "/"  (textOut "hello")
    notFound (setStatusCode 404 >=> textOut "Not found")
}