open System
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running

module Markup =
    open Falco.Markup
    open Giraffe.ViewEngine
    open Scriban

    type Product =
        { Name : string 
          Price : float 
          Description : string }

    let lorem = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum";

    let products = 
        [ 1..500 ]
        |> List.map (fun i -> { Name = sprintf "Name %i" i; Price = i |> float; Description = lorem})

    let falcoTemplate products = 
        let elem product =                
            Elem.li [] [ 
                Elem.h2 [] [ Text.raw product.Name ] 
                Text.rawf "Only %f" product.Price
                Text.raw product.Description ]

        products
        |> List.map elem
        |> Elem.ul [ Attr.id "products" ] 

    let giraffeTemplate products =
        let elem product =
            li [] [
                h2 [] [ str product.Name ] 
                strf "Only %f" product.Price 
                str product.Description ]

        products
        |> List.map elem
        |> ul [ _id "products "] 
    
    let scribanTemplateStr = "
        <ul id='products'>
            {{ for product in products; with product }}
            <li>
                <h2>{{ name }}</h2>
                    Only {{ price }}
                    {{ description }}
            </li>
            {{ end; end }}
        </ul>"

    let scribanTemplate = Template.Parse(scribanTemplateStr)

    [<MemoryDiagnoser>]
    type Bench() =
        [<Benchmark>]
        member _.StringBuilder() = 
            let sb = new Text.StringBuilder() 
            sb.Append("<ul id='products'>")
            
            for p in products do
                sb.Append("<li><h2>")
                sb.Append(p.Name)
                sb.Append("</h2>Only ")
                sb.Append(sprintf "%f" p.Price)
                sb.Append(p.Description)
                sb.Append("</li>")

            sb.Append("</ul>")
            sb.ToString ()

        [<Benchmark>]
        member _.Falco() =
            products
            |> falcoTemplate
            |> renderNode

        [<Benchmark>]
        member _.Giraffe() =
            products
            |> giraffeTemplate
            |> RenderView.AsString.htmlNode

        // [<Benchmark>]
        // member _.Scriban() =             
        //     scribanTemplate.Render(products)

[<EntryPoint>]
let main argv =        
    BenchmarkRunner.Run<Markup.Bench>() |> ignore
    0 // return an integer exit code