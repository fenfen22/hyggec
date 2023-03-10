// hyggec - The didactic compiler for the Hygge programming language.
// Copyright (C) 2023 Technical University of Denmark
// Author: Alceste Scalas <alcsc@dtu.dk>
// Released under the MIT license (see LICENSE.md for details)

/// Interpreter for Hygge programs.
module Interpreter

open AST


/// Does the given AST node represent a value?
let rec isValue (node: Node<'E,'T>): bool =
    match node.Expr with
    | UnitVal
    | BoolVal(_)
    | IntVal(_)
    | FloatVal(_)
    | StringVal(_) -> true
    | _ -> false


/// Runtime environment for the interpreter.  The type parameters have the same
/// meaning of the corresponding ones in AST.Node<'E,'T>: they allow the
/// environment to hold generic instances of AST.Node<'E,'T>.
type internal RuntimeEnv<'E,'T> = {
    /// Function called to read a line when evaluating 'ReadInt' and 'ReadFloat'
    /// AST nodes.
    Reader: Option<unit -> string>
    /// Function called to produce an output when evaluating 'Print' and
    /// 'PrintLn' AST nodes.
    Printer: Option<string -> unit>
    /// Mutable local variables: mapping from their name to their current value.
    Mutables: Map<string, Node<'E,'T>>
} with override this.ToString(): string =
        $"  - Reader: %O{this.Reader}"
          + $"%s{Util.nl}  - Printer: %O{this.Printer}"
          + $"%s{Util.nl}  - Mutables: %s{Util.formatMap this.Mutables}"


