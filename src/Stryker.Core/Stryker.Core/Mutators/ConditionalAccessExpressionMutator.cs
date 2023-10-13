using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Core.Mutants;
using Microsoft.CodeAnalysis;

namespace Stryker.Core.Mutators
{
    internal class ConditionalAccessExpressionMutator : MutatorBase<ConditionalAccessExpressionSyntax>, IMutator
    {
        public override MutationLevel MutationLevel => MutationLevel.Standard;

        public override IEnumerable<Mutation> ApplyMutations(ConditionalAccessExpressionSyntax node)
        {
            var original = node;
            if (node.Parent is ConditionalAccessExpressionSyntax || node.Parent is MemberAccessExpressionSyntax)
            {
                yield break;
            }

            foreach (var mutation in FindMutableMethodCalls(node, original))
            {
                yield return mutation;
            }
        }

        private static IEnumerable<Mutation> FindMutableMethodCalls(ExpressionSyntax node, ExpressionSyntax original)
        {
            while (node is ConditionalAccessExpressionSyntax conditional && conditional.WhenNotNull is ConditionalAccessExpressionSyntax)
            {
                foreach (var subMutants in FindMutableMethodCalls(conditional.WhenNotNull, original))
                {
                    yield return subMutants;
                }
                node = conditional.WhenNotNull;
            }

            while(true)
            {
                ExpressionSyntax next = null;

                if (!(node is ConditionalAccessExpressionSyntax conditionalAccessExpressionSyntax))
                {
                    yield break;
                }

                switch(conditionalAccessExpressionSyntax)
                {
                    case ConditionalAccessExpressionSyntax:
                        yield return Test(conditionalAccessExpressionSyntax, original);
                          yield break;
                    default:
                        break;
                }

                var whenNotNull = conditionalAccessExpressionSyntax.WhenNotNull;
                switch (whenNotNull)
                {
                    case MemberBindingExpressionSyntax:
                        yield return CreateMemberBindingExpressionMutation(conditionalAccessExpressionSyntax);
                        yield break;
                    default:
                        break;
                }


                // switch (invocationExpression.Expression)
                // {
                //     case MemberAccessExpressionSyntax memberAccessExpression:
                //         toReplace = memberAccessExpression.Name;
                //         memberName = memberAccessExpression.Name.Identifier.ValueText;
                //         next = memberAccessExpression.Expression;
                //         break;
                //     case MemberBindingExpressionSyntax memberBindingExpression:
                //         toReplace = memberBindingExpression.Name;
                //         memberName = memberBindingExpression.Name.Identifier.ValueText;
                //         break;
                //     default:
                //         yield break;
                // }

                // if (Enum.TryParse(memberName, out LinqExpression expression) &&
                //     KindsToMutate.TryGetValue(expression, out var replacementExpression))
                // {
                //     if (RequireArguments.Contains(replacementExpression) &&
                //         invocationExpression.ArgumentList.Arguments.Count == 0)
                //     {
                //         yield break;
                //     }

                //     yield return new Mutation
                //     {
                //         DisplayName =
                //             $"Linq method mutation ({memberName}() to {SyntaxFactory.IdentifierName(replacementExpression.ToString())}())",
                //         OriginalNode = original,
                //         ReplacementNode = original.ReplaceNode(toReplace,
                //             SyntaxFactory.IdentifierName(replacementExpression.ToString())),
                //         Type = Mutator.Linq
                //     };
                // }
                node = next;
            }
        
        }

         private static Mutation CreateConditionalAccessExpressionMutation(ConditionalAccessExpressionSyntax node)
        {
            var whenNotNullExpression = (node.WhenNotNull as ConditionalAccessExpressionSyntax).Expression;
            var conditionalAccesExpression = node.WhenNotNull as ConditionalAccessExpressionSyntax;

            return whenNotNullExpression switch
            {
                MemberBindingExpressionSyntax memberBindingExpression => CreateConditionalAccessMemberBindingExpressionMutation(node, conditionalAccesExpression, memberBindingExpression),
                _ => null,
            };
        }

        private static Mutation CreateConditionalAccessMemberBindingExpressionMutation(ConditionalAccessExpressionSyntax node, ConditionalAccessExpressionSyntax conditionalAccesExpression, MemberBindingExpressionSyntax memberBindingExpression)
        {
            var leftHandSide = CreateMemberAccessExpression(node, memberBindingExpression);

            var rightHandSide = conditionalAccesExpression.WhenNotNull;

            var replacementNode = SyntaxFactory.ConditionalAccessExpression(
                leftHandSide,
                rightHandSide);

            return new Mutation()
            {
                OriginalNode = node,
                DisplayName = "Conditional access expression",
                ReplacementNode = replacementNode,
                Type = Mutator.Access
            };
        }

        private static MemberAccessExpressionSyntax CreateMemberAccessExpression(ConditionalAccessExpressionSyntax node, MemberBindingExpressionSyntax memberBindingExpression)
        => SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            node.Expression,
            SyntaxFactory.Token(SyntaxKind.DotToken),
            memberBindingExpression.Name);

        private static Mutation Test(ConditionalAccessExpressionSyntax node, ExpressionSyntax original)
        {
            var leftHandSide = node.Expression as MemberBindingExpressionSyntax;
            var memberBindingExpression = node.WhenNotNull as MemberBindingExpressionSyntax;

            var replacementNode = SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            leftHandSide,
                            SyntaxFactory.Token(SyntaxKind.DotToken),
                            memberBindingExpression.Name);
var x = original.ReplaceNode(node, replacementNode);
            return new Mutation()
            {
                OriginalNode = original,
                DisplayName = "Conditional access expression",
                ReplacementNode = x,
                Type = Mutator.Access
            };
        }

        private static Mutation CreateMemberBindingExpressionMutation(ConditionalAccessExpressionSyntax node)
        {
            var leftHandSide = node.Expression as MemberAccessExpressionSyntax;
            var memberBindingExpression = node.WhenNotNull as MemberBindingExpressionSyntax;

            var replacementNode = SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            leftHandSide,
                            SyntaxFactory.Token(SyntaxKind.DotToken),
                            memberBindingExpression.Name);

            return new Mutation()
            {
                OriginalNode = node,
                DisplayName = "Conditional access expression",
                ReplacementNode = replacementNode,
                Type = Mutator.Access
            };
        }
    }
}
