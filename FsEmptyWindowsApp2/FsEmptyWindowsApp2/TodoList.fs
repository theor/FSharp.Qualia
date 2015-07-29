module TodoList

open Defs
open DragDrop
open FsXaml
open System.Reactive.Linq
open System.Reactive.Subjects
open System.Windows
open System.Collections.ObjectModel
open System.Collections.Specialized
open System.Windows.Controls
open System.Windows.Media
open System

type FilteringType = All | Active | Completed
type TodoListEvents = 
    | Add
    | Delete of ItemModel
    | Checked of bool * ItemModel
    | CheckAll
    | UncheckAll
    | NewItemTextChanged of string
    | FilteringChanged of FilteringType
    | SelectionChanged of ItemModel option

and ItemModel(text : string, _done : bool) = 
    member val Text = Defs.Prop text
    member val Done = Defs.Prop _done

module Item = 
    type Control = XAML< "TodoItem.xaml", true >
    
    type View(m : ItemModel) as this = 
        inherit Defs.View<TodoListEvents, Control, ItemModel>(Control(), m)

        do
          let v = (this.Root)
          Observable.merge (v.MouseEnter --> Visibility.Visible) (v.MouseLeave --> Visibility.Collapsed)
          |>  Observable.add (fun vis -> v.buttonDelete.Visibility <- vis)
          
        
        override x.EventStreams = 
            [ x.Root.buttonDelete.Click --> Delete x.Model
              x.Root.checkDone.Unchecked --> Checked(false, x.Model)
              x.Root.checkDone.Checked --> Checked(true, x.Model) ]
        
        override x.SetBindings(m : ItemModel) = 
            m.Text |> Observable.add(fun t -> x.Root.labelText.Content <- t)
            m.Done |> Observable.add(fun d -> x.Root.checkDone.IsChecked <- System.Nullable d;
                                              x.Root.labelText.Foreground <- if d then Brushes.Gray else Brushes.Black)

type TodoListModel() = 
    let items = new ObservableCollection<ItemModel>()
    let collChanged = items.CollectionChanged |> Observable.filter Defs.isAddOrRemove
    
    let totalCount = 
        collChanged
        |> Observable.map (fun _ -> items.Count)
        |> Observable.toProperty 0
    
    let doneCount = 
        collChanged
        |> Observable.choose Defs.toAddOrRemove
        |> Observable.map (fun _ -> 
               items
               |> Seq.filter (fun i -> i.Done.Value)
               |> Seq.length)
        //                    |> Observable.scan (fun prev change -> prev) 0
        |> Observable.toProperty 0
    
    member x.Items : ObservableCollection<ItemModel> = items
    member x.TotalCount : ReactiveProperty<int> = totalCount
    member x.DoneCount : ReactiveProperty<int> = doneCount
    member val NewItemText : ReactiveProperty<string> = Prop("")
    member val FilteringType : ReactiveProperty<FilteringType> = Prop(All)
    member val SelectedItem : ReactiveProperty<ItemModel option> = Prop(None)


