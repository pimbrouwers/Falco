module Blog.PostProcessor

open System
open System.Globalization
open System.IO
open Blog.Domain

type UnprocessedPost =
    {
        Slug : string            
        Date : DateTime
        Body : string
    }

let loadAll (postsDirectory : PostsDirectory) : Post list =  
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

    let processPost (unprocessedPost : UnprocessedPost) : Post =
        let markdownDoc = Markdown.renderMarkdown unprocessedPost.Body            
        {
            Slug = unprocessedPost.Slug
            Title = markdownDoc.Title
            Date = unprocessedPost.Date
            Body = markdownDoc.Body
        }

    postsDirectory
    |> Directory.GetFiles        
    |> List.ofArray
    |> List.map (readPost >> processPost)
    |> List.sortBy (fun p -> p.Date) 

