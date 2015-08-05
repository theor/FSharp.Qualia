﻿namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("FSharp.Qualia")>]
[<assembly: AssemblyProductAttribute("FSharp.Qualia")>]
[<assembly: AssemblyDescriptionAttribute("Event stream/rx based UI Framework and architecture")>]
[<assembly: AssemblyVersionAttribute("0.1")>]
[<assembly: AssemblyFileVersionAttribute("0.1")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.1"
