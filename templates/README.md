# Falco Templates

To check these templates locally:

- `cd templates`
- `dotnet pack`
- `dotnet new -i C:\PATH\TO\NUPKG\PACKAGE\Falco.Template.0.1.0.nupkg`

To uninstall the local version

- `dotnet new -u Falco.Template`


## Add a new template
To add a new template you can do the following

- `cd templates`
- `dotnet new falco --ProjectType * -o **` where `*` is the base template (e.g. basic, mvc, rest) and `**` is the name of your next template (e.g. `RestSwagger`)

update the code as needed and don't forget update the `.template.config/template.json` file with the new configuration

```jsonc
{
    "$schema": "http://json.schemastore.org/template",
    /*
     ... omitted code  ...
    */
    "symbols": {
        "ProjectType": {
            "type": "parameter",
            "dataType": "choice",
            "defaultValue": "basic",
            // add your template here
            "choices": [
                {
                    "choice": "basic",
                    "description": "A basic Falco app"
                },
                {
                    "choice": "mvc",
                    "description": "An MVC-style Falco app"
                },
                {
                    "choice": "rest",
                    "description": "A RESTful JSON API implemented using Falco"
                }
            ]
        }
    },
    "sources": [
        // dont forget to include the sources as well
        {
            "source": "./Basic/",
            "target": "./",
            "condition": "ProjectType== \"basic\""
        },
        {
            "source": "./Mvc/",
            "target": "./",
            "condition": "ProjectType== \"mvc\""
        },
        {
            "source": "./Rest/",
            "target": "./",
            "condition": "ProjectType== \"rest\""
        },
    ]
}
```
