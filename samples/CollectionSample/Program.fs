open System.Collections.ObjectModel
open System.Windows
open FsXaml
open FSharp.Qualia
open FSharp.Qualia.WPF
open System

type Events = Add

type ItemModel(s:string) =
    member val Text = ReactiveProperty(s)

type ItemView(m) =
    inherit View<Events, Controls.Label, ItemModel>(Controls.Label(), m)
     override x.EventStreams = []
     override x.SetBindings m =
        x.Root.Content <- m.Text.Value
//        m.Text |> Observable.add (fun _ -> x.Root.Content <- m.Text.Value)

type CollectionModel() =
    member val Items = new ObservableCollection<ItemModel>()
    
type CollectionWindow = XAML<"CollectionWindow.xaml",true>
type CollectionView(w:CollectionWindow,m) =
    inherit DerivedCollectionSourceView<Events, Window, CollectionModel>(w.Root, m)
    
    member val ItemsCollectionView:ComponentModel.ICollectionView = null with get,set

    override x.EventStreams = [
        w.buttonAdd.Click |> Observable.map traceid |> Observable.map (fun _ -> Add) ]
    override x.SetBindings m =
        x.ItemsCollectionView <- x.linkCollection w.listbox (fun im -> ItemView(im)) m.Items


type CollectionController() =
    interface IDispatcher<Events,CollectionModel> with
        member this.InitModel m = ()
        member this.Dispatcher = 
            function
            | Add -> Sync (fun m -> m.Items.Add (ItemModel(sprintf "Item #%i" m.Items.Count)))





[<EntryPoint>]
[<STAThread>]
let main argv = 
    let app = if Application.Current = null then Application() else Application.Current
    let w = new  CollectionWindow()
    let lm = CollectionModel()
    let v = CollectionView(w,lm)
    let c = CollectionController()
    let loop = EventLoop(v, c)
    use l = loop.Start()
    app.Run(w.Root)
