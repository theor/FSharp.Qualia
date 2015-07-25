module TodoList

open Defs
open FsXaml
open System.Reactive.Linq
open System.Reactive.Subjects
open System.Windows
open System.Collections.ObjectModel
open System.Collections.Specialized
open System.Windows.Controls
open System

type TodoListEvents = 
    | Add
    | Delete of ItemModel
    | Checked of bool * ItemModel
    | CheckAll
    | UncheckAll

and ItemModel(text : string, _done : bool) = 
    member val Text = Defs.Prop text
    member val Done = Defs.Prop _done

module Item = 
    type Control = XAML< "TodoItem.xaml", true >
    
    type View(m : ItemModel) = 
        inherit Defs.View<TodoListEvents, Control, ItemModel>(Control(), m)
        
        override x.EventStreams = 
            [ x.Root.buttonDelete.Click --> Delete x.Model
              x.Root.checkDone.Unchecked --> Checked(false, x.Model)
              x.Root.checkDone.Checked --> Checked(true, x.Model) ]
        
        override x.SetBindings(m : ItemModel) = 
            m.Text.Add(fun t -> x.Root.labelText.Content <- t)
            m.Done.Add(fun d -> x.Root.checkDone.IsChecked <- System.Nullable d)

type TodoListModel() = 
    let items = new ObservableCollection<ItemModel>()
    let collChanged = items.CollectionChanged
                      |> Observable.filter Defs.isAddOrRemove
    let totalCount = collChanged
                     |> Observable.map (fun _ -> items.Count)
                     |> Observable.toProperty 0
    let doneCount = collChanged
                    |> Observable.choose Defs.toAddOrRemove
                    |> Observable.map (fun _ -> items |> Seq.filter (fun i -> i.Done.Value) |> Seq.length)
//                    |> Observable.scan (fun prev change -> prev) 0
                    |> Observable.toProperty 0

    member x.Items = items
    member x.TotalCount:ReactiveProperty<int> = totalCount
    member x.DoneCount:ReactiveProperty<int> = doneCount
        

type TodoListWindow = XAML< "TodoList.xaml", true >

type TodoListView(mw : TodoListWindow, m) = 
    inherit CollectionView<TodoListEvents, Window, TodoListModel>(mw.Root, m)
    
    override this.EventStreams = 
        [ mw.buttonAdd.Click |> Observable.mapTo Add
          mw.buttonCheckAll.Click |> Observable.mapTo CheckAll
          mw.buttonUncheckAll.Click |> Observable.mapTo UncheckAll ]
    
    override this.SetBindings(m : TodoListModel) = 
        m.Items |> this.linkCollection mw.list (fun x -> Item.View(x))
        Observable.merge m.DoneCount m.TotalCount 
        //m.TotalCount 
        |> Observable.add (fun _ -> mw.labelSummary.Content <- sprintf "%i / %i" m.DoneCount.Value m.TotalCount.Value)

type TodoListController() = 
    
    let add (m : TodoListModel) = 
        m.Items.Add(ItemModel(sprintf "Item %i" m.Items.Count, false))
    
    let delete (i : ItemModel) (m : TodoListModel) = 
        tracefn "DELETE %A" i.Text
        m.Items.Remove i |> ignore
    
    let check (v : bool) (i : ItemModel) (m : TodoListModel) = 
        tracefn "%sCHECKED %A" (if v then ""
                                else "UN") i.Text
        m.DoneCount.Value <- if v then m.DoneCount.Value + 1
                             else m.DoneCount.Value - 1
    
    let checkall (value : bool) (m : TodoListModel) =
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
//    let p = ReactiveProperty(42)
//    p.Add (fun i -> tracefn "%i" i)
//    p.Value <- 7
    let v = TodoListView(TodoListWindow(), TodoListModel())
    let mvc = MVC(v, TodoListController())
    use eventloop = mvc.Start()
    app.Run(window = v.Root)
