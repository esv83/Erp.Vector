' Liste des missions du personnel (crews actifs résolus du token Keycloak) — Result pattern.
Public Class ClGetJobListUseCase
    Implements IResultUseCase(Of ClJobListModel)

    Private ReadOnly _crewIds As IReadOnlyList(Of Guid)
    Private ReadOnly _repository As ICrewRepository

    ''' <summary>MOB-4a — crews actifs du personnel (résolus depuis le sub Keycloak).</summary>
    Public Sub New(crewIds As IReadOnlyList(Of Guid), repository As ICrewRepository)
        _crewIds = crewIds
        _repository = repository
    End Sub

    Public Function Handle() As ClResult(Of ClJobListModel) Implements IResultUseCase(Of ClJobListModel).Handle

        Try
            Dim jobList = _repository.FetchJobList(_crewIds)

            ' Instructions régulation : pas d'équivalent ERP (union sur les crews, vide en V1).
            Dim instructionList As New List(Of ClInstructionListItemModel)
            For Each crewId In _crewIds
                instructionList.AddRange(_repository.FetchInstructionList(crewId))
            Next

            Return ClResult(Of ClJobListModel).Ok(New ClJobListModel(jobList, instructionList))

        Catch ex As Exception
            Return ClResult(Of ClJobListModel).Fail(ClError.Application(ex.Message, ex))
        End Try

    End Function

End Class
