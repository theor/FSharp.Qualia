module TodoList

open Defs
open FsXaml
open Reactive.Bindings
open System.Reactive.Linq
open System.Reactive.Subjects
open Reactive.Bindings.Extensions
open System.Windows
open System.Collections.ObjectModel
open System.Collections.Specialized
open System.Windows.Controls

type TodoListEvents = 
    | Add
    | Delete of ItemModel
    | Checked of bool * ItemModel

and ItemModel(text : string, _done : bool) = 
    member val Text = new ReactiveProperty<string>(text)
    member val Done = new ReactiveProperty<bool>(_done)
    override x.ToString() = x.Text.Value

module Item = 
    type Control = XAML< "TodoItem.xaml", true >
    
    type View(m : ItemModel) = 
        inherit Defs.View<TodoListEvents, Control, ItemModel>(Control(), m)
        override this.EventStreams =
            [ this.Root.buttonDelete.Click |> Observable.mapTo (Delete this.Model)
              this.Root.checkDone.Checked |> Observable.mapTo (Checked(this.Root.checkDone.IsChecked.GetValueOrDefault(), this.Model))]
        override this.SetBindings(m : ItemModel) = m.Text.Add(fun t -> this.Root.labelText.Content <- t)

type TodoListModel() = 
    member val Items = new ObservableCollection<ItemModel>()

type TodoListWindow = XAML< "TodoList.xaml", true >

type TodoListView(mw : TodoListWindow, m) as this = 
    inherit View<TodoListEvents, Window, TodoListModel>(mw.Root, m)
    let link (items:ItemsControl) (e:NotifyCollectionChangedEventArgs) =
        let it = mw.Root.FindResource "ViewTemplate"
        items.ItemTemplate <- it :?> DataTemplate
        let add (x:ItemModel) =
            let ctrl = this.ComposeView (Item.View(x))
            items.Items.Add (ctrl) |> ignore
            
        let remove (x:ItemModel) =
            let iv = items.Items |> Seq.cast<Item.View> |> Seq.tryFindIndex (fun i -> i.Model = x)
            match iv with
            | Some index -> items.Items.RemoveAt index
            | None -> ()
        match e.Action with
        | NotifyCollectionChangedAction.Add x -> e.NewItems |> Seq.cast<ItemModel> |> Seq.iter add
        | NotifyCollectionChangedAction.Remove -> e.OldItems |> Seq.cast<ItemModel> |> Seq.iter remove
        | _ -> ()
    override this.EventStreams = [ mw.buttonAdd.Click |> Observable.mapTo Add ]
    override this.SetBindings(m : TodoListModel) = 
        m.Items.CollectionChanged.Add (link mw.list)

type TodoListController() = 
    let add (m : TodoListModel) = m.Items.Add(ItemModel(sprintf "Item %i" m.Items.Count, false))
    
    let delete (i : ItemModel) (m : TodoListModel) = 
        tracefn "DELETE %A" i.Text
        m.Items.Remove i |> ignore

    let check (v:bool) (i : ItemModel) (m : TodoListModel) = 
        tracefn "CHECKED %A" i.Text
    
    interface IController<TodoListEvents, TodoListModel> with
        member this.InitModel m =
            Seq.init 5 (fun i -> ItemModel((sprintf "Item %i" i), i % 2 = 0))
            |> Seq.iter m.Items.Add
        member this.Dispatcher = 
            function 
            | Add -> Sync add
            | Delete item -> Sync(delete item)
            | Checked(v, item) -> Sync(check v item)

let run (app : Application) = 
    let v = TodoListView(TodoListWindow(), TodoListModel())
    let mvc = MVC(v, TodoListController())
    use eventloop = mvc.Start()
    app.Run(window = v.Root)
