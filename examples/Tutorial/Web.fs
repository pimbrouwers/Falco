namespace FalcoTutorial.Web

/// Kestrel endpoint routes
module Route =
    let index = "/"
    let notFound = "/not-found"
    let entryCreate = "/entry/create"
    let entryEdit = "/entry/edit/{entry_id}"

/// Url factories
module Urls =
    open System

    let index = Route.index
    let notFound = Route.notFound
    let entryCreate = Route.entryCreate
    let entryEdit (entryId : Guid) = Route.entryEdit.Replace("{entry_id}", entryId.ToString("d"))

/// Shared markup
module View =
    open Falco.Markup

    /// Master layout which accepts a title and content for <body></body>
    let layout (title : string) (content : XmlNode list) =
        Elem.html [ Attr.lang "en"; Attr.class' "bg-dark-gray" ] [
            Elem.head [] [
                Elem.meta  [ Attr.charset "UTF-8" ]
                Elem.meta  [ Attr.httpEquiv "X-UA-Compatible"; Attr.content "IE=edge, chrome=1" ]
                Elem.meta  [ Attr.name "viewport"; Attr.content "width=device-width, initial-scale=1" ]
                Elem.title [] [ Text.rawf "%s | FalcoJournal" title ]
                Elem.link  [ Attr.href "/tachyons.css"; Attr.rel "stylesheet" ] ]

            Elem.body [ Attr.class' "mw7 center ph3 f4-l georgia white-90" ] [
                Elem.main [] content
                Elem.script [ Attr.src "/index.js"; Attr.type' "text/javascript" ] [] ] ]

    /// Page title as <h1></h1>
    let pageTitle (title : string) =
        Elem.h1 [ Attr.class' "pb3 code tc white-90" ] [ Text.raw title ]

    /// Top bar with pluggable actions
    let topBar (actions : XmlNode list) =
        Elem.header [ Attr.class' "pv4" ] [
            Elem.nav [ Attr.class' "flex items-center" ] [
                Elem.a [ Attr.class' "f4 f3-l white-90 no-underline"
                         Attr.href Urls.index ] [ Text.raw "Falco Journal" ]
                Elem.div [ Attr.class' "flex-grow-1 tr" ] actions ] ]

    /// Button link elements
    module Buttons =
        let solidGray label url =
            let attr =  [
                Attr.href url
                Attr.class' "dib pa2 bg-light-gray dark-gray no-underline bn br1" ]

            Elem.a attr [ Text.raw label ]

        let solidWhite label url =
            let attr =  [
                Attr.href url
                Attr.class' "dib pa2 ph3-l white-90 no-underline bn br1" ]

            Elem.a attr [ Text.raw label ]

    /// Form elements
    module Forms =
        let inputText attr =
            // safely combine custom attributes with defaults
            let defaultAttr = [
                Attr.type' "text"
                Attr.class' "pa2 ba b--silver br1" ]

            let mergedAttr = attr |> Attr.merge defaultAttr
            Elem.input mergedAttr

        let submit attr =
            // safely combine custom attributes with defaults
            let defaultAttr = [
                Attr.type' "submit"
                Attr.class' "dib pa2 bg-light-gray dark-gray bn br1"]

            let mergedAttr = attr |> Attr.merge defaultAttr
            Elem.input mergedAttr

module ErrorController =
    open Falco
    open View

    let badRequest : HttpHandler =
        Response.withStatusCode 400
        >> Response.ofPlainText "Bad request"

    let notFound : HttpHandler =
        let html =
            layout "Not Found" [
                pageTitle "Not found" ]

        Response.withStatusCode 404
        >> Response.ofHtml html

module EntryViews =
    open System
    open Falco.Markup
    open Falco.Security
    open FalcoTutorial.Domain.EntryModel
    open View

    let index (entries : EntrySummary list) =
        let title = "Journal Entries"

        layout title [
            topBar [ Buttons.solidGray "New Entry" Urls.entryCreate ]
            pageTitle title

            Elem.div [] [
                if entries.Length = 0 then
                    Elem.i [] [ Text.raw "No entries have been added" ]
                else
                    for e in entries do
                        Elem.a [ Attr.href (Urls.entryEdit e.EntryId); Attr.class' "db mb4 no-underline white-90" ] [
                            Elem.div [ Attr.class' "mb1 f6 code moon-gray" ] [ Text.raw (e.EntryDate.ToString("yyyy/MM/dd HH:MM")) ]
                            Elem.div [] [ Text.raw e.Summary ]
                            Elem.div [ Attr.class' "w1 mt3 bt b--moon-gray" ] [] ] ] ]

    let save action entry token =
        let title = "A place for your thoughts..."

        let htmlContent =
            match entry.HtmlContent with
            | str when String.IsNullOrWhiteSpace(str) -> Elem.li [] []
            | _ -> Text.raw entry.HtmlContent

        layout title [
            topBar [
                Forms.submit [ Attr.value "Save Entry"; Attr.form "entry-editor" ]
                Buttons.solidWhite "Cancel" Urls.index ]

            pageTitle title

            Elem.form [ Attr.id "entry-editor"; Attr.class' "lh-copy"; Attr.method "post"; Attr.action action ] [
                Xss.antiforgeryInput token
                Elem.ul [
                    Attr.id "bullet-editor"
                    Attr.class' "mh0 pl3 outline-0"
                    Attr.create "contenteditable" "true" ] [
                        htmlContent ]
                Elem.input [ Attr.type' "hidden"; Attr.name "entry_id"; Attr.value (string entry.EntryId) ]
                Elem.input [ Attr.type' "hidden"; Attr.name "html_content"; Attr.id "bullet-editor-html" ]
                Elem.input [ Attr.type' "hidden"; Attr.name "text_content"; Attr.id "bullet-editor-text" ] ] ]

module EntryController =
    open System
    open Falco
    open FalcoTutorial.Domain.EntryModel
    open FalcoTutorial.Infrastructure
    open FalcoTutorial.Service

    /// GET /
    let index : HttpHandler = Falco.plug<IDbConnectionFactory> <| fun dbConnection ->
        let entries = EntryService.getAll dbConnection
        Response.ofHtml (EntryViews.index entries)

    /// GET /entry/create
    let create : HttpHandler = fun ctx ->
        let newEntry = newEntry ()
        let view = EntryViews.save Urls.entryCreate newEntry
        Response.ofHtmlCsrf view ctx

    /// GET /entry/edit/{id}
    let edit : HttpHandler = 
        Falco.plug<IDbConnectionFactory> <| fun dbConnection ->
        let readRoute (route : RequestData) =
            route.TryGetGuid "entry_id"

        let handle (input : Guid option) = 
            match input with
            | Some entryId ->
                match EntryService.get dbConnection entryId with
                | Some entry ->
                    let html = EntryViews.save (Urls.entryEdit entryId) entry
                    Response.ofHtmlCsrf html
                | None ->
                    ErrorController.notFound
            | None ->
                Response.redirectTemporarily Urls.notFound

        Request.mapRoute readRoute handle

    /// POST /entry/create, /entry/edit/{id}
    let save : HttpHandler = Falco.plug<IDbConnectionFactory> <| fun dbConnection ->
        let readForm (form : RequestData) =
            { EntryId = form.GetGuid "entry_id"
              HtmlContent = form.GetString "html_content"
              TextContent = form.GetString "text_content" }

        let handle (input : Entry) =
            EntryService.save dbConnection input
            Response.redirectTemporarily Urls.index

        Request.mapForm readForm handle