/// Attempt to reduce the given AST node by one step, using the given runtime
/// environment.  If a reduction is possible, return the reduced node and an
/// updated runtime environment; otherwise, return None.
let rec internal reduce (env: RuntimeEnv<'E,'T>)
                        (node: Node<'E,'T>): Option<RuntimeEnv<'E,'T> * Node<'E,'T>> =
    match node.Expr with
    | UnitVal
    | BoolVal(_)
    | IntVal(_)
    | FloatVal(_)
    | StringVal(_) -> None

    | Var(name) when env.Mutables.ContainsKey(name) ->
        Some(env, env.Mutables[name])
    | Var(_) -> None

    | Mult(lhs, rhs) ->
        match (lhs.Expr, rhs.Expr) with
        | (IntVal(v1), IntVal(v2)) ->
            Some(env, {node with Expr = IntVal(v1 * v2)})
        | (FloatVal(v1), FloatVal(v2)) ->
            Some(env, {node with Expr = FloatVal(v1 * v2)})
        | (_, _) ->
            match (reduceLhsRhs env lhs rhs) with
            | Some(env', lhs', rhs') ->
                Some(env', {node with Expr = Mult(lhs', rhs')})
            | None -> None
    
    | Div(lhs, rhs) ->
        match (lhs.Expr, rhs.Expr) with
        | (IntVal(v1), IntVal(v2)) ->
            Some(env, {node with Expr = IntVal(v1 / v2)})
        | (FloatVal(v1), FloatVal(v2)) ->
            Some(env, {node with Expr = FloatVal(v1 / v2)})
        | (_, _) ->
            match (reduceLhsRhs env lhs rhs) with
            | Some(env', lhs', rhs') ->
                Some(env', {node with Expr = Div(lhs', rhs')})
            | None -> None

    | Add(lhs, rhs) ->
        match (lhs.Expr, rhs.Expr) with
        | (IntVal(v1), IntVal(v2)) ->
            Some(env, {node with Expr = IntVal(v1 + v2)})
        | (FloatVal(v1), FloatVal(v2)) ->
            Some(env, {node with Expr = FloatVal(v1 + v2)})
        | (_, _) ->
            match (reduceLhsRhs env lhs rhs) with
            | Some(env', lhs', rhs') ->
                Some(env', {node with Expr = Add(lhs', rhs')})
            | None -> None
    | Sub(lhs, rhs) ->
        match (lhs.Expr, rhs.Expr) with
        | (IntVal(v1), IntVal(v2)) ->
            Some(env, {node with Expr = IntVal(v1 - v2)})
        | (FloatVal(v1), FloatVal(v2)) ->
            Some(env, {node with Expr = FloatVal(v1 - v2)})
        | (_, _) ->
            match (reduceLhsRhs env lhs rhs) with
            | Some(env', lhs', rhs') ->
                Some(env', {node with Expr = Sub(lhs', rhs')})
            | None -> None
    
    | Rem(lhs, rhs) ->
        match (lhs.Expr, rhs.Expr) with
        | (IntVal(v1), IntVal(v2)) ->
            Some(env, {node with Expr = IntVal(v1 % v2)})
        | (_, _) ->
            match (reduceLhsRhs env lhs rhs) with
            | Some(env', lhs', rhs') ->
                Some(env', {node with Expr = Rem(lhs', rhs')})
            | None -> None

    | And(lhs, rhs) ->
        match (lhs.Expr, rhs.Expr) with
        | (BoolVal(v1), BoolVal(v2)) ->
            Some(env, {node with Expr = BoolVal(v1 && v2)})
        | (_, _) ->
            match (reduceLhsRhs env lhs rhs) with
            | Some(env', lhs', rhs') ->
                Some(env', {node with Expr = And(lhs', rhs')})
            | None -> None

    | Or(lhs, rhs) ->
        match (lhs.Expr, rhs.Expr) with
        | (BoolVal(v1), BoolVal(v2)) ->
            Some(env, {node with Expr = BoolVal(v1 || v2)})
        | (_, _) ->
            match (reduceLhsRhs env lhs rhs) with
            | Some(env', lhs', rhs') ->
                Some(env', {node with Expr = Or(lhs', rhs')})
            | None -> None
    
    | Xor(lhs, rhs) ->
        match (lhs.Expr, rhs.Expr) with
        | (BoolVal(v1), BoolVal(v2)) ->
            Some(env, {node with Expr = BoolVal(v1 <> v2)})
        | (_, _) ->
            match (reduceLhsRhs env lhs rhs) with
            | Some(env', lhs', rhs') ->
                Some(env', {node with Expr = Xor(lhs', rhs')})
            | None -> None

    | Not(arg) ->
        match arg.Expr with
        | BoolVal(v) ->
            Some(env, {node with Expr = BoolVal(not v)})
        | _ ->
            match (reduce env arg) with
            | Some(env', arg2) ->
                Some(env', {node with Expr = Not(arg2)})
            | None -> None

    | Sqrt(arg) ->
        match arg.Expr with
        | FloatVal(v) ->
            Some (env, {node with Expr = FloatVal(sqrt v)})
        | _ ->
            match (reduce env arg) with
            | Some(env', arg') ->
                Some(env', {node with Expr = Sqrt(arg')})
            | None ->None
        
    | Eq(lhs, rhs) ->
        match (lhs.Expr, rhs.Expr) with
        | (IntVal(v1), IntVal(v2)) ->
            Some(env, {node with Expr = BoolVal(v1 = v2)})
        | (FloatVal(v1), FloatVal(v2)) ->
            Some(env, {node with Expr = BoolVal(v1 = v2)})
        | (_, _) ->
            match (reduceLhsRhs env lhs rhs) with
            | Some(env', lhs', rhs') ->
                Some(env', {node with Expr = Eq(lhs', rhs')})
            | None -> None

    | Less(lhs, rhs) ->
        match (lhs.Expr, rhs.Expr) with
        | (IntVal(v1), IntVal(v2)) ->
            Some(env, {node with Expr = BoolVal(v1 < v2)})
        | (FloatVal(v1), FloatVal(v2)) ->
            Some(env, {node with Expr = BoolVal(v1 < v2)})
        | (_, _) ->
            match (reduceLhsRhs env lhs rhs) with
            | Some(env', lhs', rhs') ->
                Some(env', {node with Expr = Less(lhs', rhs')})
            | None -> None
    
    | Large(lhs, rhs) ->
        match (lhs.Expr, rhs.Expr) with
        | (IntVal(v1), IntVal(v2)) ->
            Some(env, {node with Expr = BoolVal(v1 > v2)})
        | (FloatVal(v1), FloatVal(v2)) ->
            Some(env, {node with Expr = BoolVal(v1 > v2)})
        | (_, _) ->
            match (reduceLhsRhs env lhs rhs) with
            | Some(env', lhs', rhs') ->
                Some(env', {node with Expr = Large(lhs', rhs')})
            | None -> None
    
    | LessEq(lhs, rhs) ->
        match (lhs.Expr, rhs.Expr) with
        | (IntVal(v1), IntVal(v2)) ->
            Some(env, {node with Expr = BoolVal(v1 <= v2)})
        | (FloatVal(v1), FloatVal(v2)) ->
            Some(env, {node with Expr = BoolVal(v1 <= v2)})
        | (_, _) ->
            match (reduceLhsRhs env lhs rhs) with
            | Some(env', lhs', rhs') ->
                Some(env', {node with Expr = LessEq(lhs', rhs')})
            | None -> None

    | LargeEq(lhs, rhs) ->
        match (lhs.Expr, rhs.Expr) with
        | (IntVal(v1), IntVal(v2)) ->
            Some(env, {node with Expr = BoolVal(v1 >= v2)})
        | (FloatVal(v1), FloatVal(v2)) ->
            Some(env, {node with Expr = BoolVal(v1 >= v2)})
        | (_, _) ->
            match (reduceLhsRhs env lhs rhs) with
            | Some(env', lhs', rhs') ->
                Some(env', {node with Expr = LargeEq(lhs', rhs')})
            | None -> None

    | ReadInt ->
        match env.Reader with
        | None -> None
        | Some(reader) ->
            /// Input read from the runtime environment
            let input = reader()
            // Use the invariant culture to parse the integer value
            match System.Int32.TryParse(input, System.Globalization.NumberStyles.AllowLeadingSign,
                                        System.Globalization.CultureInfo.InvariantCulture) with
            | (true, result) ->
                Some(env, {node with Expr = IntVal(result)})
            | (false, _) ->
                Some(env, {node with Expr = UnitVal})

    | ReadFloat ->
        match env.Reader with
        | None -> None
        | Some(reader) ->
            /// Input read from the console
            let input = reader()
            /// Format used to parse the input
            let format = System.Globalization.NumberStyles.AllowLeadingSign
                         ||| System.Globalization.NumberStyles.AllowDecimalPoint
            // Use the invariant culture to parse the floating point value
            match System.Single.TryParse(input, format,
                                         System.Globalization.CultureInfo.InvariantCulture) with
            | (true, result) ->
                Some(env, {node with Expr = FloatVal(result)})
            | (false, _) ->
                Some(env, {node with Expr = UnitVal})

    | Print(arg) ->
        match env.Printer with
        | None -> None
        | Some(printer) -> // Reductum when printing succeeds (a unit value)
            let reductum = Some(env, {node with Expr = UnitVal})
            match arg.Expr with
            | BoolVal(v) -> printer $"%A{v}"; reductum
            | IntVal(v) -> printer $"%d{v}"; reductum
            | FloatVal(v) -> printer $"%f{v}"; reductum
            | StringVal(v) -> printer $"%s{v}"; reductum
            | _ when not (isValue node) ->
                match (reduce env arg) with
                | Some(env', arg2) ->
                    Some(env', {node with Expr = Print(arg2)})
                | None -> None
            | _ -> None

    | PrintLn(arg) ->
        // We recycle the evaluation of 'Print', by rewriting this AST node
        match (reduce env {node with Expr = Print(arg)}) with
        | Some(env', n) ->
            match n.Expr with
            | Print(targ) ->
                // Patch the reduced AST to restore the 'PrintLn' node
                Some(env', {n with Expr = PrintLn(targ)})
            | UnitVal ->
                // The 'Print' has been fully evaluated, let'd add newline
                match env.Printer with
                | None -> None
                | Some(printer) ->
                    printer $"%s{Util.nl}"
                    Some(env', n)
            | _ -> failwith $"BUG: unexpected 'Print' reduction ${n}"
        | None -> None

    | If(cond, ifTrue, ifFalse) ->
        match cond.Expr with
        | BoolVal(v) ->
            let branch = if v then ifTrue else ifFalse
            Some(env, {node with Expr = branch.Expr})
        | _ ->
            match (reduce env cond) with
            | Some(env', cond') ->
                Some(env', {node with Expr = If(cond', ifTrue, ifFalse)})
            | None -> None

    | Seq(nodes) ->
        match nodes with
        | [] -> Some(env, {node with Expr = UnitVal})
        | [last] -> // Last node in Seq: if it's a value, we reduce to it
            if isValue last then
                Some(env, {node with Expr = last.Expr})
            else
                match (reduce env last) with
                | Some(env', last') ->
                    Some(env', {node with Expr = last'.Expr})
                | None -> None
        | first :: rest -> // Notice that here 'rest' is non-empty
            if not (isValue first) then
                match (reduce env first) with
                | Some(env', first') ->
                    Some(env', {node with Expr = Seq(first' :: rest)})
                | None -> None
            else
                Some(env, {node with Expr = Seq(rest)})

    | Ascription(_, arg) -> Some(env, {node with Expr = arg.Expr})

    | Assertion(arg) ->
        match arg.Expr with
        | BoolVal(true) -> Some(env, {node with Expr = UnitVal})
        | _ ->
            match (reduce env arg) with
            | Some(env', arg') ->
                Some(env', {node with Expr = Assertion(arg')})
            | None -> None

    | Let(name, tpe, init, scope) ->
        match (reduce env init) with
        | Some(env', def') ->
            Some(env', {node with Expr = Let(name, tpe, def', scope)})
        | None when (isValue init) ->
            Some(env, {node with Expr = (ASTUtil.subst scope name init).Expr})
        | None -> None

    | LetMut(_, _, _, scope) when (isValue scope) ->
        Some(env, {node with Expr = scope.Expr})
    | LetMut(name, tpe, init, scope) ->
        match (reduce env init) with
        | Some(env', def') ->
            Some(env', {node with Expr = LetMut(name, tpe, def', scope)})
        | None when (isValue init) ->
            /// Runtime environment for reducing the 'let mutable...' scope
            let env' = {env with Mutables = env.Mutables.Add(name, init)}
            match (reduce env' scope) with
            | Some(env'', scope') ->
                /// Updated init value for the mutable variable
                let init' = env''.Mutables[name] // Crash if 'name' not found
                /// Updated runtime environment.  If the declared mutable
                /// variable 'name' was defined in the outer scope, we restore
                /// its old value (consequently, any update to the redefined
                /// variable 'name' is only visible in its scope).  Otherwise,
                /// we remove it from the updated runtime environment (so it
                /// is only visible in its scope)
                let env''' =
                    match (env.Mutables.TryFind(name)) with
                    | Some(v) -> {env'' with
                                    Mutables = env''.Mutables.Add(name, v)}
                    | None -> {env'' with
                                Mutables = env''.Mutables.Remove(name)}
                Some(env''', {node with Expr = LetMut(name, tpe, init', scope')})
            | None -> None
        | None -> None

    | Assign(target, expr) when not (isValue expr) ->
        match (reduce env expr) with
        | Some(env', expr') ->
            Some(env', {node with Expr = Assign(target, expr')})
        | None -> None
    | Assign({Expr = Var(vname)} as target, expr) when (isValue expr)  ->
        match (env.Mutables.TryFind vname) with
        | Some(_) ->
            let env' = {env with Mutables = env.Mutables.Add(vname, expr)}
            Some(env', {node with Expr = expr.Expr})
        | None -> None
    | Assign(_, _) ->
        None

    | While(cond, body) ->
        /// Rewritten 'while' loop, transformed into an 'if' on the condition
        /// 'cond'.  If 'cond' is true, we continue with the whole 'body' of the
        /// loop, followed by the whole loop itself (i.e. the node we have just
        /// matched); otherwise, when 'cond' is false, we do nothing (unit).
        let rewritten = If(cond,
                           {body with Expr = Seq([body; node])},
                           {body with Expr = UnitVal})
        Some(env, {node with Expr = rewritten})

    | Type(_, _, scope) ->
        // The interpreter does not use type information at all.
        Some(env, {node with Expr = scope.Expr})

/// Attempt to reduce the given lhs, and then (if the lhs is a value) the rhs,
/// using the given runtime environment.  Return None if either (a) the lhs
/// cannot reduce although it is not a value, or (b) the lhs is a value but the
/// rhs cannot reduce.
and internal reduceLhsRhs (env: RuntimeEnv<'E,'T>)
                          (lhs: Node<'E,'T>)
                          (rhs: Node<'E,'T>) : Option<RuntimeEnv<'E,'T> * Node<'E,'T> * Node<'E,'T>> =
    if not (isValue lhs) then
        match (reduce env lhs) with
        | Some(env', lhs') -> Some(env', lhs', rhs)
        | None -> None
    else match (reduce env rhs) with
         | Some(env', rhs') -> Some(env', lhs, rhs')
         | None -> None

/// Attempt to reduce the given list of nodes, using the given runtime
/// environment.  Proceed one node a time, following the order in the list; if a
/// node is already a value, attempt to reduce the next one.  If a reduction is
/// possible, return the updated list and environment.  Return None if (a) we
/// reach a node in the list that is stuck, or (b) the list is empty.
and internal reduceList (env: RuntimeEnv<'E,'T>)
                        (nodes: List<Node<'E,'T>>): Option<RuntimeEnv<'E,'T> * List<Node<'E,'T>>> =
    match nodes with
    | [] -> None
    | node :: rest ->
        if not (isValue node) then
            match (reduce env node) with
            | Some(env', n') -> Some(env', n' :: rest)
            | None -> None
        else
            match (reduceList env rest) with
            | Some(env', rest') -> Some(env', node :: rest')
            | None -> None


/// Reduce the given AST until it cannot reduce further, using the given
/// (optional) 'reader' and 'writer' functions.  Return the final unreducible
/// AST node.
let rec reduceFully (node: Node<'E,'T>)
                    (reader: Option<unit -> string>)
                    (printer: Option<string -> unit>): Node<'E,'T> =
    let env = {Reader = reader; Printer = printer
               Mutables = Map[]}
    reduceFullyWithEnv env node
and internal reduceFullyWithEnv (env: RuntimeEnv<'E,'T>) (node: Node<'E,'T>): Node<'E,'T> =
    match (reduce env node) with
    | Some(env', node') -> reduceFullyWithEnv env' node'
    | None -> node


/// Reduce the given AST by the given number of steps, if possible.  Return the
/// resulting AST node and the number of leftover reduction steps: if greater
/// than 0, it means that the returned AST node cannot be reduced further.
let rec reduceSteps (node: Node<'E,'T>)
                    (reader: Option<unit -> string>) (printer: Option<string -> unit>)
                    (steps: int): Node<'E,'T> * int =
    let env = {Reader = reader; Printer = printer
               Mutables = Map[]}
    reduceStepsWithEnv env node steps
and internal reduceStepsWithEnv (env: RuntimeEnv<'E,'T>) (node: Node<'E,'T>)
                                (steps: int): Node<'E,'T> * int =
    match (reduce env node) with
    | Some(env', node') -> reduceStepsWithEnv env' node' (steps-1)
    | None -> (node, steps)


/// Is the given AST node stuck?  I.e. is it unreducible, and not a value?
let isStuck (node: Node<'E,'T>): bool =
    if (isValue node) then false
    else
        let env = {Reader = Some(fun _ -> ""); Printer = Some(fun _ -> ())
                   Mutables = Map[]}
        match (reduce env node) with
         | Some(_,_) -> false
         | None -> true


/// Main interpreter function.  Reduce the given AST node until it cannot reduce
/// further; if 'verbose' is true, also print each reduction step on the console.
let interpret (node: AST.Node<'E,'T>) (verbose: bool): AST.Node<'E,'T> =
    // Reader function used when interpreting (just a regular ReadLine)
    let reader = fun (_: unit) -> System.Console.ReadLine()
    // Printer function used when interpreting (just a regular printf)
    let printer = fun str -> printf $"%s{str}"
    if not verbose then
        reduceFully node (Some reader) (Some printer)
    else
        // Internal function to verbosely interpret step-by-step.
        let rec reduceVerbosely (env: RuntimeEnv<'E,'T>)
                                (ast: AST.Node<'E,'T>): RuntimeEnv<'E,'T> * AST.Node<'E,'T> =
            Log.debug $"Runtime environment:%s{Util.nl}%O{env}"
            Log.debug $"Term to be reduced:%s{Util.nl}%s{PrettyPrinter.prettyPrint ast}"
            match (reduce env ast) with
            | Some(env', node') -> reduceVerbosely env' node'
            | None -> (env, ast)

        let env = {Reader = Some(reader); Printer = Some(printer)
                   Mutables = Map[]}
        let (env', node') = reduceVerbosely env node
        node'
