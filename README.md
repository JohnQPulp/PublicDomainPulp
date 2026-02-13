### Public Domain Pulp

This repository contains all the code and contents for running the Public Domain Pulp website (https://publicdomainpulp.com).

#### About

See https://publicdomainpulp.com/about, but the gist is that this is an open source project around transforming public domain books into visual novels.

The idea is that all this great prose is just sitting out there, but only in your boring, standard-book form. All those texts could be so much more entertaining if only they had pictures, too!

And what makes the visual novel format so great is that, unlike adaptations into movies or TV shows or graphic novels or what-have-you, the visual novel can fully retain the original prose of the converted texts — no abridging or simplifying of the source needed. You get the full literary experience, but without having to keep track of who's talking when Hemingway fails to attribute dialogue ten paragraphs in-a-row.

The ultimate end goal is to have all public domain fiction books readable as visual novels, each with a corresponding open source repo that anyone can pull from and/or improve upon.

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

For visual novel contributions, I'm open to any and all suggestions for improvements, whether that be background changes, sprite changes, line-break flow improvements, or what-have-you. Identifying straight-up mistakes is especially valuable, as while I try to eliminate these in the editing process, they can still crop up in the form of incorrect speaker attributions, image/text inconsistencies, speaker name spelling mistakes, and so forth.

Feel free to submit any such ideas to their associated VN repos as issues.

I'll also take a look at pull requests, though I haven't yet completed the documentation around the pulpification process and its formats and tooling. Therefore, it might be difficult to contribute changes directly, since for example, I have a whole pipeline around making the generated images consistent in style across all the sprites and backgrounds, but it's not written-up yet. But once completed, the documentation should not only explain how to make good edits to the existing VNs, but also the full process for creating VNs from books from scratch. Here are the current links:

* [Converting Books Into Visual Novels Part 0: The pulp.txt Format](https://publicdomainpulp.com/blog/2026-02-13)

Lastly, for code changes specifically, for this repo and/or the Pulpifier repo, I'm also open to receiving issues and/or pull requests. However, documenting a process for this is a lower priority, just because the codebases themselves are rather small and simple, and I don't anticipate a whole lot of work really needing doing. Parsing and building and then serving the visual novels is not a particularly complex problem, so it doesn't really need ongoing collaborative development.
