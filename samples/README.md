# Sample Projects

This directory contains projects which demonstrate "idiomatic" usage of Falco and an opinionated way of structuring F# ASP.NET Core web applications

- [Hello World][1]
	- The iconic and most basic app of all. Demonstrates:
		- Encapsulation of host configuration and startup.
		- Interaction with environment variables.
		- "Constrained" primitives.
		- Usage of Falco Exception & Not Found handler's.

- [Blog][2]
	- A basic markdown blog, with YAML front-matter. Demonstrates:
		- Encapsulation of host configuration and startup.
		- Interaction with environment variables.
		- "Constrained" primitives.
		- Usage of Falco Exception & Not Found handler's. 
		- Usage of Falco HTML View Engine
		- Feature-oriented file structure. `Post.fs` contains modules with similar semantics to MVC.
		- Incorporating static files.
		- Usage of ASP.NET response caching and compression.
		- Partial application as an alternative to dependency injection.
		- Custom middleware, implemented as `HttpHandler`'s
		- Atomic (i.e. functional CSS).


[1]: https://github.com/pimbrouwers/Falco/tree/master/samples/HelloWorld
[2]: https://github.com/pimbrouwers/Falco/tree/master/samples/Blog
