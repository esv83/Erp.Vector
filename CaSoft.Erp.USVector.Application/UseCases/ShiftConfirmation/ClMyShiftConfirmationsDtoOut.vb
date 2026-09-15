''' <summary>
''' Ce qui attend la confirmation de prise de service de l'ambulancier connecté — lu par l'application
''' à son lancement, pour afficher « confirmation en attente » même si le lien n'est jamais arrivé.
''' </summary>
''' <remarks>
''' ⚖️ <b>Aucun jeton, et c'est voulu.</b> Order n'en garde que l'empreinte ; en obtenir un obligerait à
''' redélivrer le lien, ce qui tuerait celui du courriel. L'ambulancier est authentifié : il confirme
''' par <c>POST api/ShiftConfirmation/{crewId}/{requestId}/confirm</c>.
''' </remarks>
Public Class ClMyShiftConfirmationsDtoOut

    ''' <summary>Vrai s'il reste au moins une prise de service à confirmer.</summary>
    Public Property HasPending As Boolean

    ''' <summary>Les demandes en attente, la plus proche d'abord. Vide si rien n'attend.</summary>
    Public Property Pending As List(Of ClPendingShiftConfirmationDtoOut) = New List(Of ClPendingShiftConfirmationDtoOut)()

End Class

''' <summary>Une prise de service qui attend la confirmation de l'ambulancier.</summary>
Public Class ClPendingShiftConfirmationDtoOut

    ''' <summary>À renvoyer tel quel dans la route de confirmation.</summary>
    Public Property RequestId As Guid

    ''' <summary>À renvoyer tel quel dans la route de confirmation.</summary>
    Public Property CrewId As Guid

    Public Property CrewLabel As String

    ''' <summary>Heure de prise de service proposée — <b>locale</b>, prête à afficher.</summary>
    Public Property ProposedLocalTime As DateTime

End Class
