module Blog.UI

open Falco.ViewEngine

let master pageTitle content = 
    html [ _lang "en" ] [
            head [] [
                meta  [ _charset "UTF-8" ]
                meta  [ _httpEquiv "X-UA-Compatible"; _content "IE=edge,chrome=1" ]
                meta  [ _name "viewport"; _content "width=device-width,initial-scale=1" ]
                title [] [ raw pageTitle ]                                        
            ]
            body [] [                     
                    main [] content
                ]
        ] 
