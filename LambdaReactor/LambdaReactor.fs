﻿namespace LambdaReactor

open System
open LambdaCalc
open LambdaCalc.Core

module Reactor =
    let mutable delayBodyReduction = false
    let mutable cancellationToken : Threading.CancellationToken option = None

    let dictionaryEnv =
        let dict = new System.Collections.Generic.Dictionary<_,_>()
        { new Parser.IParserEnvironment with
            member i.LoadExpression id = dict.[id]
            member i.StoreExpression (id, expr) = dict.[id] <- expr
            member i.KnownIdentifiers = dict |> Seq.map (fun kvp -> kvp.Key)
            member i.CoreParameters = 
                { 
                    DelayAbstractionBodyReduction = delayBodyReduction
                    CancelProcessingToken = cancellationToken
                }
        }

    let eval s =
        let res = Parser.LambdaParser.interpret dictionaryEnv s
        match res with
        | Parser.InterpreterResult.Success (expression, knownIdentifier) -> Some(expression.ToString())
        | _ -> None


    let checkLambda s =
        let res = Parser.LambdaParser.interpret dictionaryEnv s
        match res with
        | Parser.InterpreterResult.Success (expression, knownIdentifier) ->
            match expression with
            | Expression.Abstraction(_, _) -> true
            | _ -> false
        | _ -> false


    let rnd = Random()

    //let test = eval "(((\x.\y.\z.(y)x)\x.(x)y)\u.\z.(u)z)y"

    //type Expression =
    //    | Atom        of AtomName
    //    | Abstraction of AtomName * Expression
    //    | Application of Expression * Expression


    let randomExp maxlength =
        let rec exp size =
            if size = 1 then 
                Atom(string (Convert.ToChar(rnd.Next(97, 122))))
            else
                let r = rnd.Next(3)
                match r with
                | 0 -> Atom(string (Convert.ToChar(rnd.Next(97, 122))))
                | 1 -> Abstraction(string (Convert.ToChar(rnd.Next(97, 122))), exp (size - 1))
                | 2 -> Application(exp (size - 1), exp (size - 1))
                | 3 -> Atom(string (Convert.ToChar(rnd.Next(97, 122))))
        let ee = eval ((exp maxlength).ToString())
        match ee with
        | Some(s) -> s
        | _ -> string (Convert.ToChar(rnd.Next(97, 122)))


    let initPop n (seeds:string[]) =
        Array.init n (fun _ -> seeds.[(rnd.Next(seeds.Length))])

    let apply e1 e2 =
        eval ("(" + e1 + ")" + e2)

    let run (p:string[]) n maxlength copyAllowed =
        let p' = Array.copy p
        for i = 0 to n do
            let e1 = p'.[rnd.Next(p'.Length)]
            if checkLambda e1 then
                let e2 = p'.[rnd.Next(p'.Length)]
                try
                    let c = apply e1 e2
                    match c with
                    | Some(s) -> 
                        if checkLambda s then
                            if s.Length <= maxlength then 
                                if s.CompareTo(e2.ToString()) = 0 then
                                    if copyAllowed then p'.[rnd.Next(p.Length)] <- s
                                else
                                    p'.[rnd.Next(p.Length)] <- s
                        else
                            ()
                    | _ -> ()
                with
                    | _ -> ()

        Array.sort p'

    let perturb (p:string[]) objects maxlength =
        let p' = Array.copy p
        for i = 0 to objects do
            p'.[rnd.Next(p'.Length)] <- randomExp maxlength
        Array.sort p'