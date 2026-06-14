
Public MustInherit Class ClReliableValue(Of T)

    Private _valueInfoList As List(Of ClValueInfo(Of T))

    Public Sub New()
        _valueInfoList = New List(Of ClValueInfo(Of T))
        _Value = Nothing
        _Level = ValueReliableLevel.Null
    End Sub

    Public Sub AddValueInfo(info As ClValueInfo(Of T))
        _valueInfoList.Add(info)
    End Sub

    Public MustOverride Sub AnalyzeInfo()
    Protected Sub SetValue(value As T, level As ValueReliableLevel)
        _Value = value
        _Level = level
    End Sub



    Public ReadOnly Property ValueInfos As List(Of ClValueInfo(Of T))
        Get
            Return _valueInfoList
        End Get
    End Property

    Public ReadOnly Property Value As T
    Public ReadOnly Property Level As ValueReliableLevel
    Public ReadOnly Property HasValue As Boolean
        Get
            Return _Value IsNot Nothing
        End Get
    End Property

    Public Function Contains(strAttribut As String) As Boolean

        Dim value = _valueInfoList.FirstOrDefault(Function(f) f.Attribut = strAttribut)
        Return value IsNot Nothing

    End Function

End Class
Public Enum ValueReliableLevel
    Null
    Bad
    Approx
    Max
End Enum



