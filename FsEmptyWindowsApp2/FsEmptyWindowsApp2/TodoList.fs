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
    | CheckAll
    | UncheckAll

and ItemModel(text : string, _done : bool) = 
    member val Text = new ReactiveProperty<string>(text)
    member val Done = new ReactiveProperty<bool>(_done, ReactivePropertyMode.RaiseLatestValueOnSubscribe)

    override x.ToString() = x.Text.Value

module Item = 
    type Control = XAML< "TodoItem.xaml", true >
    
    type View(m : ItemModel) = 
        inherit Defs.View<TodoListEvents, Control, ItemModel>(Control(), m)
        override this.EventStreams =
            [ this.Root.buttonDelete.Click |> Observable.mapTo (Delete this.Model)
              this.Root.checkDone.Checked |> Observable.mapTo (Checked(this.Root.checkDone.IsChecked.GetValueOrDefault(), this.Model))]

        override this.SetBindings(m : ItemModel) =
            m.Text.Add(fun t -> this.Root.labelText.Content <- t)
            m.Done.Add(fun d -> this.Root.checkDone.IsChecked <- System.Nullable d)
      

type TodoListModel() = 
    member val Items = new ObservableCollection<ItemModel>()
    member val TotalCount = new ReactiveProperty<int>()
    member val DoneCount = new ReactiveProperty<int>()

type TodoListWindow = XAML< "TodoList.xaml", true >

type TodoListView(mw : TodoListWindow, m) as this = 
    inherit CollectionView<TodoListEvents, Window, TodoListModel>(mw.Root, m)
    override this.EventStreams =
        [ mw.buttonAdd.Click |> Observable.mapTo Add
          mw.buttonCheckAll.Click |> Observable.mapTo CheckAll
          mw.buttonUncheckAll.Click |> Observable.mapTo UncheckAll ]

    override this.SetBindings(m : TodoListModel) = 
        m.Items |> this.linkCollection mw.list (fun x -> Item.View(x))
        Observable.merge m.DoneCount m.TotalCount
            |> Observable.add (fun _ -> mw.labelSummary.Content <- sprintf "%i / %i" m.DoneCount.Value m.TotalCount.Value)
type TodoListController() = 
    let add (m : TodoListModel) =
        m.Items.Add(ItemModel(sprintf "Item %i" m.Items.Count, false))
        m.TotalCount.Value <- m.TotalCount.Value + 1
    let delete (i : ItemModel) (m : TodoListModel) = 
        tracefn "DELETE %A" i.Text
        m.Items.Remove i |> ignore
        m.TotalCount.Value <- m.TotalCount.Value - 1

    let check (v:bool) (i : ItemModel) (m : TodoListModel) = 
        tracefn "CHECKED %A" i.Text
        m.DoneCount.Value <- if v then m.DoneCount.Value + 1 else m.DoneCount.Value - 1
    let checkall (value:bool) (m : TodoListModel) =
        m.Items |> Seq.iter (fun i -> i.Done.Value <- value)
    
    interface IController<TodoListEvents, TodoListModel> with
        member this.InitModel m =
            Seq.init 5 (fun i -> ItemModel((sprintf "Item %i" i), i % 2 = 0))
            |> Seq.iter m.Items.Add
        member this.Dispatcher = 
            function 
            | Add -> add |> Sync
            | Delete item -> delete item |> Sync
            | Checked(v, item) -> check v item |> Sync
            | CheckAll -> checkall true |> Sync
            | UncheckAll -> checkall false |> Sync

let run (app : Application) = 
    let v = TodoListView(TodoListWindow(), TodoListModel())
    let mvc = MVC(v, TodoListController())
    use eventloop = mvc.Start()
    app.Run(window = v.Root)
