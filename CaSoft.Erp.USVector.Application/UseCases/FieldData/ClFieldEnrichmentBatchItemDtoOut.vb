''' <summary>
''' Une mission d'un lot de paquets d'enrichissement (field-data en lot, B5) — demandé par la
''' facturation le 19/09 pour ne plus tirer un paquet par mission.
''' </summary>
''' <remarks>
''' Les deux exigences de la facturation : retrouver chaque paquet <b>par son MissionId</b>, et
''' distinguer « inconnue de Vector » (son 404 d'aujourd'hui, qui produit une note à l'écran) de
''' « en erreur » (à retenter). D'où <see cref="Status"/>, et une entrée par mission demandée — jamais
''' d'absence silencieuse.
''' </remarks>
Public Class ClFieldEnrichmentBatchItemDtoOut

    Public Const StatusFound As String = "Found"
    Public Const StatusNotFound As String = "NotFound"
    Public Const StatusError As String = "Error"

    Public Property MissionId As Guid

    ''' <summary>
    ''' <c>Found</c> : le paquet est dans <see cref="Data"/> · <c>NotFound</c> : mission inconnue
    ''' d'Orders (l'ancien 404) · <c>Error</c> : lecture impossible pour cette mission, à retenter.
    ''' </summary>
    Public Property Status As String

    ''' <summary>Le paquet, identique à celui de <c>GET api/missions/{id}/field-data</c>. Nothing sauf si Found.</summary>
    Public Property Data As ClFieldEnrichmentDtoOut

    ''' <summary>Motif, quand <see cref="Status"/> vaut Error. Nothing sinon.</summary>
    Public Property [Error] As String

    Public Shared Function Found(data As ClFieldEnrichmentDtoOut) As ClFieldEnrichmentBatchItemDtoOut
        Return New ClFieldEnrichmentBatchItemDtoOut With {.MissionId = data.MissionId, .Status = StatusFound, .Data = data}
    End Function

    Public Shared Function NotFound(missionId As Guid) As ClFieldEnrichmentBatchItemDtoOut
        Return New ClFieldEnrichmentBatchItemDtoOut With {.MissionId = missionId, .Status = StatusNotFound}
    End Function

    Public Shared Function Failed(missionId As Guid, reason As String) As ClFieldEnrichmentBatchItemDtoOut
        Return New ClFieldEnrichmentBatchItemDtoOut With {.MissionId = missionId, .Status = StatusError, .Error = reason}
    End Function

End Class
