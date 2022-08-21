open System
open System.Collections.Generic
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open Falco.Markup

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
        [ 1..5 ]
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

    let scribanTemplate =
        "<ul id='products'>
            {{ for product in products; with product }}
            <li>
                <h2>{{ name }}</h2>
                    Only {{ price }}
                    {{ description }}
            </li>
            {{ end; end }}
        </ul>"
        |> fun str -> Template.Parse(str)

    [<MemoryDiagnoser>]
    type RenderBench() =
        [<Benchmark(Baseline = true)>]
        member _.StringBuilder() =
            let sb = new Text.StringBuilder()
            sb.Append("<ul id='products'>")

            for p in products do
                sb.Append("<li><h2>")
                  .Append(p.Name)
                  .Append("</h2>Only ")
                  .Append(sprintf "%f" p.Price)
                  .Append(p.Description)
                  .Append("</li>") |> ignore
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

        [<Benchmark>]
        member _.Scriban() =
            scribanTemplate.Render(products)

    [<MemoryDiagnoser>]
    type MergeBench() =
        let attrs1 = [ Attr.class' "ma2 ds axzcx cxzc cxzc xzczx xzzc"; Attr.target "_blank"; Attr.href "https://dotnet.microsoft.com"; Attr.title "This is a title for an attribute"; Attr.alt "Alt text" ]
        let attrs2 = [ Attr.id "some-el"; Attr.class' "bg-red"; Attr.readonly; Attr.disabled; ]

        [<Benchmark(Baseline = true)>]
        member _.NestedLists() =
            (attrs2, attrs1)
            ||> List.fold (fun acc elem -> elem :: acc)
            |> List.map (fun attr ->
                match attr with
                | KeyValueAttr(k, v) -> k, Some v
                | NonValueAttr(k) -> k, None)
            |> List.groupBy (fun (k, _) -> k)
            |> List.map (fun (g, attrs) ->
                let attrValue : string option =
                    attrs
                    |> List.fold (fun acc (_, v) ->
                        match acc, v with
                        | None, _          -> v
                        | Some _, None     -> acc
                        | Some acc, Some v ->
                            Some (String.Join(" ", [| acc; v |]))) None

                match attrValue with
                | None   -> NonValueAttr(g)
                | Some v -> KeyValueAttr(g, v))

        [<Benchmark>]
        member _.DictionaryOfOptionsStringConcat() =
            Attr.merge attrs1 attrs2

[<EntryPoint>]
let main argv =
    // BenchmarkRunner.Run<Markup.RenderBench>() |> ignore
    BenchmarkRunner.Run<Markup.MergeBench>() |> ignore
    0 // return an integer exit code
