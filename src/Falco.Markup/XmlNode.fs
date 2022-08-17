namespace Falco.Markup

open System
open System.Globalization
open System.IO
open System.Text

/// Specifies an XML-style attribute
type XmlAttribute =
    | KeyValueAttr of string * string
    | NonValueAttr of string

/// Represents an XML-style element containing attributes
type XmlElement =
    string * XmlAttribute list

/// Describes the different XML-style node patterns
type XmlNode =
    | ParentNode      of XmlElement * XmlNode list
    | SelfClosingNode of XmlElement
    | TextNode        of string

[<AbstractClass; Sealed>]
type StringBuilderCache private () =
    // The value 360 was chosen in discussion with performance experts as a compromise between using
    // as litle memory (per thread) as possible and still covering a large part of short-lived
    // StringBuilder creations on the startup path of VS designers.
    [<Literal>]
    static let _maxBuilderSize = 360

    // == StringBuilder.DefaultCapacity
    [<Literal>]
    static let _defaultCapacity = 16

    [<ThreadStatic; DefaultValue>]
    static val mutable private cachedInstance : StringBuilder

    static member Acquire (?capacity : int) =
        let capacity' = defaultArg capacity _defaultCapacity
        let sb = StringBuilderCache.cachedInstance

        // Avoid stringbuilder block fragmentation by getting a new StringBuilder
        // when the requested size is larger than the current capacity
        if capacity' <= _maxBuilderSize &&
           not(isNull sb) &&
           capacity' <= sb.Capacity then
           StringBuilderCache.cachedInstance <- null
           sb.Clear() |> ignore
           sb
        else
            new StringBuilder(capacity')

    static member GetString (sb : StringBuilder) =
        let result = sb.ToString()
        if sb.Capacity <= _maxBuilderSize then StringBuilderCache.cachedInstance <- sb
        result

module internal XmlNode =
    let [<Literal>] _openChar = '<'
    let [<Literal>] _closeChar = '>'
    let [<Literal>] _term = '/'
    let [<Literal>] _space = ' '
    let [<Literal>] _equals = '='
    let [<Literal>] _quote = '"'

    let serialize (w : StringWriter, xml : XmlNode) =
        let writeAttributes attrs =
            for attr in (attrs : XmlAttribute list) do
                if attrs.Length > 0 then
                    w.Write _space

                match attr with
                | NonValueAttr attrName ->
                    w.Write attrName

                | KeyValueAttr (attrName, attrValue) ->
                    w.Write attrName
                    w.Write _equals
                    w.Write _quote
                    w.Write attrValue
                    w.Write _quote

        let rec buildXml tag =
            match tag with
            | TextNode text ->
                w.Write text

            | SelfClosingNode (tag, attrs) ->
                w.Write _openChar
                w.Write tag
                writeAttributes attrs
                w.Write _space
                w.Write _term
                w.Write _closeChar

            | ParentNode ((tag, attrs), children) ->
                w.Write _openChar
                w.Write tag
                writeAttributes attrs
                w.Write _closeChar

                for c in children do
                    buildXml c

                w.Write _openChar
                w.Write _term
                w.Write tag
                w.Write _closeChar

        buildXml xml

        StringBuilderCache.GetString(w.GetStringBuilder())

[<AutoOpen>]
module XmlNodeRenderer =
    /// Render XmlNode recursively to string representation
    let renderNode (tag : XmlNode) =
        let sb = StringBuilderCache.Acquire()
        let w = new StringWriter(sb, CultureInfo.InvariantCulture)
        XmlNode.serialize(w, tag)

    /// Render XmlNode as HTML string
    let renderHtml (tag : XmlNode) =
        let sb = StringBuilderCache.Acquire()
        let w = new StringWriter(sb, CultureInfo.InvariantCulture)
        w.Write "<!DOCTYPE html>"
        XmlNode.serialize(w, tag)

    /// Render XmlNode as XML string
    let renderXml (tag : XmlNode) =
        let sb = StringBuilderCache.Acquire()
        let w = new StringWriter(sb, CultureInfo.InvariantCulture)
        w.Write "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
        XmlNode.serialize(w, tag)