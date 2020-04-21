module Blog.Model

open System
open System.Collections.Generic
open System.Globalization
open System.IO
open System.Linq
open Markdig
open Markdig.Extensions.Yaml
open Markdig.Renderers
open Markdig.Syntax
open YamlDotNet.Core
open YamlDotNet.Core.Events
open YamlDotNet.Serialization
open YamlDotNet.Serialization.NamingConventions

type BlogPost =
    {
        Slug  : string
        Title : string
        Date  : DateTime
        Body  : string
    }

module BlogPost =
    let contentRoot = Directory.GetCurrentDirectory()
    let postsDirectory = Path.Combine(contentRoot, "Posts")

    let parseTitleFromYaml (yaml : string) =
        let des = 
            DeserializerBuilder().
                WithNamingConvention(CamelCaseNamingConvention.Instance).
                Build()
        use rd = new StringReader(yaml)
        let parser = new Parser(rd)

        parser.Consume<StreamStart>() |> ignore
        parser.Consume<DocumentStart>() |> ignore

        let dict = des.Deserialize<Dictionary<string,string>>(parser)

        parser.Consume<DocumentEnd>() |> ignore

        if dict.ContainsKey "title" then dict.["title"]
        else failwith "Blog post must have title attr in YAML"
        
    let parseMarkdown (markdown : string) =
        let pipeline = MarkdownPipelineBuilder().UseYamlFrontMatter().Build()
        use sw = new StringWriter()
        let renderer = HtmlRenderer(sw)
        
        pipeline.Setup(renderer) |> ignore
        
        let doc = Markdown.Parse(markdown, pipeline)
        
        let title = 
            match doc.Descendants<YamlFrontMatterBlock>().FirstOrDefault() with
            | null      -> failwith "Blog post must contain YAML front matter"
            | yamlBlock -> 
                markdown.Substring(yamlBlock.Span.Start + 3, yamlBlock.Span.Length - 3)
                |> parseTitleFromYaml 
            
        renderer.Render(doc) |> ignore
        sw.Flush() |> ignore
        let body = sw.ToString()  

        title, body

    let parseBlogPost (postPath : string) =         
        let path = Path.GetFileNameWithoutExtension(postPath)
        let date = DateTime.ParseExact(path.Substring(0, 10), "yyyy-MM-dd", CultureInfo.InvariantCulture)
        let slug = path.Substring(11)
        
        let pipeline = MarkdownPipelineBuilder().UseYamlFrontMatter().Build()
        use sw = new StringWriter()
        let renderer = HtmlRenderer(sw)
        
        pipeline.Setup(renderer) |> ignore
        
        let markdown = File.ReadAllText(postPath)
        let title, html = parseMarkdown markdown
        
        {
            Slug = slug
            Title = title
            Date = date
            Body = html
        }
        
    let all =        
        postsDirectory
        |> Directory.GetFiles
        |> Array.map parseBlogPost
        