namespace LambdaCalc.Core

open System

type AtomName = string

/// the basic LambdaCalculus expression Type for mainpulation and representation
type Expression =
    | Atom        of AtomName
    | Abstraction of AtomName * Expression
    | Application of Expression * Expression
    with
    override e.ToString() =
        let fe = FormatedExpression.FromExpression e
        fe.ToString()

/// used to give Expressions a nice human-readable string-representation
and FormatedExpression =
    | FormatedAtom of AtomName
    | FormatedAbstraction of (AtomName list * FormatedExpression)
    | FormatedApplication of FormatedExpression list
    | FormatedMultiApplication of FormatedExpression * int * FormatedExpression list
    with
    override this.ToString() =
        match this with
        | FormatedAtom a                
            -> a
        | FormatedAbstraction (a, body) 
            -> sprintf "\\%s.%s" (String.Join("", List.toArray a)) (body.ToString())
        | FormatedApplication a         
            -> sprintf "(%s)" (String.Join("", 
                                           a |> List.map (fun x -> x.ToString()) 
                                             |> List.toArray))
        | FormatedMultiApplication (f, times, body)
            -> sprintf "(%s^{%d} %s)" (f.ToString())
                                      times
                                      (String.Join(
                                           "", 
                                           body |> List.map (fun x -> x.ToString()) 
                                                |> List.toArray))

    static member private Compactify (e : FormatedExpression) =
        // tail-recursive implementation
        let rec comp e continuation =
            match e with
            // uncurry Abstractions to make them more readable
            | FormatedAbstraction (args, FormatedAbstraction (args', innerBody))
                -> comp innerBody
                        (fun b' -> let e' = FormatedAbstraction (args@args', b')
                                   comp e' continuation) 
            | FormatedAbstraction (args, body)
                -> comp body (fun b' -> FormatedAbstraction (args, b') |> continuation)
            | FormatedApplication [f; FormatedApplication (g::r)] when f = g
                -> FormatedMultiApplication (f, 2, r) |> continuation
            | FormatedApplication [f; FormatedMultiApplication (g,times,r)] when f = g
                -> FormatedMultiApplication (f, times+1, r) |> continuation
            | FormatedApplication [FormatedApplication a; b]
                -> comp (FormatedApplication (a@[b])) continuation
            | FormatedApplication a
                -> a |> List.map FormatedExpression.Compactify 
                     |> FormatedApplication
                     |> continuation
            | _ -> e |> continuation
        comp e id

    static member FromExpression (expression : Expression) =
        let rec from e continuation =
            match e with
            | Expression.Atom a 
                -> FormatedAtom a 
                   |> continuation
            | Expression.Abstraction (a, body)
                -> from body (fun b -> FormatedAbstraction([a], b) 
                                       |> FormatedExpression.Compactify
                                       |> continuation) 
            | Expression.Application (a,b) 
                -> from a 
                    (fun a' -> 
                        from b 
                            (fun b' -> FormatedApplication [a'; b'] 
                                       |> FormatedExpression.Compactify
                                       |> continuation))
        from expression id

type Substitution = { Substitute : Expression; For : AtomName } with
    override s.ToString() =
        sprintf "[%s/%s]" (s.Substitute.ToString()) (s.For)

