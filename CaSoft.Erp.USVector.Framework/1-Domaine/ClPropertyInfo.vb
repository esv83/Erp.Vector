Public Class ClPropertyInfo
    Public Property Name As String
    Public Property Type As Type

    Private _value As Object
    Public Property Value As Object
        Get
            If HasValue Then
                Return _value
            Else
                Return DefaultValue
            End If
        End Get
        Set(value As Object)
            _value = value
        End Set
    End Property
    Public Property Relation As ModEnumeration.RelationshipTypes
    Public ReadOnly Property HasValue As Boolean
        Get
            Return (_value IsNot Nothing)

        End Get
    End Property

    Public Property DefaultValue As Object
End Class




