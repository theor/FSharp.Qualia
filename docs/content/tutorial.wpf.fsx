(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin"
#I "../../packages"
#r "System.Core.dll"
#r "System.dll"
#r "System.Drawing.dll"
#r "WindowsBase.dll"
#r "PresentationCore.dll"
#r "PresentationFramework.dll"
#r "System.Numerics.dll"
#r "Rx-Core/lib/net45/System.Reactive.Core.dll"
#r "Rx-Interfaces/lib/net45/System.Reactive.Interfaces.dll"
#r "Rx-Linq/lib/net45/System.Reactive.Linq.dll"
#r "Rx-PlatformServices/lib/net45/System.Reactive.PlatformServices.dll"
#r "FSharp.Qualia/FSharp.Qualia.dll"
#r "FSharp.Qualia.WPF/FSharp.Qualia.WPF.dll"

open System.Windows
open FSharp.Qualia
open System.Collections.ObjectModel

(**
Collections
===================
*)

type Events = Add

type ItemModel() =
    member val Text = ReactiveProperty("")

type ItemView(elt, m) =
    inherit View<Events, FrameworkElement, ItemModel>(elt, m)
     override x.EventStreams = []
     override x.SetBindings m = ()
type ListModel() =
    member val Items = new ObservableCollection<ItemModel>()
    
type ListView(elt, m) =
    inherit View<Events, Window, ListModel>(elt, m)
    override x.EventStreams = []
    override x.SetBindings m = ()
type ListController() =
    interface IDispatcher<Events,ListModel> with
        member this.InitModel m = ()
        member this.Dispatcher = 
            function
            | Add -> Sync (fun m -> ())

let w = new Window()
let app = if Application.Current = null then Application() else Application.Current
let lm = ListModel()
let v = ListView(Window(),lm)
let c = ListController()
let loop = EventLoop(v, c)
loop.Start()
app.Run(w)

