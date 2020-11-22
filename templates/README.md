# Falco Templates

[![NuGet Version](https://img.shields.io/nuget/v/Falco.svg)](https://www.nuget.org/packages/Falco)


## Running locally

To try these templates locally from source:

- `cd src/Templates`
- `dotnet pack`
- `dotnet new -i C:\PATH\TO\NUPKG\PACKAGE\Falco.Template.x.x.x.nupkg`

To uninstall the local version

- `dotnet new -u Falco.Templates`

## Adding a new project type
To add a new template you can do the following

- `cd templates`
- `dotnet new falco -o *` where `*` is the name of your next template.

Update the code as needed and don't forget to add the project type to the `.template.config/template.json` file.

```json
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
