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
#r "System.Xaml.dll"
#r "UIAutomationTypes.dll"
#r "Rx-Core/lib/net45/System.Reactive.Core.dll"
#r "Rx-Interfaces/lib/net45/System.Reactive.Interfaces.dll"
#r "Rx-Linq/lib/net45/System.Reactive.Linq.dll"
#r "Rx-PlatformServices/lib/net45/System.Reactive.PlatformServices.dll"
#r "FSharp.Qualia/FSharp.Qualia.dll"
#r "FSharp.Qualia.WPF/FSharp.Qualia.WPF.dll"

open System.Windows
open FSharp.Qualia
open System.Collections.ObjectModel
open System.Windows.Controls
open FSharp.Qualia.WPF

(**
Collections
===================
*)

let cast<'a> (x:obj) : 'a option =
    match x with
    | :? 'a as y -> Some y
    | _ -> None

type Events = Add | Remove | SelectionChanged of ItemModel option

and ItemModel(s) =
    member val Text = ReactiveProperty(s)
    override x.ToString() = x.Text.Value

type ListViewWindow() as x =
    inherit Window()
    let label = Label()
    let button = Button(Content="Add")
    let list = ListBox()
    do
        let sp = StackPanel()
        sp.Children.Add button |> ignore
        sp.Children.Add label |> ignore
        sp.Children.Add list |> ignore
        x.Content <- sp
    member val Label = label
    member val Button = button
    member val List = list


type ItemView(m) =
    inherit View<Events, Label, ItemModel>(Label(), m)
     override x.EventStreams = []
     override x.SetBindings m = x.Root.Content <- m.Text.Value

type ListModel() =
    member val Items = new ObservableCollection<ItemModel>()
    member val SelectedItem = new ReactiveProperty<ItemModel option>(None)
    
type ListView(elt, m) =
    inherit DerivedCollectionSourceView<Events, ListViewWindow, ListModel>(elt, m)

    override x.EventStreams = [
        elt.Button.Click --> Add
        elt.List.SelectionChanged |> Observable.map (fun _ -> SelectionChanged((cast<ItemView> elt.List.SelectedItem |> Option.map(fun v -> v.Model))))]
    override x.SetBindings m =
        let collview = x.linkCollection elt.List (fun i -> ItemView(i)) m.Items
        m.SelectedItem |> Observable.add (fun i -> elt.Label.Content <- sprintf "Selection: %A" i)
        ()
type ListController() =
    interface IDispatcher<Events,ListModel> with
        member this.InitModel m = ()
        member this.Dispatcher = 
            function
            | Add -> Sync (fun m -> m.Items.Add (ItemModel(sprintf "#%i" m.Items.Count)))
            | Remove -> failwith "Not implemented yet"
            | SelectionChanged item -> printfn "%A" item; Sync (fun m -> m.SelectedItem.Value <- item)

let app = if Application.Current = null then Application() else Application.Current
let lm = ListModel()
let v = ListView(new ListViewWindow(),lm)
let c = ListController()
let loop = EventLoop(v, c)
loop.Start()
app.Run(v.Root)
app.Shutdown()

