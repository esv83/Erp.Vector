Imports System.Threading
' Liste des missions du personnel (crews actifs résolus du token Keycloak) — Result pattern.
Public Class ClGetJobListUseCase
    Implements IResultUseCaseAsync(Of ClJobListModel)

    Private ReadOnly _crewIds As IReadOnlyList(Of Guid)
    Private ReadOnly _repository As ICrewRepository

    ''' <summary>MOB-4a — crews actifs du personnel (résolus depuis le sub Keycloak).</summary>
    Public Sub New(crewIds As IReadOnlyList(Of Guid), repository As ICrewRepository)
        _crewIds = crewIds
        _repository = repository
    End Sub

    Public Async Function HandleAsync(ct As CancellationToken) As Task(Of ClResult(Of ClJobListModel)) Implements IResultUseCaseAsync(Of ClJobListModel).HandleAsync

        Dim jobList = Await _repository.FetchJobListAsync(_crewIds, ct)

        ' Instructions régulation : pas d'équivalent ERP (union sur les crews, vide en V1).
        Dim instructionList As New List(Of ClInstructionListItemModel)
        For Each crewId In _crewIds
            instructionList.AddRange(_repository.FetchInstructionList(crewId))
        Next

        Return ClResult(Of ClJobListModel).Ok(New ClJobListModel(jobList, instructionList))

    End Function

End Class
