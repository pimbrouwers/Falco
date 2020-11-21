# Falco Templates

To check these templates locally:

- `cd src/Templates`
- `dotnet pack`
- `dotnet new -i C:\PATH\TO\NUPKG\PACKAGE\Falco.Templates.0.1.0.nupkg`

To uninstall the local version

- `dotnet new -u Falco.Templates`


## Add a new template
To add a new template you can do the following

- `cd templates`
- `dotnet new falco.basic -o Falco.*` where `*` is the name of your next template (e.g. `Falco.Advanced`)

update the code as needed and don't forget to add a `.template.config/template.json` file with the new configuration

```jsonc
{
    "$schema": "http://json.schemastore.org/template",
    "author": "Falco",
    "classifications": [
        "Web",
        "F#"
    ],
    "identity": "Falco.Basic", // update this
    "name": "Falco Basic Template",
    "sourceName": "Falco.Basic", // update this
    "preferNameDirectory": true,
    "shortName": "falco.basic", // update this
    "defaultName": "AppServer",
    "tags": {
        "language": "F#",
        "type": "project"
    },
    "description": "Provides a bare bones Falco project just to get you started right away." // update this
}
```
