
Imports System.ComponentModel
    Imports System.Reflection
    Imports System.Runtime.CompilerServices

    Public Module ModErrorExtension

        <Extension>
        Public Function GetLabel(value As [Enum]) As String
            Dim t = value.GetType()
            Dim name = [Enum].GetName(t, value)
            If String.IsNullOrWhiteSpace(name) Then Return value.ToString()

            Dim field = t.GetField(name)
            If field Is Nothing Then Return value.ToString()

            Dim attr = field.GetCustomAttribute(Of DescriptionAttribute)()
            If attr IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(attr.Description) Then
                Return attr.Description
            End If

            Return name
        End Function

    End Module
