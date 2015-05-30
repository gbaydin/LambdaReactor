namespace LambdaCalc.Parser

open System
open FParsec

open LambdaCalc

module LambdaParser =

    let private ws = spaces

    let private parseLowerChar = satisfy (fun c -> isLower c && isLetter c)
    let private parseUpperChar = satisfy (fun c -> isUpper c && isLetter c)
    let private parseNumber = many digit
    let private parseNumberOrChar = many (digit <|> letter)
    let private getStr (cs : char list) = new String(Array.ofList cs)
    let private parseString = many letter |>> getStr
    let private parseFileName = 
        pchar '\"'
            >>. many1 (noneOf ['\"']) 
            .>> pchar '\"'
            |>> getStr
        <|> parseString

    let private parseAtomName =
        ws >>.
        pipe2 parseLowerChar
              parseNumber
              (fun c n -> c.ToString() + new String(List.toArray n))
        .>> ws

    let private parseIdentifierName =
        ws >>.
        pipe2 parseUpperChar
              parseNumberOrChar
              (fun c n -> c.ToString() + new String(List.toArray n))
        .>> ws
    
    let private parseAtom = parseAtomName |>> Atom
    let private parseIdentifier = parseIdentifierName |>> Identifier
    let private parseAtomOrIdentifier = parseAtom <|> parseIdentifier

    let private parseAtomNameChain =
        many1 parseAtomName

    let private SingleOrApplication = function
        | [] -> failwith "empty list cannot be parsed"
        | [e] -> e
        | exprs -> Application exprs

    let private parseExpression, private parseExpressionRef = 
        createParserForwardedToRef<ParserExpression, unit>()

    let private parseAbstraction =
        pipe2 (pchar '\\' >>. parseAtomNameChain .>> pchar '.')
              parseExpression
              (fun a body -> Abstraction (a, body))

    do parseExpressionRef :=
        let parseSingle =
            (pchar '(' >>. parseExpression .>> pchar ')')
            <|> attempt parseAbstraction
            <|> parseAtomOrIdentifier
        many1 (parseSingle .>> ws) |>> SingleOrApplication

    let private parseAssignmentHead =
        parseIdentifierName .>> (ws .>> (pstring ":=") .>> ws)

    let private parseAssignment =
        pipe2 parseAssignmentHead
              parseExpression
              (fun id expr -> Assignment (id, expr))

    let private parseDirectAssignmentHead =
        parseIdentifierName .>> (ws .>> (pstring "::=") .>> ws)

    let private parseDirectAssignment =
        pipe2 parseDirectAssignmentHead
              parseExpression
              (fun id expr -> DirectAssignment (id, expr))

    let private parseComparisionHead =
        parseExpression .>> (ws .>> (pstring "==") .>> ws)

    let private parseComparision =
        pipe2 parseComparisionHead
              parseExpression
              (fun a b -> Comparison (a, b))

    let private parseCommand =
        let translateCommand (cmd : string) (param : string option) =
            match cmd.ToUpper(), param with
            | "QUIT", _ 
                -> InterpreterCommands.Quit
            | "LOAD", Some file
                -> InterpreterCommands.Load file
            | "LOAD", None
                -> failwith "you have to give a filename to LOAD"
            | "ENABLEBODYREDUCTIONDELAY", _
                -> InterpreterCommands.EnableBodyReductionDelay
            | _ -> failwith (sprintf "unknown command %s" cmd)
        pchar '?' >>.
            pipe2 parseString 
                (ws >>. opt parseFileName)
                translateCommand

    let parse (input : string) : ParserTerm =
        let termParser =
            attempt parseAssignment
            <|> attempt parseDirectAssignment
            <|> attempt parseComparision
            <|> (parseExpression |>> Evaluation)
            <|> (parseCommand |>> Command)
        match run termParser input with
        | Success (result, _, _) -> result
        | Failure (error, _, _)  -> failwith error

    let private lookup (environment : IParserEnvironment) (id : IdentifierName) =
        environment.LoadExpression id

    let private store (environment : IParserEnvironment) (id : IdentifierName, exp : Core.Expression) =
        environment.StoreExpression (id, exp)

    let private buildLeftAssociativeApplication (expressions : Core.Expression list) =
        let rec inner expressions acc =
            match expressions with
            | [] -> acc
            | a::tl -> let acc' = Core.Application(acc, a)
                       inner tl acc'
        match expressions with
        | [] -> failwith "empty application"
        | [a] -> a
        | a::b::rest -> inner rest (Core.Application (a,b))

    let translate (environment : IParserEnvironment) (input : ParserExpression) : Core.Expression =
        let rec trans input continuation =
            match input with
            | Atom a       
                -> Core.Atom a |> continuation
            | Abstraction ([], body)
                -> trans body continuation
            | Abstraction (a::tl, body)
                -> trans (Abstraction (tl, body))
                     (fun body' -> Core.Abstraction (a, body') |> continuation)
            | Application expressions
                -> transList expressions
                    (fun tl -> tl |> buildLeftAssociativeApplication
                                  |> continuation)
            | Identifier i -> lookup environment i |> continuation
        and transList expressions continuation =
            match expressions with
            | []    
                -> continuation []
            | h::tl 
                -> transList tl
                    (fun tl' ->
                        trans h 
                            (fun h' -> h'::tl' |> continuation))
        trans input id

    let evaluate (environment : IParserEnvironment) (term : ParserTerm) : Core.Expression =
        let reduce e = Core.Operations.betaReduce environment.CoreParameters e
        match term with
        | Command cmd                 -> failwith "cannot evaluate command"
        | Evaluation expression       -> translate environment expression
                                         |> reduce
        | Comparison (a,b)            -> failwith "cannot evaluate comparison"
        | Assignment (id, expression) -> let expr = translate environment expression
                                                    |> reduce
                                         store environment (id, expr)
                                         expr
        | DirectAssignment (id, expression) 
                                      -> let expr = translate environment expression
                                         store environment (id, expr)
                                         expr

    let private findKnownIdentifiers (environment : IParserEnvironment) (expression : Core.Expression) : IdentifierName list =
        let get = environment.LoadExpression
        let knownIdentifieres = environment.KnownIdentifiers
        knownIdentifieres |> Seq.filter (get >> Core.Operations.equals expression)
                          |> Seq.toList

    let interpret (environment : IParserEnvironment) (input : string) : InterpreterResult =
        let reduce e = Core.Operations.betaReduce environment.CoreParameters e
        try
            match input |> parse with
            | Command cmd -> InterpreterResult.Command cmd
            | Comparison (a, b)
                          -> let a' = translate environment a |> reduce
                             let b' = translate environment b |> reduce
                             let res = Core.Operations.equals a' b'
                             InterpreterResult.Comparision (res, (a', b')) 
            | _ as other  -> let expression = evaluate environment other
                             let knownIdentifiers = findKnownIdentifiers environment expression
                             InterpreterResult.Success (expression, knownIdentifiers)
        with
        | _ as failure -> InterpreterResult.Failure failure

    let interpretMany (environment : IParserEnvironment) (inputs : string seq) : InterpreterResult seq =
        inputs |> Seq.map (interpret environment)

