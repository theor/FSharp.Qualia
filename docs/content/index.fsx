(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin"
#I "../../packages"
#r "System.Core.dll"
#r "System.dll"
#r "System.Drawing.dll"
#r "System.Numerics.dll"
#r "System.Windows.Forms.dll"
#r "Rx-Core/lib/net45/System.Reactive.Core.dll"
#r "Rx-Interfaces/lib/net45/System.Reactive.Interfaces.dll"
#r "Rx-Linq/lib/net45/System.Reactive.Linq.dll"
#r "Rx-PlatformServices/lib/net45/System.Reactive.PlatformServices.dll"
#r "FSharp.Qualia/FSharp.Qualia.dll"
#r "FSharp.Qualia.WPF/FSharp.Qualia.WPF.dll"

(**
FSharp.Qualia
======================

Qualia is a MVC-ish UI framework heavily inspired by [React+Flux](https://facebook.github.io/flux/) and [FSharp.Desktop.UI](http://fsprojects.github.io/FSharp.Desktop.UI/)
The goal is to provide a decoupled architecture using [Rx](http://rx.codeplex.com/) and idiomatic F#, based on an unidirectional event loop.

This project started as UI architecture research. FSharp.Desktop.UI seemed like a great solution, but still relied on WPF bindings and had no facility to handle collections. Still, it would not exist without FSharp.Desktop.UI - a big THANK YOU to [@dmitry-a-morozov](https://github.com/dmitry-a-morozov) and [@forki](https://github.com/forki).

Architecture overview
---------------------

![Qualia Architecture Overview](img/qualia_archi.png)

<div class="row">
  <div class="span1"></div>
  <div class="span6">
    <div class="well well-small" id="nuget">
      The FSharp.Qualia library can be <a href="https://nuget.org/packages/FSharp.Qualia">installed from NuGet</a>:
      <pre>PM> Install-Package FSharp.Qualia</pre>
    </div>
  </div>
  <div class="span1"></div>
</div>

*)

(**
Some more info

Samples & documentation
-----------------------

The library comes with comprehensible documentation. 
It can include tutorials automatically generated from `*.fsx` files in [the content folder][content]. 
The API reference is automatically generated from Markdown comments in the library implementation.

 * [Tutorial](tutorial.html) contains a further explanation of this sample library.

 * [API Reference](reference/index.html) contains automatically generated documentation for all types, modules
   and functions in the library. This includes additional brief samples on using most of the
   functions.
 
Contributing and copyright
--------------------------

The project is hosted on [GitHub][gh] where you can [report issues][issues], fork 
the project and submit pull requests. If you're adding a new public API, please also 
consider adding [samples][content] that can be turned into a documentation. You might
also want to read the [library design notes][readme] to understand how it works.

The library is available under Public Domain license, which allows modification and 
redistribution for both commercial and non-commercial purposes. For more information see the 
[License file][license] in the GitHub repository. 

  [content]: https://github.com/fsprojects/FSharp.Qualia/tree/master/docs/content
  [gh]: https://github.com/fsprojects/FSharp.Qualia
  [issues]: https://github.com/fsprojects/FSharp.Qualia/issues
  [readme]: https://github.com/fsprojects/FSharp.Qualia/blob/master/README.md
  [license]: https://github.com/fsprojects/FSharp.Qualia/blob/master/LICENSE.txt
*)
