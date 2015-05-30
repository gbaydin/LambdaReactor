namespace LambdaCalc.Parser

open System
open LambdaCalc

type IdentifierName = string
type AtomName = string
type Filename = string

type IParserEnvironment = 
    abstract KnownIdentifiers : IdentifierName seq
    abstract StoreExpression : IdentifierName * Core.Expression -> unit
    abstract LoadExpression : IdentifierName -> Core.Expression
    abstract CoreParameters : Core.SystemParameters

type InterpreterCommands =
    | Quit
    | Load of Filename
    | EnableBodyReductionDelay

type InterpreterResult =
    | Command of InterpreterCommands
    | Comparision of bool*(Core.Expression*Core.Expression)
    | Success of Core.Expression * IdentifierName list
    | Failure of exn

type ParserExpression =
    | Identifier  of IdentifierName
    | Atom        of AtomName
    | Abstraction of AtomName list * ParserExpression
    | Application of ParserExpression list
    with
    override pe.ToString() =
        let paramList = List.fold (+) ""
        let exprList = List.map (fun o -> o.ToString()) >> paramList
        match pe with
        | Identifier n
        | Atom n                -> n
        | Abstraction (ps,body) -> sprintf "(\%s.%s)" (paramList ps) (body.ToString())
        | Application exprs     -> sprintf "(%s)" (exprList exprs)

type ParserTerm =
    | Evaluation of ParserExpression
    | Comparison of ParserExpression * ParserExpression
    | Assignment of IdentifierName * ParserExpression
    | DirectAssignment of IdentifierName * ParserExpression
    | Command of InterpreterCommands
    with
    override pt.ToString() =
        match pt with
        | Evaluation pe     -> sprintf "%% >> %s" (pe.ToString())
        | Assignment (n,pe) -> sprintf "%s := %s" n (pe.ToString())
        | DirectAssignment (n,pe) 
                            -> sprintf "%s ::= %s" n (pe.ToString())
        | Comparison (a,b)  -> sprintf "%s == %s" (a.ToString()) (b.ToString())
        | Command cmd       -> sprintf "?%A" cmd

