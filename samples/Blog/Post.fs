module Blog.Post

module Model =
    open System
        
    type PostModel =
        {
            Slug  : string
            Title : string
            Date  : DateTime
            Body  : string
        }

     
module View =
    open Falco.Markup
    open Blog.UI
    open Model 
    
    let details (blogPost : PostModel) =
        [ 
            Elem.a [ Attr.href "/"; ] [ Text.raw "<< Back home" ]
            Text.raw blogPost.Body 
        ]
        |> layout blogPost.Title 

    let index (blogPosts : PostModel list) =    
        let postElement p =
            Elem.div [] [ 
                    Elem.span [] [ Text.raw (p.Date.ToShortDateString()) ]
                    Elem.span [] [ Text.raw " &mdash; "]
                    Elem.a [ Attr.href p.Slug ] [ Text.raw p.Title ]
                ]

        let postElements = 
            blogPosts         
            |> List.map postElement                    

        [ 
            Elem.h1 [] [ Text.raw "Falco Blog "]
            Elem.h2 [] [ Text.raw "Posts"]                
        ] @ postElements
        |> layout "Falco Blog"
        
    let notFound slug =
        let msg = 
            match slug with
            | None -> "Invalid post URL"
            | Some slug -> (sprintf "Post with URL %s was not found" slug)

        [
            Elem.h1 [] [ Text.raw "Not Found"]
            Elem.p  [] [ Text.raw msg ]
        ]
        |> layout "Not Found"
    
module Controller =
    open Falco 
    open Falco.StringUtils
    open Model
    
    let details (posts : PostModel list) : HttpHandler =   
        fun ctx ->
            let findPost slug = 
                posts 
                |> List.tryFind (fun post -> strEquals post.Slug slug)

            let handleNotFound slug =
                slug
                |> View.notFound 
                |> Response.ofHtml

            let handlePost slug =
                match findPost slug with
                | None _    -> handleNotFound (Some slug)
                | Some post ->
                    post
                    |> View.details
                    |> Response.ofHtml

            let respondWith =
                Request.bindRoute 
                    (fun route -> 
                        match route.TryGet "slug" with
                        | None      -> Error None
                        | Some slug -> Ok slug)
                    handlePost
                    handleNotFound                       

            respondWith ctx
                        
            
    let index (posts : PostModel list) : HttpHandler =       
        posts         
        |> View.index 
        |> Response.ofHtml

    let json (posts : PostModel list) : HttpHandler =
        posts         
        |> Response.ofJson
