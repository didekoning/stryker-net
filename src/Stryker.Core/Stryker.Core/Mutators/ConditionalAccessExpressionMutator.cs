using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Core.Mutants;
using Microsoft.CodeAnalysis;
using Stryker.Core.Reporters.Json.TestFiles;

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
            var currentNode = node;
            while (currentNode is ConditionalAccessExpressionSyntax conditional && conditional.WhenNotNull is ConditionalAccessExpressionSyntax)
            {
                foreach (var subMutation in FindMutableMethodCalls(conditional.WhenNotNull, original))
                {
                    yield return subMutation;
                }
                currentNode = conditional.WhenNotNull;
            }

            while (true)
            {
                ExpressionSyntax next = null;

                if (!(node is ConditionalAccessExpressionSyntax conditionalAccessExpressionSyntax))
                {
                    yield break;
                }

                yield return conditionalAccessExpressionSyntax.WhenNotNull switch
                {
                    MemberBindingExpressionSyntax _ => CreateConditionalAccessMemberBindingExpressionMutation(conditionalAccessExpressionSyntax, original),
                    ConditionalAccessExpressionSyntax _ => CreateConditionalAccessMemberAccessExpressionMutation(conditionalAccessExpressionSyntax, original),
                    MemberAccessExpressionSyntax _ => Test(conditionalAccessExpressionSyntax, original),
                    _ => null,
                };


                node = next;
            }

        }

        private static Mutation Test(ConditionalAccessExpressionSyntax node, ExpressionSyntax original)
        {
            var leftHandSide = node.Expression;
            var rightHandSide = node.WhenNotNull as MemberAccessExpressionSyntax;
            var middle = rightHandSide.Expression;

            while (middle is MemberAccessExpressionSyntax innerMemberAccess)
            {
  
              if(innerMemberAccess.Expression is MemberBindingExpressionSyntax test){
                    leftHandSide = SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    leftHandSide,
                    test.Name);
                }
                leftHandSide = SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    leftHandSide,
                    innerMemberAccess.Name);

                middle = innerMemberAccess.Expression as MemberBindingExpressionSyntax;
            }

            var replacementNode = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                leftHandSide,
                rightHandSide.Name);
            return new Mutation()
            {
                OriginalNode = original,
                DisplayName = "Conditional access expression",
                ReplacementNode = original.ReplaceNode(node, replacementNode),
                Type = Mutator.Access
            };
        }

        private static MemberAccessExpressionSyntax CreateMemberAccessExpression(ConditionalAccessExpressionSyntax node, MemberBindingExpressionSyntax memberBindingExpression)
        => SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            node.Expression,
            SyntaxFactory.Token(SyntaxKind.DotToken),
            memberBindingExpression.Name);


        private static Mutation CreateConditionalAccessMemberAccessExpressionMutation(ConditionalAccessExpressionSyntax node, ExpressionSyntax original)
        {
            var whenNotNullExpression = (node.WhenNotNull as ConditionalAccessExpressionSyntax).Expression;
            var conditionalAccesExpression = node.WhenNotNull as ConditionalAccessExpressionSyntax;
            var leftHandSide = CreateMemberAccessExpression(node, (MemberBindingExpressionSyntax)whenNotNullExpression);

            var rightHandSide = conditionalAccesExpression.WhenNotNull;

            var replacementNode = SyntaxFactory.ConditionalAccessExpression(
                leftHandSide,
                rightHandSide);

            return new Mutation()
            {
                OriginalNode = node,
                DisplayName = "Conditional access expression",
                ReplacementNode = original.ReplaceNode(node, replacementNode),
                Type = Mutator.Access
            };
        }

        private static Mutation CreateConditionalAccessMemberBindingExpressionMutation(ConditionalAccessExpressionSyntax node, ExpressionSyntax original)
        {
            var memberBindingExpression = node.WhenNotNull as MemberBindingExpressionSyntax;

            var replacementNode = SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            node.Expression,
                            SyntaxFactory.Token(SyntaxKind.DotToken),
                            memberBindingExpression.Name);
            return new Mutation()
            {
                OriginalNode = original,
                DisplayName = "Conditional access expression",
                ReplacementNode = original.ReplaceNode(node, replacementNode),
                Type = Mutator.Access
            };
        }
    }
}
