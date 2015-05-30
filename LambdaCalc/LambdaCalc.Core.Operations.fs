namespace LambdaCalc.Core

open System


[<AutoOpen>]
module Operations =

    /// returns the name of all Atoms in the Expression
    let rec private identifiersTr (expression : Expression) (continuation) : AtomName Set =
        match expression with
        | Atom a 
            -> Set.singleton a |> continuation
        | Abstraction (a, body)
            -> identifiersTr body (Set.add a >> continuation)
        | Application (a, b)
            -> identifiersTr a
                (fun a' -> 
                    identifiersTr b
                        (fun b' -> Set.union a' b' |> continuation))

    /// returns the name of all Atoms in the Expression
    let identifiers (expression : Expression) : AtomName Set =
        identifiersTr expression id

    /// returns the names of all free (not bound nor binding) atoms
    /// Tail-recursive
    let rec private freeVariablesTr (expression : Expression) (continuation) : AtomName Set =
        match expression with
        | Atom a 
            -> Set.singleton a |> continuation
        | Abstraction (a, body)
            ->  freeVariablesTr body
                    (Set.remove a >> continuation)
        | Application (a, b)
            -> freeVariablesTr a
                (fun fvA -> 
                    freeVariablesTr b
                        (fun fvB -> Set.union fvA fvB |> continuation))

    /// returns the names of all free (not bound nor binding) atoms
    let freeVariables (expression : Expression) : AtomName Set =
        freeVariablesTr expression id

    /// checks if the given variable-name is free in the expression
    let isVariableFreeIn (expression : Expression) (variable : AtomName) = 
        let FV = freeVariables expression
        FV |> Set.contains variable

    /// checks if the given variable-name is bound or binding
    let isVariableNotFreeIn (expression : Expression) = isVariableFreeIn expression >> not

    /// finds a new/unused variable-name based on the given 
    /// by prepending '_' till the resulting variable is free
    /// Notice: this is already tail-recursive
    let rec findUnusedVariableFor (identifieres : AtomName Set) (variable : AtomName) : AtomName =
        if identifieres |> Set.contains variable
        then "_" + variable |> findUnusedVariableFor identifieres
        else variable

    /// tail-recursive version of alpha-subsitution: 
    /// substitutes free occurences of the for-part in the given expression
    /// and renames if needed to avoid unwanted binding of variables
    let rec private substituteTr (substitution : Substitution) (inExpression : Expression) (continuation) : Expression =
        match inExpression with
        | Atom a when a = substitution.For
            -> substitution.Substitute |> continuation
        | Atom a as unchanged
            -> unchanged |> continuation
        | Application (a, b)
            -> substituteTr substitution a
                (fun resA ->
                    substituteTr substitution b
                        (fun resB ->
                            Application (resA, resB)
                            |> continuation))
        | Abstraction (a, _) as unchanged when a = substitution.For
            -> unchanged |> continuation
        | Abstraction (a, body) as unchanged when substitution.For |> isVariableNotFreeIn body
            -> unchanged |> continuation
        | Abstraction (y, P) when y |> isVariableNotFreeIn substitution.Substitute
            -> substituteTr substitution P
                (fun resP -> Abstraction (y, resP) |> continuation)
        | Abstraction (a, body)
            -> let identifiers = Set.union (identifiers substitution.Substitute) (identifiers body)
               let newFor = findUnusedVariableFor identifiers a
               let newSubst = { Substitute = Atom newFor; For = a }
               substituteTr newSubst body
                 (fun resRename -> 
                    substituteTr substitution resRename
                        (fun res -> Abstraction (newFor, res) |> continuation))

    /// alpha-subsitution: substitutes free occurences of the for-part in the given expression
    /// and renames if needed to avoid unwanted binding of variables
    let substitute (substitution : Substitution) (inExpression : Expression) : Expression =
        substituteTr substitution inExpression id

    /// tail-recursive betaReduction
    let rec private betaReduceTr (sysParams : SystemParameters) (expression : Expression) (continuation) : Expression =
        sysParams.FailOnCancellation()
        match expression with
        | Application(a, b)
            -> betaReduceTr sysParams a
                (function
                 | Abstraction (x, body)
                   -> let body' = substitute { Substitute = b; For = x } body
                      betaReduceTr sysParams body' continuation
                 | _ as a'
                   -> betaReduceTr sysParams b
                        (fun b' -> Application (a', b') |> continuation))
        | Abstraction (a, body) when not sysParams.DelayAbstractionBodyReduction
            -> betaReduceTr sysParams body
                (fun body' -> Abstraction (a, body') |> continuation)
        | _ -> expression |> continuation

    /// beta-reduction
    let betaReduce (sysParams : SystemParameters) (expression : Expression) : Expression =
        betaReduceTr sysParams expression id

    /// tail-recursive equal-Operation
    let rec equalsTr (left : Expression) (right : Expression) (continuation) : bool =
        match left, right with
        | Atom a, Atom b 
            -> a = b |> continuation
        | Application(a, b), Application(a', b')
            -> equalsTr a a'
                (fun eq1 ->
                    if eq1 = false 
                    then continuation false 
                    else equalsTr b b' continuation)
        | Abstraction(a, bodyA), Abstraction(b, bodyB)
            -> let idents = Set.union (identifiers bodyA) (identifiers bodyB)
               let var = findUnusedVariableFor idents "?"
               let bodyA' = substitute { Substitute = (Atom var); For = a } bodyA
               let bodyB' = substitute { Substitute = (Atom var); For = b } bodyB
               equalsTr bodyA' bodyB' continuation
        | _ -> false

    let rec equals (left : Expression) (right : Expression) : bool =

        equalsTr left right id