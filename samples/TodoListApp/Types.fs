module Types
open FSharp.Qualia

type FilteringType = All | Active | Completed

type TodoListEvents = 
    | Add
    | Delete of TodoItemModel
    | Checked of bool * TodoItemModel
    | CheckAll
    | UncheckAll
    | NewItemTextChanged of string
    | FilteringChanged of FilteringType
    | SelectionChanged of TodoItemModel option

and TodoItemModel(text : string, _done : bool) = 
    member val Text = ReactiveProperty text
    member val Done = ReactiveProperty _done