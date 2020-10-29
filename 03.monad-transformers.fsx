#r "FSharpPlus.dll"
open FSharpPlus
open FSharpPlus.Data

// Ok, wszystko fajnie. A co jeśli mam monadę w monadzie?
// Używamy wtedy transformatora monady:
// 0. Dodajemy FSharpPlus i otwieramy FSharpPlus.Data
// 1. Mamy typ WesołaMonada<ŚmiesznaMonada<'a>>
// 2. Opakowujemy go w typ ŚmiesznaMonadaT i dostajemy ŚmiesznaMonadaT<WesołaMonada<ŚmiesznaMonada<'a>>
// 3. Robimy obliczenia na typie 'a w monad expression
// 4. Po monad expression odpalamy ŚmiesznaMonadaT.run

////// Asynchroniczna opcja. Prosty przypadek
////// Async<Option<int>> opakowujemy w OptionT, żeby dodać liczby
let v1 : Async<Option<int>> = async { return Some 1 }
let v2 : Async<Option<int>> = async { return Some 2 }

let vResult = 
    monad {
        let! x1 = OptionT v1
        let! x2 = OptionT v2
        return x1 + x2
    } |> OptionT.run
      |> Async.RunSynchronously

// Prosty przypadek można obsłużyć przez lift2
let vResult' = lift2 (+) (OptionT v1) (OptionT v2)
               |> OptionT.run
               |> Async.RunSynchronously
// Albo map2
let vResult'' = OptionT.map2 (+) (OptionT v1) (OptionT v2)
                |> OptionT.run
                |> Async.RunSynchronously




////// Co jeśli typy się nie zgadzają?
////// Muszę je doprowadzić do takiego samego, czyli Async<Option<int>>
let ao1 = async { return Some 1 }
let ao2 = async { return Some 2 }
let ao3 = Some 3                        // Mamy opcję ale nie asynchroniczną, opakowujemy ją w async
let ao4 = async { return 4 }            // Mamy asynchroniczną, ale nie opcję. 
                                        // Używamy OptionT.lift, żeby dostać OptionT<Async<Option<int>>>

let aoResult = 
    monad {
        let! x1 = OptionT ao1
        let! x2 = OptionT ao2
        let! x3 = OptionT <| async.Return ao3
        let! x4 = OptionT.lift ao4
        return x1 + x2 + x3 + x4  // Or use `List.reduce (+) [x1;x2;x3;x4]`
    } |> OptionT.run
      |> Async.RunSynchronously



////// Result działa podobnie
type AsyncResult = Async<Result<int, string>>
let ar1: AsyncResult = async { return Ok 1 }
let ar2: AsyncResult = async { return Ok 2 }

let ar = 
    monad {
        let! r1 = ResultT ar1
        let! r2 = ResultT ar2
        return r1 + r2 
    } |> ResultT.run
      |> Async.RunSynchronously

let ar'  = lift2 (+) (ResultT ar1) (ResultT ar2)
          |> ResultT.run
          |> Async.RunSynchronously

let ar'' = ResultT.map2 (+) (ResultT ar1) (ResultT ar2)
          |> ResultT.run
          |> Async.RunSynchronously



/////// Async Result z efektami i przekazywaniem wartości
let r1 () = async { 
    printfn "r1 = %d" 1
    return Ok 1 }

let r2 x = async {
    let r2Val = x + 1
    printfn "r2 = %d" r2Val
    return Ok r2Val }

let r3 x = async {
    let r3Val = x + 1
    printfn "r3 = %d" r3Val
    return Error "Wystąpiły problemy w r3" } 

let r4 () = async {
    printfn "r4 is run"
    return Ok () }

let aer = 
    monad {
        let! v1 = ResultT <| (r1 ())
        let! v2 = ResultT <| (r2 v1)
        do!       ResultT <| r3 v2 
        do!       ResultT <| r4 ()
    } |> ResultT.run 
      |> Async.RunSynchronously




////// Trzy warstwy. Transformery można ze sobą komponeować
type T1 = Async<Option<Result<int, string>>> 

let v1' = async { return Some (Ok 1) } : T1
let v2' = async { return Some (Ok 2) } : T1

let combinedT = OptionT >> ResultT
let runCombined : ResultT<OptionT<T1>> -> T1 = 
    ResultT.run >> OptionT.run 

let result = 
    monad {
        let! x1 = combinedT v1'
        let! x2 = combinedT v2'
        return x1 + x2 
    } |> runCombined
      |> Async.RunSynchronously



////// A co jeśli chcę odpalić wiele operacji asynchronicznie i zobaczyć które się udały, a które nie?
let a = async { return Ok    <| 1 }
let b = async { return Error <| "b error" }
let c = async { return Ok    <| 42 }
let d = async { return Error <| "d error" }
let arList = [a;b;c;d]

let multiResult = 
    Async.Sequential arList
    |>> (toList >> Result.partition) 
    |> Async.RunSynchronously

// Ciekawostka: Applicative style
Async.RunSynchronously <!> [a;b;c;d]
|> Result.partition
