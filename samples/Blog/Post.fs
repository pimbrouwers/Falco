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

[<RequireQualifiedAccess>]
module Data =            
    open System
    open System.Globalization
    open System.IO
    open Model

    type UnprocessedPost =
        {
            Slug : string            
            Date : DateTime
            Body : string
        }

    let loadAll postsDirectory =  
        let readPost (postPath : string) = 
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

        let processPost (unprocessedPost : UnprocessedPost) =
            let markdownDoc = Markdown.renderMarkdown unprocessedPost.Body            
            {
                Slug = unprocessedPost.Slug
                Title = markdownDoc.Title
                Date = unprocessedPost.Date
                Body = markdownDoc.Body
            }

        postsDirectory
        |> Directory.GetFiles        
        |> Array.map (readPost >> processPost)
     
[<RequireQualifiedAccess>]
module View =
    open Falco.ViewEngine
    open Blog.UI
    open Model 
        
    let details (blogPost : PostModel) =
        [ 
            a   [ _href "/"; ] [ raw "<< Back home" ]
            raw blogPost.Body 
        ]
        |> layout blogPost.Title 

    let index (blogPosts : PostModel[]) =    
        let postElement p =
            div [] [ 
                    span [] [ raw (p.Date.ToShortDateString()) ]
                    span [] [ raw " &mdash; "]
                    a [ _href p.Slug ] [ raw p.Title ]
                ]

        let postElements = 
            blogPosts         
            |> Array.map postElement        
            |> List.ofArray

        [ 
            h1 [] [ raw "Falco Blog "]
            h2 [] [ raw "Posts"]                
        ] @ postElements
        |> layout "Falco Blog"

    let notFound slug =
        let msg = 
            match slug with
            | None -> "Invalid post URL"
            | Some slug -> (sprintf "Post with URL %s was not found" slug)

        [
            h1 [] [ raw "Not Found"]
            p  [] [ raw msg ]
        ]
        |> layout "Not Found"
    
module Middleware =
    open Falco 
    open Falco.StringUtils
    open Model

    let withSortedPosts         
        (handlePosts : PostModel[] -> HttpHandler)
        (posts : PostModel[]) : HttpHandler =
         posts
        |> Array.sortBy (fun p -> p.Date)
        |> handlePosts

    let withPostFromSlug      
        (handleNotFound : string option -> HttpHandler)
        (handlePost : PostModel -> HttpHandler) 
        (posts : PostModel[]) : HttpHandler =    
        fun next ctx ->                        
            let getSlugFromQuery () = 
                ctx.TryGetRouteValue "slug"

            let findPost slug = 
                posts |> Array.tryFind (fun p -> strEquals p.Slug slug)

            match getSlugFromQuery () with
            | None      -> handleNotFound None
            | Some slug ->
                match findPost slug with
                | None      -> handleNotFound (Some slug)
                | Some post -> handlePost post
            |> fun handler -> handler next ctx

[<RequireQualifiedAccess>]
module Controller =
    open Falco 
    open Middleware
    open Model

    let notFound (slug : string option) : HttpHandler =
        slug
        |> View.notFound
        |> htmlOut
        
    let details (posts : PostModel[]) : HttpHandler =         
        let handlePost post =
            post 
            |> View.details 
            |> htmlOut

        posts
        |> withPostFromSlug notFound handlePost
        
    
    let index (posts : PostModel[]) : HttpHandler =       
        let handlePosts posts =           
            posts 
            |> View.index 
            |> htmlOut

        posts
        |> withSortedPosts handlePosts