Imports System.Text.RegularExpressions

Public Class ClVehicleImmatriculation

    Private _inputValue As String
    Public Sub New(value As String)
        _inputValue = value
    End Sub

    Public ReadOnly Property ImmatriculationWithoutSpaces As String
        Get
            Return GetCleanImmatriculation(_inputValue)
        End Get

    End Property

    Public Shared Function GetCleanImmatriculation(strImmat As String) As String
        If String.IsNullOrWhiteSpace(strImmat) Then
            Return String.Empty
        End If

        Dim pattern As String = "\b(?:[A-Z]{2}[-\s]?\d{3}[-\s]?[A-Z]{2}|\d{1,4}[\s-]?[A-Z]{1,3}[\s-]?\d{2})\b"
        Dim regex As New Regex(pattern, RegexOptions.IgnoreCase)
        Dim match As Match = regex.Match(strImmat)

        If match.Success Then
            ' On nettoie les tirets et les espaces
            Return match.Value.Replace("-", "").Replace(" ", "").ToUpper()
        Else
            Return strImmat
        End If
    End Function

    Public Overrides Function Equals(obj As Object) As Boolean
        Return MyBase.Equals(obj)
    End Function

End Class
