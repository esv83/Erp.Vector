Public Class ClContactModel
    Public Property ContactId As Guid?
    Public Property Name As String = String.Empty
    Public Property FirstName As String = String.Empty
    Public ReadOnly Property CompleteName As String
        Get
            Return String.Format("{0} {1}", FirstName, Name)
        End Get
    End Property

    Public Property DDN As Date?

    Public ReadOnly Property Age As String
        Get
            If DDN.HasValue Then
                Return ClHelper.GetAgeString(DDN.Value)
            Else
                Return String.Empty
            End If
        End Get

    End Property
End Class