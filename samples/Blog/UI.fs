module Blog.UI

open Falco.ViewEngine
open Blog.Model


let master pageTitle content = 
    html [ _lang "en" ] [
            head [] [
                meta  [ _charset "UTF-8" ]
                meta  [ _httpEquiv "X-UA-Compatible"; _content "IE=edge,chrome=1" ]
                meta  [ _name "viewport"; _content "width=device-width,initial-scale=1" ]
                title [] [ raw pageTitle ]                                        
                link  [ _href "/style.css"; _rel "stylesheet"]
            ]
            body [] [                     
                    main [] content
                ]
        ] 

let indexView (blogPosts : BlogPost array) =    
    blogPosts 
    |> Array.sortByDescending (fun p -> p.Date)
    |> Array.map (fun p -> 
        div [] [ 
                span [] [ raw (p.Date.ToShortDateString()) ]
                span [] [ raw "&nbsp;&mdash;&nbsp;"]
                a [ _href p.Slug ] [ raw p.Title ]
            ])
    |> Array.toList
    |> fun p -> 
        master "Falco Blog" ([ 
                h1 [] [ raw "Falco Blog "]
                h2 [] [ raw "Posts"]                
            ] @ p)
    
let blogPostView (blogPost : BlogPost) =
    master blogPost.Title [ raw blogPost.Body ]