type TodoListWindow = XAML< "TodoList.xaml", true >
type TodoListView(mw : TodoListWindow, m) = 
    inherit DerivedCollectionSourceView.T<TodoListEvents, Window, TodoListModel>(mw.Root, m)

    let filter (m:TodoListModel) (x:Item.View) =
        match m.FilteringType.Value with
        | All -> true
        | Completed -> x.Model.Done.Value
        | Active -> not x.Model.Done.Value
    let listDragHandler = DragSourceHandler()
    let deleteDropHandler = DropTargetHandler()
    do
        mw.list |> setDragHandler listDragHandler
                |> setDefaultDropHandler
                |> ignore
        mw.deleteZone |> setDropHandler deleteDropHandler |> ignore
        listDragHandler.Events |> Observable.map (fun x ->
            match x with
            | StartDrag _ -> Visibility.Visible
            | _ -> Visibility.Collapsed)
            |> Observable.add (fun v -> mw.deleteZone.Visibility <- v)
        ()

    member val ItemsCollectionView:ComponentModel.ICollectionView = null with get,set

    override this.EventStreams = 
        [ // (un)check all
          mw.buttonCheckAll.Click --> CheckAll
          mw.buttonUncheckAll.Click --> UncheckAll
          // filtering
          mw.radioFilterAll.Checked --> FilteringChanged All
          mw.radioFilterActive.Checked --> FilteringChanged Active
          mw.radioFilterCompleted.Checked --> FilteringChanged Completed
          // on Enter keypress, create an item
          mw.tbNewItem.KeyDown
          |> Observable.filter (fun e -> e.Key = System.Windows.Input.Key.Enter)
          |> Observable.mapTo Add
          // new item's text
          Observable.Throttle(mw.tbNewItem.TextChanged, TimeSpan.FromMilliseconds 100.0)
          |> DispatcherObservable.ObserveOnDispatcher
          |> Observable.map (fun _ -> NewItemTextChanged mw.tbNewItem.Text)
          mw.list.SelectionChanged |> Observable.map (fun _ ->
            SelectionChanged (if mw.list.SelectedItem <> null then (Some (mw.list.SelectedItem :?> Item.View).Model) else None))
          
          deleteDropHandler.Events |> Observable.choose(fun x ->
              match x with
              | Drop i when (i.Data :? Item.View) -> Some (i.Data :?> Item.View)
              | _ -> None)
          |> Observable.map (fun x -> Delete x.Model)    ]

    
    override this.SetBindings(m : TodoListModel) = 
        // items list
        this.ItemsCollectionView <- m.Items |> this.linkCollection mw.list (Item.View)
        // filtering
        let p = (fun (x:obj) -> x :?> Item.View |> filter m)
        this.ItemsCollectionView.Filter <- Predicate<obj>(p)
        m.FilteringType |> Observable.add (fun f ->
            match f with
            | All -> mw.radioFilterAll.IsChecked <- Nullable true
            | _ -> ()
            this.ItemsCollectionView.Refresh())
        // counts
        Observable.merge m.DoneCount m.TotalCount 
        |> Observable.add (fun _ -> mw.labelSummary.Content <- sprintf "%i / %i" m.DoneCount.Value m.TotalCount.Value)
        // new item's text tb
        m.NewItemText |> Observable.add (fun x -> mw.tbNewItem.Text <- x)

        m.SelectedItem |> Observable.add (fun x ->
            mw.labeSelected.Content <- match x with
            | None -> "None"
            | Some x -> x.Text.Value)

type TodoListController() = 
    let add (m : TodoListModel) =
        m.Items.Add(ItemModel(m.NewItemText.Value, false))
        m.NewItemText.Value <- ""
    
    let delete (i : ItemModel) (m : TodoListModel) = 
        tracefn "DELETE %A" i.Text
        m.Items.Remove i |> ignore
    
    let check (v : bool) (i : ItemModel) (m : TodoListModel) = 
        tracefn "%sCHECKED %A" (if v then "" else "UN") i.Text
        i.Done.Value <- v
        m.DoneCount.Value <- m.DoneCount.Value + if v then 1 else -1
    
    let checkall (value : bool) (m : TodoListModel) =
        m.Items |> Seq.iter (fun i -> i.Done.Value <- value)

    interface IController<TodoListEvents, TodoListModel> with
        member this.InitModel m = 
            Seq.init 5 (fun i -> ItemModel((sprintf "Item %i" i), i % 2 = 0)) |> Seq.iter m.Items.Add

        member this.Dispatcher = 
            function 
            | Add -> add |> Sync
            | Delete item -> delete item |> Sync
            | Checked(v, item) -> check v item |> Sync
            | CheckAll -> checkall true |> Sync
            | UncheckAll -> checkall false |> Sync
            | NewItemTextChanged text -> Sync(fun m -> m.NewItemText.Value <- text)
            | FilteringChanged f -> Sync(fun m -> m.FilteringType.Value <- f)
            | SelectionChanged item -> Sync(fun m -> m.SelectedItem.Value <- item)

let run (app : Application) = 
    let v = TodoListView(TodoListWindow(), TodoListModel())
    let mvc = MVC(v, TodoListController())
    use eventloop = mvc.Start()
    app.Run(window = v.Root)
