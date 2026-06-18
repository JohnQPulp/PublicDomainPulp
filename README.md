<h1 align='center'>Public Domain Pulp<br><sub>Converting Books Into Open Source Visual Novels</sub></h1>

This repository contains all the code and contents for running the Public Domain Pulp website, including all the CC0-licensed visual novels themselves.

You can read the VNs online at https://publicdomainpulp.com.

Or: You can read the VNs locally by building/running the site via the setup steps.

Or: You can read about the book-to-VN conversion process in the Documentation section further below.

### About

See https://publicdomainpulp.com/about, but TL;DR:

This is a project around converting public domain books into public domain visual novels, unabridged.

The end goal is to have all public domain fiction novels readable as visual novels, each with a corresponding open source (CC0) repo that anyone can pull from and/or improve upon.

### Running the Site

To start, ensure you have .NET/ASP.NET 10 installed: https://dotnet.microsoft.com/download

Check out, build, and run the site like:

```bash
git clone https://github.com/JohnQPulp/PublicDomainPulp.git
cd PublicDomainPulp
git submodule update --init --recursive
dotnet build PublicDomainPulp.slnx
dotnet run --no-build --project PublicDomainPulp --launch-profile http
```

Since every visual novel repo is a submdoule of this repo, the `submodule update` will automatically pull them all in for local reading. [Simple!](https://alexcabal.com/posts/standard-ebooks-and-classic-web-tech)

#### Structure

* `PublicDomainPulp/`: The ASP.NET/C# project that contains the middleware pipeline, routings, and web helpers.
* `PublicDomainPulp.Tests/`: The integration tests for the website.
* `Pulpifier/`: The https://github.com/JohnQPulp/Pulpifier submodule, a prerequisite for the website.
    * `Pulpifier/Pulpifier/`: The C# library that builds the visual novel `pulp.txt` files into html.
    * `Pulpifier/Pulpifier.Tests/`: The unit tests for the library.
    * `Pulpifier/PulpifierCLI/`: CLI tool for invoking the library.
* `VisualPulps/`: A directory containing all of the website's CC0 visual novel contents
    * `VisualPulps/PrideAndPrejudice/`: The https://github.com/JohnQPulp/PrideAndPrejudice.git submodule (*Pride and Prejudice: The Visual Novel*).
    * `VisualPulps/AStudyInScarlet/`: The https://github.com/JohnQPulp/AStudyInScarlet.git submodule (*A Study in Scarlet: The Visual Novel*).
    * etc
* `CreativeCommonsContent/`: The https://github.com/JohnQPulp/CreativeCommonsContent.git submodule containing the website's CC0 blog contents.

### Contributing

While this project treats the texts of its public domain novel sources as immutable, the images and editing of the converted VNs have continuous room for improvement.

Improvements can take the form of background changes, sprite changes, line-break flow improvements, and so on. These can be either to address mistakes that can sometimes sneak into the visuals (e.g., [image/text inconsistencies](https://publicdomainpulp.com/blog/2026-03-09)) or to just enhance the visuals and/or reading experience.

All visual novel repos are open for issues and pull requests, in collaborative pursuit of making them all as visually pleasing and accurate as possible.

#### Documentation

Here are the links to the currently written posts in the book-to-VN conversion process series:

* [Converting Books Into Visual Novels Part 0: The pulp.txt Format](https://publicdomainpulp.com/blog/2026-02-13)
* [Converting Books Into Visual Novels Part 0.5: Creating book.txt](https://publicdomainpulp.com/blog/2026-02-14)
* [Converting Books Into Visual Novels Part 1: The First Edit — Creating the Starter pulp.txt](https://publicdomainpulp.com/blog/2026-03-20)
* [Converting Books Into Visual Novels Part 2±0.5: An Image Prompting and Processing Guide](https://publicdomainpulp.com/blog/2026-06-04)
* Converting Books Into Visual Novels Part 2: The Second Edit — Fully Populating pulp.txt
* Converting Books Into Visual Novels Part 3: Character Sprite Generation
* Converting Books Into Visual Novels Part 4: Background Generation
* Converting Books Into Visual Novels Part 5: The Third Edit — Visualizing pulp.txt

While the full pulpification process-and-tooling documentation isn't yet fully complete, there should be enough information here for contributing edits/improvements towards the existing VNs.

In particular, see the "pulp.txt Format" for learning about how to edit the VNs' metadata and the the "Image Prompting and Processing" post for how to generate new images in the correct (consistent) style.

