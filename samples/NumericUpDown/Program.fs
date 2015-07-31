module NumericUpDown

open System
open System.Windows
open System.Windows.Threading
open System.Threading
open FSharp.Qualia
open FsXaml

type MainModel() = 
    member val Value = new ReactiveProperty<int>(0)

type MainEvents = 
    | Up
    | Down
    | Edit of string

type MainWindow = XAML<"MainWindow.xaml", true>   

type MainView(mw : MainWindow, m) = 
    inherit View<MainEvents, Window, MainModel>(mw.Root, m)
    
    override this.EventStreams = 
        [ mw.buttonUp.Click |> Observable.mapTo Up
          mw.buttonDown.Click |> Observable.mapTo Down
          mw.textBox.TextChanged |> Observable.map (fun e -> Edit mw.textBox.Text) ]
    
    override this.SetBindings(m : MainModel) = 
        m.Value.Add(fun v -> mw.label.Content <- v)
        m.Value.Add(fun v -> mw.textBox.Text <- (string v))

type MainDispatcher() = 
    let up (m : MainModel) = m.Value.Value <- m.Value.Value + 1
    let down (m : MainModel) = m.Value.Value <- m.Value.Value - 1
    let edit str (m : MainModel) = 
        match Int32.TryParse str with
        | true, i -> m.Value.Value <- i
        | false, _ -> ()
    
    interface IDispatcher<MainEvents, MainModel> with
        member this.InitModel _ = ()
        member this.Dispatcher = 
            function 
            | Up -> Sync up
            | Down -> Sync down
            | Edit str -> Sync(edit str)

let run(app:Application) =
    let v = MainView(MainWindow(), MainModel())
    let mvc = EventLoop(v, MainDispatcher())
    use eventloop = mvc.Start()
    app.Run(window = v.Root)

[<STAThread>]
[<EntryPoint>]
let main argv = 
    let app = Application()
    run app
