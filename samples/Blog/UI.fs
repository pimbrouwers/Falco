module Blog.UI

open Falco.ViewEngine

let layout pageTitle content = 
    html [ _lang "en"; _class "border-box mw60-rem center" ] [
            head [] [
                meta  [ _charset "UTF-8" ]
                meta  [ _httpEquiv "X-UA-Compatible"; _content "IE=edge,chrome=1" ]
                meta  [ _name "viewport"; _content "width=device-width,initial-scale=1" ]
                title [] [ raw pageTitle ]           
                link  [ _rel "stylesheet"; _href "style.css" ]                             
            ]
            body [ _class "pa4 ff-georgia" ] [                     
                    main [] content
                ]
        ] 
