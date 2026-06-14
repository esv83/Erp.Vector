Public Class ClUpdateJobEditCommand
    Inherits ClJobQueryWithCache

    Public Sub New(gJobId As Guid,  attributsValues As List(Of ClAttributValueModel))
        MyBase.New(gJobId, Nothing)

        _NewAttributsValues = attributsValues

    End Sub

    Public Property NewAttributsValues As List(Of ClAttributValueModel)

End Class

