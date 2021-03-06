﻿namespace LambdaReactor

open System
open LambdaCalc
open LambdaCalc.Core
open System.Runtime.InteropServices

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


    let inline isLambda (s:string) =
        s.StartsWith(@"\")

    let inline isValidLambda (s:string) =
        if s.StartsWith(@"\") then
            let var = s.Substring(1, 1)
            let v = s.Split([|'.'|])
            v.[1].Contains(var)
        else
            false

    let rnd = Random()

    let rec randomExp patomic pabstraction maxdepth =
        let rec expression size =
            if size = 1 then 
                Atom(string (Convert.ToChar(rnd.Next(97, 122))))
            else
                let r = rnd.NextDouble()
                if r < patomic then
                    Atom(string (Convert.ToChar(rnd.Next(97, 122))))
                elif r < patomic + pabstraction then
                    let args = expression (size - 1)
                    let argss = args.ToString().Replace(".", "").Replace(@"\", "")
                    Abstraction(argss.Substring(rnd.Next(argss.Length), 1), args)
                else
                    Application(Abstraction(string (Convert.ToChar(rnd.Next(97, 122))), expression (size - 1)), expression (size - 1))
        let ee = eval ((expression maxdepth).ToString())
        match ee with
        | Some(s) -> 
            if isLambda s then
                if isValidLambda s then
                    s
                else
                    randomExp patomic pabstraction maxdepth
            else
                s
        | _ -> randomExp patomic pabstraction maxdepth


    let initPop n (seeds:string[]) =
        Array.init n (fun _ -> seeds.[(rnd.Next(seeds.Length))])

    let apply e1 e2 =
        eval ("(" + e1 + ")" + e2)

    let collide ([<Out>]p:string[] byref) maxlength copyAllowed =
        let e1 = p.[rnd.Next(p.Length)]
        if isLambda e1 then
            let e2 = p.[rnd.Next(p.Length)]
            try
                let c = apply e1 e2
                match c with
                | Some(s) -> 
                    if s.Length <= maxlength then 
                        if (not copyAllowed) && ((s.CompareTo(e1) = 0) || (s.CompareTo(e2) = 0)) then
                            "Elastic collision (product is a copy): " + e1 + " & " + e2
                        elif (isLambda s) && (not (isValidLambda s)) then
                            "Elastic collision (product is a bad lambda): " + e1 + " & " + e2
                        else
                            p.[rnd.Next(p.Length)] <- s
                            p <- Array.sort p
                            "Collision: " + e1 + " & " + e2 + " -> " + s
                    else
                        "Elastic collision (product too long): " + e1 + " & " + e2
                | _ -> "Elastic collision (product unknown): " + e1 + " & " + e2
            with
                | _ -> "Elastic collision (product error): " + e1 + " & " + e2
        else
            "Elastic collision (operator not lambda): " + e1

    let perturb ([<Out>]p:string[] byref) objects patomic pabstraction maxlength =
        for i = 0 to objects do
            p.[rnd.Next(p.Length)] <- randomExp patomic pabstraction maxlength
        p <- Array.sort p