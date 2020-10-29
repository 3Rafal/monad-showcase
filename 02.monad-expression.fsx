#r "FSharpPlus.dll"
open FSharpPlus

// Jak pracować na monadach?
// Jak pracować z bardziej skomplikowanymi przypadkami?
// Co zrobić jak mamy monadę w monadzie?

// Wygodnym rozwiązaniem jest użycie computation expression
// To wygodna składnia która upraszcza operacje na monadach

////// Prosty przykład na listach i funkcji
let l1 = [1;2;3]
let l2 = [4;5;6]
let listFun  = bind (fun x -> [x;x;x])

// Możemy użyć operatora
let lConcat = l1 @ listFun l2

// List computation expression z FSharp.Core
// yield to znany koncept z C# (iterator block) i z pythona (generatory)
let lConcat' = 
    [ yield! l1
      yield! listFun l2
      yield 42 ] 

// albo generycznego monad expression z F#+ (działa też na seq i aray)
// monad.plus oznacza computation expression który może mieć wiele return'ów
// `Additive monad`
let lConcat'' = 
    monad.plus {
        return! l1
        return! listFun l2
        return 42 }




/////// Drugim rodzajem jest monad expression
// Używamy go dla monad które mają efekty
// Przypomina async expression FSharp.Core, ale jest generyczny
let async1 = async { return 1 }
let async2 = async { return 2 }

let aResult = 
    async {
        let! x1 = async1
        let! x2 = async2
        return x1 + x2
    } 

aResult |> Async.RunSynchronously

let aResult' = 
    monad {
        let! x1 = async1
        let! x2 = async2
        return x1 + x2 
    } 

aResult' |> Async.RunSynchronously

// Wykonanie powyższych obliczeń bez computation expression jest nieestetyczne
// async.Bind to funkcja która jest wywoływana przy użyciu `let!` w async expression
// async.Return to funkcja która jest wywoływana przy użyciu return
let aResult'' = async.Bind(async1, 
                    fun x1 -> async.Bind(async2, 
                                 fun x2 -> async.Return (x1 + x2)))
aResult'' |> Async.RunSynchronously

// Trochę lepiej wygląda bind chain z wykorzystaniem F#+
let aResult''' = async1 >>= fun x1 -> 
                    async2 >>= fun x2 -> 
                        x1 + x2 |> async.Return
aResult''' |> Async.RunSynchronously

// Chociaż dla prostych operacji możemy po prostu użyć
// map2 (F#+)
let aResult'''' = Async.map2 (+) async1 async2
                  |> Async.RunSynchronously
// albo lift2 (F#+)
let aResult''''' = lift2 (+) async1 async2
                   |> Async.RunSynchronously




////// Monad expression dla opcji. Tylko w F#+, nie ma w FSharp.Core.
let opt1 = Some 1
let opt2 = Some 2

let oResult = monad {
    let! x1 = opt1
    let! x2 = opt2
    return x1 + x2 }

let oResult' = 
    opt1 |> Option.bind (fun x1 -> 
        opt2 |> Option.bind (fun x2 -> x1 + x2 |> Some)) 

let oResult''  = lift2       (+) opt1 opt2
let oResult''' = Option.map2 (+) opt1 opt2

// Opcje można też zmatchować
let oResult'''' =
    match (opt1, opt2) with
    | (Some opt1, Some opt2) -> Some <| opt1 + opt2
    | (_,_)                  -> None




////// Trudniejsze operacje na opcjach
let opt3 = Some 3
let add1 = (+) 1
let isEven x = if x % 2 = 0                                 
               then Some x 
               else None
let multiplyBy4 = (*) 4

let harderOptResult = 
    monad {
        let! x1 = opt3
        let! even = x1 |> add1 |> isEven
        return multiplyBy4 even }

let harderOptResult' = 
    opt3 
    |> Option.bind (add1 >> isEven) 
    |> Option.map multiplyBy4

// Matchowanie się słabo skaluje
let harderOptResult'' = 
    match opt3 with
    | None -> None
    | Some v -> v |> add1 |> isEven
                |> function 
                    | None ->   None
                    | Some e -> Some <| multiplyBy4 e




////// Prosty przykład użycia dla Result
////// Działa bardzo podobnie do Option 
let res1 : Result<int,string> = Ok 1
let res2 : Result<int,string> = Error "Niepowodzenie"

let rResult = monad {
    let! x1 = res1
    let! x2 = res2
    return x1 + x2 }

let rResult'  = Result.map2 (+) res1 res2
let rResult'' = lift2       (+) res1 res2 

// Matchowanie resultów jest jeszcze żmudniejsze niż opcji
// Trzeba obsłużyć oba errory
let rResult''' =
    match (res1, res2) with
    | (Ok res1, Ok res2) -> Ok <| res1 + res2
    | (Error e ,_)       -> Error e
    | (_, Error e)       -> Error e




////// Rezultat funkcji która ma efekty
let eRes1 () = 
    printfn "computing res1"
    Ok ()

let eRes2 () = Error "Could not compute res2"

let effResult = 
    monad {
        do! eRes1 ()             // Gdy funkcja zwraca unit, to używamy do!
        do! eRes2 () }           // Zwracanie unit (void w C#) oznacza, że funkcję uruchamiamy dla efektów

// Jeśli nie używamy F#+, to implementacja jest mniej estetyczna
let effResult' = eRes1 () 
                 |> Result.bind (fun () -> eRes2 () 
                                           |> Result.bind (fun () -> Ok ()))

// F#+ bind niewiele pomaga
let effResult'' = eRes1 () 
                  >>= fun () -> eRes2 () 
                                >>= (fun () -> Ok ())
