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
WPF Collections tutorial
========================

The resulting app will contain a listbox, an 'Add Item' button, and label displaying the current selection and will delete the selected item when pressing the Delete key.

First, a small helper used for the ListBox.SelectedItem:

*)

let cast<'a> (x:obj) : 'a option =
    match x with
    | :? 'a as y -> Some y
    | _ -> None

(**
The app event type - adding, removing and selecting an item
*)

type Events = Add | Remove | SelectionChanged of ItemModel option
(**
ItemModel itself is defined here, containing a string property. As it won't change, this is a plain property, not a ReactiveProperty<string>.

We also override ToString() to avoid defining a template for the sake of the tutorial.
*)
and ItemModel(s) =
    member val Text = s
    override x.ToString() = x.Text
(**
The concrete WPF window type - this should be replaced by a .xaml loaded by FsXaml in the real world.
*)
type ListViewWindow() as x =
    inherit Window()
    let label = Label()
    let button = Button(Content="Add Item")
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

(**
Item Qualia view: no template here, just a Label. That's why we overrode ToString().
In SetBindings, we just set once the label content - this is equivalent to a binding mode Once.
*)
type ItemView(m) =
    inherit View<Events, Label, ItemModel>(Label(), m)
     override x.EventStreams = []
     override x.SetBindings m = x.Root.Content <- m.Text
(**
List model : the items collection and a reactive property containing the selected item. As it can be null, this is an ItemModel option.
*)
type ListModel() =
    member val Items = new ObservableCollection<ItemModel>()
    member val SelectedItem = new ReactiveProperty<ItemModel option>(None)
(**
The view inherits DerivedCollectionSourceView, but this is a convenience class providing one helper method at the moment, *linkCollection*. You could just do the plumbing by hand.
*)   
type ListView(elt, m) =
    inherit DerivedCollectionSourceView<Events, ListViewWindow, ListModel>(elt, m)

    override x.EventStreams = [
        (** Add an item when clicking the Add button *)
        elt.Button.Click --> Add
        (** This one just fetch the selected item, tries to cast it to ItemView, then select the view's Model as an option. *)
        elt.List.SelectionChanged |> Observable.map (fun _ -> SelectionChanged((cast<ItemView> elt.List.SelectedItem |> Option.map(fun v -> v.Model))))
        (** Send a remove event, only when <Del> is pressed *)
        elt.List.KeyDown |> Observable.filter (fun (e:Input.KeyEventArgs) -> e.Key = Input.Key.Delete) |> Observable.mapTo Remove ]
    override x.SetBindings m =
        (** That's all the collection plumbing: which WPF list, how to create a view for each item model, and which model collection to monitor.
            We could use the returned CollectionView to do some filtering/grouping/sorting/... *)
        let collview = x.linkCollection elt.List (fun i -> ItemView(i)) m.Items
        m.SelectedItem |> Observable.add (fun i -> elt.Label.Content <- sprintf "Press <DEL> to delete the selection item. Current Selection: %A" i)
        ()
(**
Typical dispatcher - 
*)
type ListController() =
    interface IDispatcher<Events,ListModel> with
        member this.InitModel m = ()
        member this.Dispatcher = 
            function
//            | Add -> Async (fun m -> async { do m.Items.Add (ItemModel(sprintf "#%i" m.Items.Count)) })
            | Add ->
                let a = (fun (m:ListModel) -> async {
                    do! Async.Sleep 1000
                    do m.Items.Add (ItemModel(sprintf "#%i" m.Items.Count))
                })
                Async a
//                Sync (Async.Start a)
            | Remove -> Sync (fun m -> m.SelectedItem.Value |> Option.iter (m.Items.Remove >> ignore))
            | SelectionChanged item -> printfn "%A" item; Sync (fun m -> m.SelectedItem.Value <- item)
(**
*)
let app = Application()
let lm = ListModel()
let v = ListView(new ListViewWindow(),lm)
let c = ListController()
let loop = EventLoop(v, c)
loop.Start()
app.Run(v.Root)
app.Shutdown()

