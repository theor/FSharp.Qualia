module main

open Defs
open System
open System.Threading
open System.Windows
open System.Windows.Threading
open NumericUpDown

[<STAThread>]
[<EntryPoint>]
let main argv = 
    let app = Application()
    let context = new DispatcherSynchronizationContext(Application.Current.Dispatcher)
    SynchronizationContext.SetSynchronizationContext(context)
//    let v = MainView(MainWindow())
//    let mvc = MVC(MainModel(), v, MainController())
//    use eventloop = mvc.Start()
//    app.Run(window = v.Root)
    TodoList.run app
