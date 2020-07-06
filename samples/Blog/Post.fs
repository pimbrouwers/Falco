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
        let (PostsDirectory postsDirectory) = postsDirectory

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
    open Falco.Markup
    open Blog.UI
    open Model 
        
    let details (blogPost : PostModel) =
        [ 
            Elem.a [ Attr.href "/"; ] [ raw "<< Back home" ]
            raw blogPost.Body 
        ]
        |> layout blogPost.Title 

    let index (blogPosts : PostModel[]) =    
        let postElement p =
            Elem.div [] [ 
                    Elem.span [] [ raw (p.Date.ToShortDateString()) ]
                    Elem.span [] [ raw " &mdash; "]
                    Elem.a [ Attr.href p.Slug ] [ raw p.Title ]
                ]

        let postElements = 
            blogPosts         
            |> Array.map postElement        
            |> List.ofArray

        [ 
            Elem.h1 [] [ raw "Falco Blog "]
            Elem.h2 [] [ raw "Posts"]                
        ] @ postElements
        |> layout "Falco Blog"

    let notFound slug =
        let msg = 
            match slug with
            | None -> "Invalid post URL"
            | Some slug -> (sprintf "Post with URL %s was not found" slug)

        [
            Elem.h1 [] [ raw "Not Found"]
            Elem.p  [] [ raw msg ]
        ]
        |> layout "Not Found"
    
[<RequireQualifiedAccess>]
module Controller =
    open Falco 
    open Falco.StringUtils
    open Model
    
    let details (posts : PostModel[]) : HttpEndpoint =   
        get "/{slug:regex(^[a-z\-])}" (fun ctx ->
            let findPost slug = 
                posts 
                |> Array.tryFind (fun post -> strEquals post.Slug slug)

            let handleNotFound slug =
                slug
                |> View.notFound 
                |> Response.ofHtml

            let handlePost post =
                post
                |> View.details
                |> Response.ofHtml

            let respondWith =
                match Request.tryGetRouteValue "slug" ctx with
                | None      -> handleNotFound None            
                | Some slug ->

                    match findPost slug with
                    | None      -> handleNotFound (Some slug)
                    | Some post -> handlePost post

            respondWith ctx)
                        
            
    let index (posts : PostModel[]) : HttpEndpoint =       
        get "/" (posts |> Array.sortBy (fun p -> p.Date) |> View.index |> Response.ofHtml)
