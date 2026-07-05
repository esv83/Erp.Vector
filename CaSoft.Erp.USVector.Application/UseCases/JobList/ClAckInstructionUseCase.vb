' Acquittement d'une instruction régulation — Result pattern. (Instructions non exposées en V1.)
Friend Class ClAckInstructionUseCase
    Implements IResultUseCase(Of Boolean)

    Private ReadOnly _query As Integer
    Private ReadOnly _repository As ICrewRepository

    Public Sub New(query As Integer, repository As ICrewRepository)
        _query = query
        _repository = repository
    End Sub

    Public Function Handle() As ClResult(Of Boolean) Implements IResultUseCase(Of Boolean).Handle

        Try
            _repository.AckInstruction(_query)
            Return ClResult(Of Boolean).Ok(True)
        Catch ex As Exception
            Return ClResult(Of Boolean).Fail(ClError.Application(ex.Message, ex))
        End Try

    End Function

End Class
