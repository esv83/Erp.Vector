
Public Class ClBuilderBase(Of T As ClBusinessBase(Of T))
    Protected _businessBase As T
    Public Sub New(businessbase As T)
        _businessBase = businessbase
    End Sub
    Public Function SetIfNotNull(strValue As String) As String
        Dim result As String = String.Empty
        If Not String.IsNullOrWhiteSpace(strValue) Then
            result = strValue.Trim
        End If

        Return result

    End Function

    'Public Sub SetIfNotNull(value As Object, ByRef destination As Object)
    '    If value IsNot Nothing Then
    '        destination = value
    '    End If
    'End Sub


    'Public Function IsValid() As Boolean
    '    Return True
    'End Function
    Public Function MarkAsNew() As ClBuilderBase(Of T)
        _businessBase.MarkAsNew()
        Return Me
    End Function

    Public Function Build() As T
        Return _businessBase
    End Function
End Class
