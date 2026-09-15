
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
            ' Ne conserver que les équipages ACTIFS à l'instant demandé : ouvert (prise de service, ou les
            ' ClCrew.EarlyAccessMinutes min qui la précèdent), non clôturé, non obsolète — règle domaine
            ' ClCrew.IsSelectableAt. Un crew trop en avance / clôturé / expiré n'est pas sélectionnable.
            Dim crews As New List(Of ClActiveCrewDtoOut)
            Dim blocked As New List(Of ClCrew)
            For Each id In _crewIds
                Dim crew = _repository.GetCrew(id)
                If crew.IsSelectableAt(_at) Then
                    crews.Add(crew.ToActiveCrewDtoOut(_at))
                Else
                    blocked.Add(crew)
                End If
            Next

            If crews.Count = 0 Then
                Return ClResult(Of ClActiveCrewSelectionDtoOut).Fail(ClError.NotFound(UnselectableMessage(blocked, _at)))
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

    ''' <summary>
    ''' Message terrain quand l'équipage est composé mais qu'aucun n'est sélectionnable : il dit quoi
    ''' faire. Priorité à l'équipage qui ouvre le plus tôt — c'est celui que l'ambulancier attend —,
    ''' puis à la clôture, puis à l'expiration.
    ''' </summary>
    Private Shared Function UnselectableMessage(blocked As IReadOnlyCollection(Of ClCrew), at As DateTime) As String
        Dim upcoming = blocked.
            Where(Function(c) c.UnselectableReasonAt(at) = EnCrewUnselectableReason.NotYetOpen).
            OrderBy(Function(c) c.ServiceStart).
            FirstOrDefault()
        If upcoming IsNot Nothing Then
            Dim jour = If(upcoming.ServiceStart.Date = at.Date, String.Empty, $" le {upcoming.ServiceStart:dd/MM}")
            Return $"Votre service commence{jour} à {upcoming.ServiceStart:HH:mm} : vos missions seront accessibles à partir de {upcoming.SelectableFrom:HH:mm}."
        End If

        If blocked.Any(Function(c) c.UnselectableReasonAt(at) = EnCrewUnselectableReason.Closed) Then
            Return "Votre service est clôturé : il n'y a plus de mission à afficher. Si c'est une erreur, appelez la régulation."
        End If

        If blocked.Any(Function(c) c.UnselectableReasonAt(at) = EnCrewUnselectableReason.Expired) Then
            Return $"Votre vacation a commencé il y a plus de {ClCrew.MaxServiceDurationHours} h sans être clôturée. Appelez la régulation pour la corriger."
        End If

        Return "Aucun équipage sélectionnable pour le moment. Réessayez dans quelques minutes ; si rien ne change, appelez la régulation."
    End Function

End Class
