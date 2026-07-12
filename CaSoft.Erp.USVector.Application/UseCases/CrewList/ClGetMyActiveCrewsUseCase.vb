
''' <summary>
''' Sélecteur d'équipage actif du personnel — construit la réponse décision-complète servie à l'UI
''' (choix au login + changement d'équipage en cours de journée). Result pattern.
''' Les <paramref name="crewIds"/> sont déjà résolus depuis le token Keycloak (crews actifs du jour).
''' </summary>
Public Class ClGetMyActiveCrewsUseCase
    Implements IResultUseCase(Of ClActiveCrewSelectionDtoOut)

    Private ReadOnly _crewIds As IReadOnlyList(Of Guid)
    Private ReadOnly _at As DateTime
    Private ReadOnly _repository As ICrewRepository

    Public Sub New(crewIds As IReadOnlyList(Of Guid), at As DateTime, repository As ICrewRepository)
        _crewIds = crewIds
        _at = at
        _repository = repository
    End Sub

    Public Function Handle() As ClResult(Of ClActiveCrewSelectionDtoOut) Implements IResultUseCase(Of ClActiveCrewSelectionDtoOut).Handle

        Try
            ' Ne conserver que les équipages ACTIFS à l'instant demandé : commencé, non clôturé, non obsolète
            ' (règle domaine ClCrew.IsSelectableAt). Un crew non démarré / clôturé / expiré n'est pas sélectionnable.
            Dim crews As New List(Of ClActiveCrewDtoOut)
            For Each id In _crewIds
                Dim crew = _repository.GetCrew(id)
                If crew.IsSelectableAt(_at) Then
                    crews.Add(crew.ToActiveCrewDtoOut(_at))
                End If
            Next

            If crews.Count = 0 Then
                Return ClResult(Of ClActiveCrewSelectionDtoOut).Fail(
                    ClError.NotFound("Aucun équipage actif à cet instant (service non commencé, clôturé ou expiré)."))
            End If

            ' Pré-sélection : l'équipage qui couvre « maintenant » ; à défaut l'unique équipage.
            Dim recommended = crews.FirstOrDefault(Function(c) c.IsCurrent)
            If recommended Is Nothing AndAlso crews.Count = 1 Then recommended = crews(0)

            Dim selection As New ClActiveCrewSelectionDtoOut With {
                .RequiresSelection = crews.Count > 1,
                .RecommendedCrewId = If(recommended IsNot Nothing, CType(recommended.CrewId, Guid?), Nothing),
                .Crews = crews
            }

            Return ClResult(Of ClActiveCrewSelectionDtoOut).Ok(selection)

        Catch ex As Exception
            Return ClResult(Of ClActiveCrewSelectionDtoOut).Fail(ClError.Application(ex.Message, ex))
        End Try

    End Function

End Class
