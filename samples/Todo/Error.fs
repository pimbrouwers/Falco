module Todo.Error

open Falco
open Falco.Markup 

let invalidCsrfToken : HttpHandler = 
    Response.withStatusCode 400 >> Response.ofEmpty

let notFound : HttpHandler =
    let doc = UI.Layouts.master "Not Found" [ 
            Elem.h1 [] [ Text.raw "Not found" ] 
            Elem.div [] [ Text.raw "The page you've request could not be found." ]
        ]
    Response.withStatusCode 404 >> Response.ofHtml doc
