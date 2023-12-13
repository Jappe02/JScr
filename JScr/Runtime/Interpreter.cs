using JScr.Frontend;
using JScr.Runtime.Eval;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static JScr.Frontend.Ast;
using static JScr.Runtime.Values;
using ValueType = JScr.Runtime.Values.ValueType;

namespace JScr.Runtime
{
    internal static class Interpreter
    {
        public static RuntimeVal EvaluateProgram(Program program, ref bool running)
        {
            var env = Environment.CreateGlobalEnv();

            return Statements.EvalProgram(program, env, ref running);
        }

        // TODO Make fully async & FIX ISSUES BECAUSE RETURN TYPE CHANGED TO ASYNC
        public static RuntimeVal Evaluate(Stmt astNode, Environment env)
        {
            switch (astNode.Kind) {
                case NodeType.NumericLiteral:
                    return new NumberVal((astNode as NumericLiteral).Value) as RuntimeVal;
                case NodeType.Identifier:
                    return Expressions.EvalIdentifier(astNode as Identifier, env);
                case NodeType.ObjectLiteral:
                    return Expressions.EvalObjectExpr(astNode as ObjectLiteral, env);
                case NodeType.CallExpr:
                    return Expressions.EvalCallExpr(astNode as CallExpr, env);
                case NodeType.AssignmentExpr:
                    return Expressions.EvalAssignment(astNode as AssignmentExpr, env);
                case NodeType.BinaryExpr:
                    return Expressions.EvalBinaryExpr(astNode as BinaryExpr, env);
                /*case NodeType.Program:
                    return Statements.EvalProgram(astNode as Program, env);*/

                // Handle statements
                case NodeType.VarDeclaration:
                    return Statements.EvalVarDeclaration(astNode as VarDeclaration, env);
                case NodeType.FunctionDeclaration:
                    return Statements.EvalFunctionDeclaration(astNode as FunctionDeclaration, env);

                // Handle unimplemented ast types as error
                default:
                    throw new RuntimeException($"This AST Node has not yet been setup for interpretation: {astNode}"); // <-- TODO runtime error
            }
        }
    }
}
