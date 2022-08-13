namespace Falco.Markup

open System
open System.Net

module Text =
    /// Empty Text node
    let empty = 
        TextNode String.Empty

    /// Encoded-text XmlNode constructor
    let enc (content : string) = 
        TextNode (WebUtility.HtmlEncode content)

    /// Text XmlNode constructor
    let raw (content : string) = 
        TextNode content

    /// Text XmlNode constructor that will invoke "sprintf template"
    let rawf (template : Printf.StringFormat<'a, XmlNode>) = 
        Printf.kprintf raw template

    /// HTML Comment Text XmlNode construction
    let comment (content : string) = 
        rawf "<!-- %s -->" content
