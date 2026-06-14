Public Class ClContractTypeGetModel
    Private _values As List(Of String)

    Public Sub New(ByVal intId As Integer, ByVal strName As String, ByVal bHasPmt As Boolean, ByVal bHasRef As Boolean, ByVal strRefLabel As String, ByVal Values As List(Of String))
        _values = New List(Of String)()

        If Values IsNot Nothing Then
            _values.AddRange(Values)
        End If

        Id = intId
        Name = strName
        HasPmt = bHasPmt
        HasReference = bHasRef
        ReferenceLabel = strRefLabel
    End Sub

    Public Property Id As Integer
    Public Property Name As String
    Public Property HasPmt As Boolean
    Public Property HasReference As Boolean
    Public Property ReferenceLabel As String

    Public ReadOnly Property Values As List(Of String)
        Get
            Return _values
        End Get
    End Property
End Class

