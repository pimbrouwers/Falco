module Blog.Post

module Model =
    open System
    
    type UnprocessedPost =
        {
            Slug  : string            
            Date  : DateTime
            Body  : string
        }
        
    type PostModel =
        {
            Slug  : string
            Title : string
            Date  : DateTime
            Body  : string
        }

    module PostModel =
        open Blog.Markdown

        let parseBlogPost (unprocessedPost : UnprocessedPost)=                                 
            let markdownDoc = renderMarkdown unprocessedPost.Body            
            {
                Slug = unprocessedPost.Slug
                Title = markdownDoc.Title
                Date = unprocessedPost.Date
                Body = markdownDoc.Body
            }

[<RequireQualifiedAccess>]
module IO =            
    open System
    open System.Globalization
    open System.IO
    open Model

    let all postsDirectory =  
        let readPostContet (postPath : string) = 
            let relativePath = Path.GetFileNameWithoutExtension postPath

            let extractDateFromPath =
                let path = relativePath.Substring(0, 10) // keep only yyyy-MM-dd
                DateTime.ParseExact(path, "yyyy-MM-dd", CultureInfo.InvariantCulture)

            let extractSlugFromPath =
                relativePath.Substring(11) //strip yyyy-MM-dd
                
            let date = extractDateFromPath
            let slug = extractSlugFromPath            
            let markdown = File.ReadAllText(postPath)

            {
                Slug = slug
                Date = date
                Body = markdown
            }

        postsDirectory
        |> Directory.GetFiles        
        |> Array.map readPostContet
        |> Array.map PostModel.parseBlogPost
        |> Array.toList

[<RequireQualifiedAccess>]
module View =
    open Falco.ViewEngine
    open Blog.UI
    open Model 

    let index (blogPosts : PostModel list) =    
        blogPosts         
        |> List.map (fun p -> 
            div [] [ 
                    span [] [ raw (p.Date.ToShortDateString()) ]
                    span [] [ raw "&nbsp;&mdash;&nbsp;"]
                    a [ _href p.Slug ] [ raw p.Title ]
                ])        
        |> fun p -> 
            master "Falco Blog" ([ 
                    h1 [] [ raw "Falco Blog "]
                    h2 [] [ raw "Posts"]                
                ] @ p)
        
    let details (blogPost : PostModel) =
        master blogPost.Title [ raw blogPost.Body ]

module Middleware =
    open Falco 
    open Falco.StringUtils
    open Model

    let withPost (posts : PostModel list) (handler : PostModel -> HttpHandler) : HttpHandler =    
        fun next ctx ->
            let slug = ctx.TryGetRouteValue "slug" |> Option.defaultValue ""
            let handleNotFound = setStatusCode 404 >=> textOut "Not found"
            
            let handlerResult =
                posts
                |> List.tryFind (fun p -> strEquals p.Slug slug)
                |> function
                   | None      -> handleNotFound
                   | Some post -> handler post
                   
            handlerResult next ctx 
    
[<RequireQualifiedAccess>]
module Controller =
    open Falco 
    open Model

    let index (posts : PostModel list) : HttpHandler =
        posts
        |> List.sortByDescending (fun p -> p.Date)
        |> View.index 
        |> htmlOut

    let details (post : PostModel) : HttpHandler = 
        post 
        |> View.details 
        |> htmlOut

    

