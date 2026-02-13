### Public Domain Pulp

This repository contains all the code and contents for running the Public Domain Pulp website (https://publicdomainpulp.com).

#### About

See https://publicdomainpulp.com/about, but the gist is that this is an open source project around transforming public domain books into visual novels.

The idea is that all this great prose is just sitting out there, but only in boring standard-book form. And wouldn't it all be much more entertaining if they were visual novels instead. (Ooh, pictures!)

Also, as part of the site, we do a little editorializing. (Not changing or simplifying the original writing, mind you — [that would be bad](https://www.goodreads.com/book/show/13079982-fahrenheit-451) — but just adding "helpful" editor's notes.)

#### Structure

* `PublicDomainPulp/`: The ASP.NET/C# project that contains the middleware pipeline, routings, and web helpers.
* `PublicDomainPulp.Tests/`: The integration tests for the website.
* `Pulpifier/`: The https://github.com/JohnQPulp/Pulpifier submodule, a prerequisite for the website.
    * `Pulpifier/Pulpifier/`: The C# library that builds the visual novel `pulp.txt` files into html.
    * `Pulpifier/Pulpifier.Tests/`: The unit tests for the library.
    * `Pulpifier/PulpifierCLI/`: CLI tool for invoking the library.
* `VisualPulps/`: A directory containing all of the website's CC0 visual novel contents
    * `VisualPulps/CupOfGold/`: The https://github.com/JohnQPulp/CupOfGold.git submodule (*Cup of Gold: The Visual Novel*).
    * `VisualPulps/TheThirtyNineSteps/`: The https://github.com/JohnQPulp/TheThirtyNineSteps.git submodule (*The Thirty-Nine Steps: The Visual Novel*).
    * etc
* `CreativeCommonsContent/`: The https://github.com/JohnQPulp/CreativeCommonsFiction.git submodule containing the website's CC0 blog contents.

#### Running the Site

To start, ensure you have .NET/ASP.NET 10 installed: https://dotnet.microsoft.com/download

Check out, build, and run the site like:

```bash
git clone https://github.com/JohnQPulp/PublicDomainPulp.git
cd PublicDomainPulp
git submodule update --init --recursive
dotnet build PublicDomainPulp.slnx
dotnet run --no-build --project PublicDomainPulp --launch-profile http
```

A simple setup for a simple website. (Since if I'm stealing Standard Ebooks' texts, I might as well steal [their principles](https://alexcabal.com/posts/standard-ebooks-and-classic-web-tech) too.)

#### Contributing

For code changes, you *could* submit pull requests to this repo and/or the Pulpifier, although it might be better to just start with an issue. (I'm extremely opinionated on the issue of keeping things as simple as possible *from my perspective*, which I accept does not make my codebase especially comprehensible for others.)

For visual novel contributions, I'm more open to any and all ideas people have for improvements, whether that be background changes, sprite changes, line-break changes, or whatever. And of course, identifying mistakes in the VNs is also welcome (e.g., incorrect speaker attributions, speaker name spelling mistakes, images inconsistent with the original text, etc). Feel free to submit issues and/or pull requests to any of the visual novel repos.

For pull requests specifically, I'm working on writing up a series of blog posts documenting my full process for converting books into visual novels, which should help explain why the setup is as it is, the tooling available for making improvements, and even the information necessary for doing a VN-conversion from scratch of some as-of-yet unconverted text. Here are the current links:

* [Converting Books Into Visual Novels Part 0: The pulp.txt Format](https://publicdomainpulp.com/blog/2026-02-13)
