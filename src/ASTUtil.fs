// hyggec - The didactic compiler for the Hygge programming language.
// Copyright (C) 2023 Technical University of Denmark
// Author: Alceste Scalas <alcsc@dtu.dk>
// Released under the MIT license (see LICENSE.md for details)

/// Utility functions to inspect and manipulate the Abstract Syntax Tree of
/// Hygge programs.
module ASTUtil

open AST


/// Given the AST 'node', return a new AST node where every free occurrence of
/// the variable called 'var' is substituted by the AST node 'sub'.
let rec subst (node: Node<'E,'T>) (var: string) (sub: Node<'E,'T>): Node<'E,'T> =
    match node.Expr with
    | UnitVal
    | IntVal(_)
    | BoolVal(_)
    | FloatVal(_)
    | StringVal(_) -> node // The substitution has no effect

    | Var(vname) when vname = var -> sub // Substitution applied
    | Var(_) -> node // The substitution has no effect

    | Add(lhs, rhs) ->
        {node with Expr = Add((subst lhs var sub), (subst rhs var sub))}
    | Sub(lhs, rhs) ->
        {node with Expr = Sub((subst lhs var sub), (subst rhs var sub))}
    | Mult(lhs, rhs) ->
        {node with Expr = Mult((subst lhs var sub), (subst rhs var sub))}
    | Div(lhs,rhs)->
        {node with Expr = Div((subst lhs var sub), (subst rhs var sub))}
    | Rem(lhs, rhs) ->
        {node with Expr = Rem((subst lhs var sub), (subst rhs var sub))}
    | And(lhs, rhs) ->
        {node with Expr = And((subst lhs var sub), (subst rhs var sub))}
    | Or(lhs, rhs) ->
        {node with Expr = Or((subst lhs var sub), (subst rhs var sub))}
    | Xor(lhs, rhs) ->
        {node with Expr = Xor((subst lhs var sub), (subst rhs var sub))}
    | Not(arg) ->
        {node with Expr = Not(subst arg var sub)}
    | Sqrt(arg) ->
        {node with Expr = Sqrt(subst arg var sub)}
    | Eq(lhs, rhs) ->
        {node with Expr = Eq((subst lhs var sub), (subst rhs var sub))}
    | Less(lhs, rhs) ->
        {node with Expr = Less((subst lhs var sub), (subst rhs var sub))}
    | Large(lhs, rhs) ->
        {node with Expr = Large((subst lhs var sub), (subst rhs var sub))}
    | LessEq(lhs, rhs) ->
        {node with Expr = LessEq((subst lhs var sub), (subst rhs var sub))}
    | LargeEq(lhs, rhs) ->
        {node with Expr = LargeEq((subst lhs var sub), (subst rhs var sub))}

    | ReadInt
    | ReadFloat -> node // The substitution has no effect

    | Print(arg) ->
        {node with Expr = Print(subst arg var sub)}
    | PrintLn(arg) ->
        {node with Expr = PrintLn(subst arg var sub)}

    | If(cond, ifTrue, ifFalse) ->
        {node with Expr = If((subst cond var sub), (subst ifTrue var sub),
                                                   (subst ifFalse var sub))}

    | Seq(nodes) ->
        let substNodes = List.map (fun n -> (subst n var sub)) nodes
        {node with Expr = Seq(substNodes)}

    | Ascription(tpe, node) ->
        {node with Expr = Ascription(tpe, (subst node var sub))}

    | Let(vname, _, _, _) when vname = var -> node // No substitution
    | Let(vname, tpe, init, scope) ->
        {node with Expr = Let(vname, tpe, (subst init var sub),
                              (subst scope var sub))}

    | LetMut(vname, _, _, _) when vname = var -> node // No substitution
    | LetMut(vname, tpe, init, scope) ->
        {node with Expr = LetMut(vname, tpe, (subst init var sub),
                                 (subst scope var sub))}

    | Assign(target, expr) ->
        {node with Expr = Assign((subst target var sub), (subst expr var sub))}

    | While(cond, body) ->
        let substCond = subst cond var sub
        let substBody = subst body var sub
        {node with Expr = While(substCond, substBody)}

    | Assertion(arg) ->
        {node with Expr = Assertion(subst arg var sub)}

    | Type(tname, def, scope) ->
        {node with Expr = Type(tname, def, (subst scope var sub))}
