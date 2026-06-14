Public MustInherit Class ClGeoPointBase

    Protected _value As Double?

    Public ReadOnly Property IsEmpty As Boolean
        Get
            Return Not Value.HasValue
        End Get
    End Property
    Public MustOverride Sub SetValue(Coordonate As Double)


    Public ReadOnly Property Value As Double?
        Get
            Return _value
        End Get
    End Property

    Public Overloads Function ToString()
        Dim result As String = String.Empty
        If _value.HasValue Then
            result = _value.Value.ToString
        End If
        Return result

    End Function

End Class

