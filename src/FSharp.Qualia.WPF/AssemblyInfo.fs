namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("FSharp.Qualia")>]
[<assembly: AssemblyProductAttribute("FSharp.Qualia")>]
[<assembly: AssemblyDescriptionAttribute("Event stream/rx based UI Framework and architecture")>]
[<assembly: AssemblyVersionAttribute("1.0")>]
[<assembly: AssemblyFileVersionAttribute("1.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "1.0"
