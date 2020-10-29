#r "FSharpPlus.dll"
open FSharpPlus

// Celem na dziś nie jest stworzenie tutorialu.
// Chcę tylko pokazać kilka przykładów, żeby każdy miał intuicję o czym rozmawiamy.
// Zachęcam do przeanalizowania przykładów samodzielnie

// Monada
// Formalnie: Monoid w kategorii endofunktorów 
// Prościej: Abstrakcyjny typ który implementuje pewien interfejs i ma pewne właściwości
// Intuicja: Pudełko na dane, na którym mogę robić pewne operacje

// Jaki interfejs implementują monady?

// Return
// a -> M a

let l  = [1]
let l' = List.singleton 1

let s  = seq [1]
let s' = Seq.singleton 1

let ar  = [|1|]
let ar' = Array.singleton 1

let a  = async { return 1 }
let a' = 1 |> async.Return

let o  = Some 1
let o' = None

let r  = Ok 1
let r' = Error "Wystąpił problem"

// Join
// M M a -> M a

let jl  = [[1]] |> List.concat
let jl' = [[1]] |> join

let sl  = seq [seq [1]] |> Seq.concat
let sl' = seq [seq [1]] |> join

let jar  = [|[|1|]|] |> Array.concat
let jar' = [|[|1|]|] |> join

let ja  = 1 |> async.Return |> async.Return |> fun x -> async.Bind (x,id)
let ja' = 1 |> async.Return |> async.Return |> join

let jo  = Some (Some 1) |> Option.flatten
let jo' = Some (Some 1) |> join

let jr : Result<int,int> = 
    Ok (Ok 1)
    |> function 
        | Ok (Ok v) -> Ok v 
        | Ok (Error e) 
        | Error e -> Error e  
          
let jr' : Result<int,int> = Ok (Ok 1) |> join 

// Map
// Monady są funktorami, więc możemy je mapować
// (a -> b) -> M a -> M b

// Przykładowa wspólna funkcja mapująca
let mapFunction x = x + 1 

let ml  = [1] |> List.map mapFunction
let ml' = [1] |>> mapFunction

let ms  = seq [1] |> Seq.map mapFunction
let ms' = seq [1] |>> mapFunction

let mar  = [|1|] |> Array.map mapFunction
let mar' = [|1|] |>> mapFunction

let ma  = async { return 1 } |> Async.map mapFunction
let ma' = async { return 1 } |>> mapFunction

let mo  = Some 1 |> Option.map mapFunction
let mo' = Some 1 |>> mapFunction

let mr  : Result<int,int> = Ok 1 |> Result.map mapFunction
let mr' : Result<int,int> = Ok 1 |>> mapFunction

// Bind
// (a -> M b) -> M a -> M b

let blf  = mapFunction >> List.singleton
let bl   = [1] |> List.collect blf
let bl'  = [1] |> bind blf
let bl'' = [1] >>= blf

let barf  = mapFunction >> Array.singleton
let bar   = [|1|] |> Array.collect barf
let bar'  = [|1|] |> bind barf
let bar'' = [|1|] >>= barf

let bsf  = mapFunction >> Seq.singleton
let bs   = [|1|] |> Seq.collect bsf
let bs'  = [|1|] |> bind bsf
let bs'' = [|1|] >>= bsf

let baf  = mapFunction >> async.Return
let ba   = async { return 1 } |> fun x -> async.Bind (x, baf)
let ba'  = async { return 1 } |> bind baf
let ba'' = async { return 1 } >>= baf

let bof  = mapFunction >> Some
let bo   = Some 1 |> Option.bind bof
let bo'  = Some 1 |> bind bof
let bo'' = Some 1 >>= bof

let brf = mapFunction >> Ok 
let br   : Result<int,int> = Ok 1 |> Result.bind brf
let br'  : Result<int,int> = Ok 1 |> bind brf
let br'' : Result<int,int> = Ok 1 >>= brf

// Niespodzianka
// Bind to Map + Join
let funkcja x = [x + 1] 

let niespodzianka   = [1] >>= funkcja
let niespodzianka'  = [1] |> map funkcja |> join