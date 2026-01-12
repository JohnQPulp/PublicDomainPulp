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

An even easier setup than https://github.com/standardebooks/web.git, if I do say so myself. Take *that*, [Alex Cabal](https://alexcabal.com/posts/standard-ebooks-and-classic-web-tech).
