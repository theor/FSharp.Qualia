module TodoItem

open System.Windows
open FsXaml
open FSharp.Qualia
open FSharp.Qualia.Core
open Types
type Control = XAML< "TodoItem.xaml", true >
    
type View(m : TodoItemModel) as this = 
    inherit View<TodoListEvents, Control, TodoItemModel>(Control(), m)

    do
        let v = (this.Root)
        Observable.merge (v.MouseEnter --> Visibility.Visible) (v.MouseLeave --> Visibility.Collapsed)
        |>  Observable.add (fun vis -> v.buttonDelete.Visibility <- vis)
          
    override x.EventStreams = 
        [ x.Root.buttonDelete.Click --> Delete x.Model
          x.Root.checkDone.Unchecked --> Checked(false, x.Model)
          x.Root.checkDone.Checked --> Checked(true, x.Model) ]
        
    override x.SetBindings(m : TodoItemModel) = 
        m.Text |> Observable.add(fun t -> x.Root.labelText.Content <- t)
        m.Done |> Observable.add(fun d -> x.Root.checkDone.IsChecked <- System.Nullable d;
                                            x.Root.labelText.Foreground <- if d then Media.Brushes.Gray else Media.Brushes.Black)

let createView x = new View(x)