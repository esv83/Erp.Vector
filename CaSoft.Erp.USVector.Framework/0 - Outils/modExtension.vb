Imports System.Runtime.CompilerServices

Public Module modExtension

    <Extension()>
    Public Function CapitalizeFirstLetter(ByVal input As String) As String

        If String.IsNullOrEmpty(input) Then
            Return input
        End If

        Return Char.ToUpper(input(0)) & input.Substring(1).ToLower()

    End Function

End Module
