module Blog.Markdown

open System.Collections.Generic
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

type ParsedMarkdownDocument =
    {
        Title : string
        Body  : string 
    }

let parseTitleFromYaml (yaml : string) =
    let des = 
        DeserializerBuilder().
            WithNamingConvention(CamelCaseNamingConvention.Instance).
            Build()
    use rd = new StringReader(yaml)
    let parser = Parser(rd)

    parser.Consume<StreamStart>() |> ignore
    parser.Consume<DocumentStart>() |> ignore

    let dict = des.Deserialize<Dictionary<string,string>>(parser)

    parser.Consume<DocumentEnd>() |> ignore

    if dict.ContainsKey "title" then dict.["title"]
    else failwith "Blog post must have title attr in YAML"
    
let renderMarkdown (markdown : string) =
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

    {
        Title = title
        Body  = body 
    }    

