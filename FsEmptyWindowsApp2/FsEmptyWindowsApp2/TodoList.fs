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

and ItemModel(text : string) = 
    member val Text = new ReactiveProperty<string>(text)
    override x.ToString() = x.Text.Value

module Item = 
    type Control = XAML< "TodoItem.xaml", true >
    
    type View(m : ItemModel) = 
        inherit Defs.View<TodoListEvents, Control, ItemModel>(Control(), m)
        override this.EventStreams = [ this.Root.buttonDelete.Click |> Observable.mapTo (Delete this.Model) ]
        override this.SetBindings(m : ItemModel) = m.Text.Add(fun t -> this.Root.labelText.Content <- t)

type TodoListModel() = 
    member val Items = new ObservableCollection<ItemModel>()

type TodoListWindow = XAML< "TodoList.xaml", true >

type TodoListView(mw : TodoListWindow, m) as this = 
    inherit View<TodoListEvents, Window, TodoListModel>(mw.Root, m)
    let link (items:ItemCollection) (e:NotifyCollectionChangedEventArgs) =
        let add (x:ItemModel) =
            let ctrl = this.ComposeView (Item.View(x))
            items.Add (ctrl.Root) |> ignore
        match e.Action with
        | NotifyCollectionChangedAction.Add x -> e.NewItems |> Seq.cast<ItemModel> |> Seq.iter add
        | _ -> ()
    override this.EventStreams = [ mw.buttonAdd.Click |> Observable.mapTo Add ]
    override this.SetBindings(m : TodoListModel) = 
        m.Items.CollectionChanged.Add (link mw.list.Items)

type TodoListController() = 
    let add (m : TodoListModel) = m.Items.Add(ItemModel(sprintf "Item %i" m.Items.Count))
    
    let delete (i : ItemModel) (m : TodoListModel) = 
        tracefn "DELETE %A" i.Text
        m.Items.Remove i |> ignore
    
    interface IController<TodoListEvents, TodoListModel> with
        member this.InitModel _ = ()
        member this.Dispatcher = 
            function 
            | Add -> Sync add
            | Delete i -> Sync(delete i)

let run (app : Application) = 
    let v = TodoListView(TodoListWindow(), TodoListModel())
    let mvc = MVC(v, TodoListController())
    use eventloop = mvc.Start()
    app.Run(window = v.Root)
