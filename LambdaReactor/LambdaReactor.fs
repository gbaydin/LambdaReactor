namespace LambdaReactor

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


    let checkLambda s =
        let res = Parser.LambdaParser.interpret dictionaryEnv s
        match res with
        | Parser.InterpreterResult.Success (expression, knownIdentifier) ->
            match expression with
            | Expression.Abstraction(_, _) -> true
            | _ -> false
        | _ -> false


    let rnd = Random()

    let randomExp patomic pabstraction maxdepth =
        let rec expression size =
            if size = 1 then 
                Atom(string (Convert.ToChar(rnd.Next(97, 122))))
            else
                let r = rnd.NextDouble()
                if r < patomic then
                    Atom(string (Convert.ToChar(rnd.Next(97, 122))))
                elif r < patomic + pabstraction then
                    Abstraction(string (Convert.ToChar(rnd.Next(97, 122))), expression (size - 1))
                else
                    Application(expression (size - 1), expression (size - 1))
        let ee = eval ((expression maxdepth).ToString())
        match ee with
        | Some(s) -> s
        | _ -> string (Convert.ToChar(rnd.Next(97, 122)))


    let initPop n (seeds:string[]) =
        Array.init n (fun _ -> seeds.[(rnd.Next(seeds.Length))])

    let apply e1 e2 =
        eval ("(" + e1 + ")" + e2)

    let collide ([<Out>]p:string[] byref) maxlength copyAllowed =
        let e1 = p.[rnd.Next(p.Length)]
        //if checkLambda e1 then
        let e2 = p.[rnd.Next(p.Length)]
        try
            let c = apply e1 e2
            match c with
            | Some(s) -> 
                if s.Length <= maxlength then 
                    if s.CompareTo(e2.ToString()) = 0 then
                        if copyAllowed then 
                            p.[rnd.Next(p.Length)] <- s
                            p <- Array.sort p
                            "Collision: " + e1 + " & " + e2 + " -> " + s
                        else
                            "Elastic collision (product is a copy): " + e1 + " & " + e2
                    else
                        p.[rnd.Next(p.Length)] <- s
                        p <- Array.sort p
                        "Collision: " + e1 + " & " + e2 + " -> " + s
                else
                    "Elastic collision (product too long): " + e1 + " & " + e2
            | _ -> "Elastic collision (product unknown): " + e1 + " & " + e2
        with
            | _ -> "Elastic collision (product error): " + e1 + " & " + e2


    let perturb ([<Out>]p:string[] byref) objects patomic pabstraction maxlength =
        for i = 0 to objects do
            p.[rnd.Next(p.Length)] <- randomExp patomic pabstraction maxlength
        p <- Array.sort p