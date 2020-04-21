module Blog.Handlers

open Microsoft.AspNetCore.Http
open Falco
open Blog.Model
open Blog.UI

let notFoundHandler =
    setStatusCode 404 >=> textOut "Not found"

let blogPostHandler : HttpHandler = 
    fun (next : HttpFunc) (ctx : HttpContext) ->
        let slug = ctx.TryGetRouteValue "slug" |> Option.defaultValue "slug"
        (BlogPost.all
        |> Array.tryFind (fun p -> strEquals p.Slug slug)
        |> function
           | None      -> notFoundHandler
           | Some post -> post |> blogPostView |> htmlOut) next ctx 

let blogIndexHandler : HttpHandler =
    BlogPost.all |> indexView |> htmlOut
   

