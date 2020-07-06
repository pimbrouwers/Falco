module Blog.UI

open Falco.Markup

let layout pageTitle content = 
    Elem.html [ Attr.lang "en"; Attr.class' "border-box mw60-rem center" ] [
            Elem.head [] [
                Elem.meta  [ Attr.charset "UTF-8" ]
                Elem.meta  [ Attr.httpEquiv "X-UA-Compatible"; Attr.content "IE=edge,chrome=1" ]
                Elem.meta  [ Attr.name "viewport"; Attr.content "width=device-width,initial-scale=1" ]
                Elem.title [] [ raw pageTitle ]           
                Elem.link  [ Attr.rel "stylesheet"; Attr.href "style.css" ]                             
            ]
            Elem.body [ Attr.class' "pa4 ff-georgia" ] [                     
                    Elem.main [] content
                ]
        ] 
