namespace Falco.Forms

open System
open Falco.Markup
open Falco.Security
open Microsoft.AspNetCore.Antiforgery

type FormInputConfig<'a> =
    { Name : string
      Value : 'a option }

type FormGroupConfig<'a> =
    { Label : string
      Input : FormInputConfig<'a>
      Attrs : XmlAttribute list }

type SelectItem =
    { Label : string
      Value : string }

    static member Empty = { Label = "--"; Value = "" }

type SelectFormGroupConfig<'a> =
    { Label : string
      Input : FormInputConfig<'a>
      Attrs : XmlAttribute list
      Items : SelectItem list }

type FormInput<'a> =
    | BooleanInput of FormGroupConfig<bool>
    | DateInput of FormGroupConfig<DateTime>
    | DateTimeInput of FormGroupConfig<DateTime>
    | EmailInput of FormGroupConfig<string>
    | FileInput of FormGroupConfig<unit>
    | HiddenInput of FormInputConfig<'a>
    | Int16Input of FormGroupConfig<int16>
    | Int32Input of FormGroupConfig<int>
    | DecimalInput of FormGroupConfig<decimal>
    | PasswordInput of FormGroupConfig<string>
    | SelectInput of SelectFormGroupConfig<'a>
    | SelectMultipleInput of SelectFormGroupConfig<'a list>
    | TextInput of FormGroupConfig<string>
    | LargeTextInput of FormGroupConfig<string>

module Forms =
    let errorSummary (errors : string seq) =
        match errors with
        | x when Seq.isEmpty x -> Text.empty
        | _ ->
            Elem.blockquote [] [
                Elem.ul [] [
                for e in errors do
                    Elem.li [] [ Text.raw e ] ] ]

    // form types
    let formGet errors controls =
        Elem.form [ Attr.method "get" ] [
            errorSummary errors
            yield! controls
        ]

    let formPost errors (token : AntiforgeryTokenSet) controls =
        Elem.form [ Attr.method "post" ] [
            Xss.antiforgeryInput token
            errorSummary errors
            yield! controls
        ]

    let formPostMultipart errors (token : AntiforgeryTokenSet) controls =
        Elem.form [ Attr.method "post"; Attr.enctype "multipart/form-data" ] [
            Xss.antiforgeryInput token
            errorSummary errors
            yield! controls
        ]

    let formGroup name label input =
        Elem.div [] [
            Elem.label [ Attr.for' name ] [ Text.raw label ]
            input ]

    let formInput inputType name value attrs =
        let valueStr = match value with Some x -> x | None -> ""

        let attrs =
            Attr.merge attrs [
                Attr.id name
                Attr.name name
                Attr.type' inputType
                Attr.value valueStr ]

        Elem.input attrs

    let formInputDate = formInput "date"
    let formInputDatetime = formInput "datetime-local"
    let formInputEmail = formInput "email"
    let formInputFile = formInput "file"
    let formInputHidden = formInput "hidden"
    let formInputNumber = formInput "number"
    let formInputPassword = formInput "password"
    let formInputText = formInput "text"

    let formSelect name values attrs options =
        let attrs =
            Attr.merge attrs [
                Attr.id name
                Attr.name name ]

        let options =
            [ for { Label = label; Value = value } in options ->
                let attrs =
                    if List.contains value values then [ Attr.value value; Attr.selected ]
                    else [ Attr.value value ]

                Elem.option attrs [ Text.raw label ] ]

        Elem.select attrs options

    let formTextarea name value attrs =
        let valueStr = value |> Option.defaultValue ""

        let attrs =
            Attr.merge attrs [
                Attr.id name
                Attr.name name ]

        Elem.textarea attrs [ Text.raw valueStr ]

    let rec input (config : FormInput<'a>) =
        match config with
        | BooleanInput { Label = label; Input = input; Attrs = attrs } ->
            let attrs =
                let defaultAttrs =
                    Attr.type' "checkbox"
                    :: Attr.name input.Name
                    :: attrs

                match input.Value with
                | Some x when x = true -> Attr.checked' :: defaultAttrs
                | _ -> defaultAttrs

            Elem.input attrs
            |> formGroup input.Name label

        | DateInput { Label = label; Input = input; Attrs = attrs } ->
            let value = input.Value |> Option.map _.ToString("yyyy-MM-dd")
            formInputDate input.Name value attrs
            |> formGroup input.Name label

        | DateTimeInput { Label = label; Input = input; Attrs = attrs } ->
            let value = input.Value |> Option.map _.ToString("yyyy-MM-ddTHH:mm")
            formInputDate input.Name value attrs
            |> formGroup input.Name label

        | EmailInput { Label = label; Input = input; Attrs = attrs } ->
            formInputEmail input.Name input.Value attrs
            |> formGroup input.Name label

        | FileInput  { Label = label; Input = input; Attrs = attrs } ->
            formInputFile input.Name None attrs
            |> formGroup input.Name label

        | HiddenInput { Name = name; Value = value } ->
            let valueStr = Option.map string value
            formInputHidden name valueStr []

        | Int16Input { Label = label; Input = input; Attrs = attrs } ->
            let valueStr = Option.map string input.Value
            formInputNumber input.Name valueStr attrs
            |> formGroup input.Name label

        | Int32Input { Label = label; Input = input; Attrs = attrs } ->
            let valueStr = Option.map string input.Value
            formInputNumber input.Name valueStr attrs
            |> formGroup input.Name label

        | DecimalInput { Label = label; Input = input; Attrs = attrs } ->
            let valueStr = Option.map string input.Value
            formInputNumber input.Name valueStr (Attr.step ".01" :: attrs)
            |> formGroup input.Name label

        | PasswordInput { Label = label; Input = input; Attrs = attrs } ->
            formInputPassword input.Name input.Value attrs
            |> formGroup input.Name label

        | SelectInput { Label = label; Input = input; Attrs = attrs; Items = items } ->
            let values = Option.map (fun x -> [ string x ]) input.Value |> Option.defaultValue []
            formSelect input.Name values attrs items
            |> formGroup input.Name label

        | SelectMultipleInput { Label = label; Input = input; Attrs = attrs; Items = items } ->
            let values = Option.map (List.map string) input.Value |> Option.defaultValue []
            let itemsCount = string (List.length items)
            formSelect input.Name values (Attr.multiple :: Attr.size itemsCount :: attrs) items
            |> formGroup input.Name label

        | TextInput { Label = label; Input = input; Attrs = attrs } ->
            formInputText input.Name input.Value attrs
            |> formGroup input.Name label

        | LargeTextInput { Label = label; Input = input; Attrs = attrs } ->
            let attrs = Attr.merge attrs [ Attr.rows "5" ]
            formTextarea input.Name input.Value attrs
            |> formGroup input.Name label

    let booleanInput (config : FormGroupConfig<bool>) = BooleanInput config |> input
    let dateInput (config : FormGroupConfig<DateTime>) = DateInput config |> input
    let dateTimeInput (config : FormGroupConfig<DateTime>) = DateTimeInput config |> input
    let emailInput (config : FormGroupConfig<string>) = EmailInput config |> input
    let fileInput (config : FormGroupConfig<unit>) = FileInput config |> input
    let hiddenInput (config : FormInputConfig<'a>) = HiddenInput config |> input
    let int16Input (config : FormGroupConfig<int16>) = Int16Input config |> input
    let int32Input (config : FormGroupConfig<int>) = Int32Input config |> input
    let decimalInput (config : FormGroupConfig<decimal>) = DecimalInput config |> input
    let passwordInput (config : FormGroupConfig<string>) = PasswordInput config |> input
    let selectInput (config : SelectFormGroupConfig<'a>) = SelectInput config |> input
    let selectMultipleInput (config : SelectFormGroupConfig<'a list>) = SelectMultipleInput config |> input
    let textInput (config : FormGroupConfig<string>) = TextInput config |> input
    let largeTextInput (config : FormGroupConfig<string>) = LargeTextInput config |> input

    let submit name value attrs =
        let name' = name |> Option.defaultValue "submit"
        let attrs =
            Attr.type' "submit"
            :: Attr.name name'
            :: Attr.value value
            :: attrs

        Elem.input attrs

    let defaultSubmit =
        submit None "Submit" []
