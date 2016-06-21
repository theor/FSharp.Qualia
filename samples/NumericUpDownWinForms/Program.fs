module NumericUpDownWinForms

open System
open System.Windows.Forms
open FSharp.Qualia
open Chessie.ErrorHandling

type MainModel() = 
    member val Value = new ReactiveProperty<int>(0)

type MainEvents = 
    | Up
    | Down
    | Edit of string
    
type MainForm() =
    inherit Form()

    let p = new TableLayoutPanel()
    let addAt c i j =
        p.Controls.Add c
        p.SetCellPosition(c, TableLayoutPanelCellPosition(i,j))
        
    member val ButtonUp = new Button(Text="+")
    member val ButtonDown = new Button(Text="-")
    member val TextBox = new TextBox()
    member val Label = new Label()

    override x.OnLoad(e) =
        x.Controls.Add(p)
        addAt x.ButtonUp 1 0
        addAt x.ButtonDown 1 1
        addAt x.TextBox 0 0
        addAt x.Label 0 1

type MainView(mw : MainForm, m) = 
    inherit View<MainEvents, Form, MainModel>(mw, m)
    
    override this.EventStreams = 
        [ mw.ButtonUp.Click |> Observable.mapTo Up
          mw.ButtonDown.Click |> Observable.mapTo Down
          mw.TextBox.TextChanged |> Observable.map (fun e -> Edit mw.TextBox.Text) ]
    
    override this.SetBindings(m : MainModel) = 
        m.Value.Add(fun v -> mw.Label.Text <- (string v))
        m.Value.Add(fun v -> mw.TextBox.Text <- (string v))

type MainDispatcher() = 
    let up (m : MainModel) = ok (m.Value.Value <- m.Value.Value + 1)
    let down (m : MainModel) = ok (m.Value.Value <- m.Value.Value - 1)
    let edit str (m : MainModel) = 
        match Int32.TryParse str with
        | true, i -> ok (m.Value.Value <- i)
        | false, _ -> ok (())
    
    interface IDispatcher<MainEvents, MainModel> with
        member this.InitModel _ = ()
        member this.Dispatcher = 
            function 
            | Up -> Sync up
            | Down -> Sync down
            | Edit str -> Sync(edit str)

let run() =
    let v = MainView(new MainForm(), MainModel())
    let mvc = EventLoop(v, MainDispatcher())
    use eventloop = mvc.Start()
    v.Root.ShowDialog() |> ignore

[<STAThread>]
[<EntryPoint>]
let main argv = 
    run()
    0
