# Falco Templates

[![NuGet Version](https://img.shields.io/nuget/v/Falco.Template.svg)](https://www.nuget.org/packages/Falco.Template)
[![Build Status](https://travis-ci.org/pimbrouwers/Falco.svg?branch=master)](https://travis-ci.org/pimbrouwers/Falco)

## Installation

The easiest way to install the Falco template is by running the following command in your terminal:

```
dotnet new -i "Falco.Template::*"
```

This will pull and install the latest [Falco.Template NuGet package](https://www.nuget.org/packages/Falco.Template/) into your .NET environment and make it available to subsequent `dotnet new` commands.

## Updating the template

Whenever there is a new version of the Falco template you can update it by re-running the [instructions from the installation](#installation).

You can also explicitly set the version when installing the template:

```
dotnet new -i "Falco.Template::1.0.0"
```

## Getting Started

After the template has been installed you can create a new Falco web application by simply running `dotnet new falco` in your terminal:

```
dotnet new falco
```

This will generate a basic ASP.NET web application with Falco installed and activate, demonstrating some basic routing and output techniques.

There are two other project types available, which can be accessed using the `-P` or `--ProjectType` argument:

1. MVC - `dotnet new falco -P mvc`
2. RESTful JSON API - `dotnet new falco -P rest`

## Running locally

To try these templates locally from source:

- `cd src/Templates`
- `dotnet pack`
- `dotnet new -i C:\PATH\TO\NUPKG\PACKAGE\Falco.Template.x.x.x.nupkg`

To uninstall the local version

- `dotnet new -u Falco.Templates`

### Adding a new project type

To add a new template you can do the following

- `cd templates`
- `dotnet new falco -o *` where `*` is the name of your next template.

Update the code as needed and don't forget to add the project type to the `.template.config/template.json` file.