namespace Falco.Markup

open System
open System.Globalization
open System.IO

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

module internal XmlNode =
    let [<Literal>] _openChar = "<"
    let [<Literal>] _closeChar = ">"
    let [<Literal>] _term = "/"
    let [<Literal>] _space = " "
    let [<Literal>] _equals = "="
    let [<Literal>] _quote = "\""

    let serialize (w : StringWriter, xml : XmlNode) =
        // Concatenating constants can be optimized by the compiler if
        // combined using the "+"
        let writeAttributes attrs =
            for attr in (attrs : XmlAttribute list) do
                if attrs.Length > 0 then
                    w.Write _space

                match attr with
                | NonValueAttr attrName ->
                    w.Write attrName

                | KeyValueAttr (attrName, attrValue) ->
                    w.Write attrName
                    w.Write (_equals + _quote)
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
                w.Write (_space + _term + _closeChar)

            | ParentNode ((tag, attrs), children) ->
                w.Write _openChar
                w.Write tag
                writeAttributes attrs
                w.Write _closeChar

                for c in children do
                    buildXml c

                w.Write (_openChar + _term)                
                w.Write tag
                w.Write _closeChar

        buildXml xml

        w.GetStringBuilder().ToString()

[<AutoOpen>]
module XmlNodeRenderer = 
    /// Render XmlNode recursively to string representation
    let renderNode (tag : XmlNode) =
        let sb = Text.StringBuilder()
        let w = new StringWriter(sb, CultureInfo.InvariantCulture)
        XmlNode.serialize(w, tag)

    /// Render XmlNode as HTML string
    let renderHtml (tag : XmlNode) =
        let sb = Text.StringBuilder()
        let w = new StringWriter(sb, CultureInfo.InvariantCulture)
        w.Write "<!DOCTYPE html>"
        XmlNode.serialize(w, tag)

    /// Render XmlNode as XML string
    let renderXml (tag : XmlNode) =
        let sb = Text.StringBuilder()
        let w = new StringWriter(sb, CultureInfo.InvariantCulture)
        w.Write "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
        XmlNode.serialize(w, tag)