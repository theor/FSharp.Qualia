module TodoListApp

open FSharp.Qualia
open System.Windows
open System
open TodoList

[<STAThread>]
[<EntryPoint>]
let main argv = 
    let v = TodoListView(TodoListWindow(), Model())
    use eventloop = EventLoop(v, Dispatcher.T()).Start()
    Application().Run(window = v.Root)
