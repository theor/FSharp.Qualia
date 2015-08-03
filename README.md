[![Issue Stats](http://issuestats.com/github/theor/FSharp.Qualia/badge/issue)](http://issuestats.com/github/theor/FSharp.Qualia)
[![Issue Stats](http://issuestats.com/github/theor/FSharp.Qualia/badge/pr)](http://issuestats.com/github/theor/FSharp.Qualia)
[![Build status](https://ci.appveyor.com/api/projects/status/ir8thdptylo1w4w2?svg=true)](https://ci.appveyor.com/project/theor/fsharp-qualia)
# FSharp.Qualia

[http://theor.github.io/FSharp.Qualia/](http://theor.github.io/FSharp.Qualia/)

Qualia is a MVC-ish UI framework heavily inspired by React+Flux and FSharp.Desktop.UI The goal is to provide a decoupled architecture using Rx and idiomatic F#, based on an unidirectional event loop.

This project started as UI architecture research. FSharp.Desktop.UI seemed like a great solution, but still relied on WPF bindings and had no facility to handle collections. Still, it would not exist without FSharp.Desktop.UI - a big THANK YOU to @dmitry-a-morozov and @forki.

Qualia is made with [Project Scaffold](https://github.com/fsprojects/ProjectScaffold), using [Paket](https://github.com/fsprojects/Paket) to manage dependencies, [FAKE](http://fsharp.github.io/FAKE/) to build and [FSharp.Formatting](http://tpetricek.github.io/FSharp.Formatting/) to generate documentation.

In order to to build the project, run

    $ build.cmd // on windows    
    $ build.sh  // on mono
    
Read the [Getting started tutorial](http://theor.github.io/FSharp.Qualia/) to learn more.
