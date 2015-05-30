namespace LambdaCalc.Core

open System
open System.Threading

type SystemParameters = 
    { 
        DelayAbstractionBodyReduction : bool;
        CancelProcessingToken : CancellationToken option
    } with
    static member Default =
        { 
            DelayAbstractionBodyReduction = false;
            CancelProcessingToken = None
        }
    static member FromCancellationToken(token) =
        { 
            DelayAbstractionBodyReduction = false;
            CancelProcessingToken = Some token
        }
    member this.FailOnCancellation() =
        match this.CancelProcessingToken with
        | None    -> ()
        | Some ct -> ct.ThrowIfCancellationRequested()