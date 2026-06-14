Imports System.Linq.Expressions

Public Class ClReflexionHelper
    Public Shared Function GetPropertyName(expressionBody As Expression) As String
        Dim Result As String = String.Empty

        Dim body As Expressions.MemberExpression = TryCast(expressionBody, Expressions.MemberExpression)

        If body Is Nothing Then
            Dim ubody As Expressions.UnaryExpression = CType(expressionBody, Expressions.UnaryExpression)
            body = TryCast(ubody.Operand, Expressions.MemberExpression)
        End If

        Result = body.Member.Name

        Return Result

    End Function

End Class
