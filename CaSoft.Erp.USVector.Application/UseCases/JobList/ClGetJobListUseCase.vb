Public Class ClGetJobListUseCase
    Inherits ClUseCaseBase
    Implements IUseCase

    Private _crewIds As IReadOnlyList(Of Guid)
    Private _repository As ICrewRepository


    ''' <summary>MOB-4a — crews actifs du personnel (résolus depuis le sub Keycloak).</summary>
    Public Sub New(crewIds As IReadOnlyList(Of Guid), repository As ICrewRepository)
        _crewIds = crewIds
        _repository = repository

    End Sub

    Public Overrides Sub execute(presenter As IResponseHandler) Implements IUseCase.Execute

        If CanExecute() Then

            Try

                Dim jobList = _repository.FetchJobList(_crewIds)

                ' Instructions régulation : pas d'équivalent ERP (union sur les crews, vide en V1).
                Dim instructionList As New List(Of ClInstructionListItemModel)
                For Each crewId In _crewIds
                    instructionList.AddRange(_repository.FetchInstructionList(crewId))
                Next

                Response.SetResult(New ClJobListModel(jobList, instructionList))

            Catch ex As Exception
                Response.AddError(ex.Message)
            Finally
                presenter.Handle(Response)
            End Try

        End If

    End Sub

    Public Overrides Sub Before()

    End Sub
End Class
