''' <summary>
''' B5 — Lecture groupée des signatures : une requête pour tout le lot, au lieu d'une par mission.
''' </summary>
Public Interface ISignatureQueryService

    ''' <summary>
    ''' Une entrée par mission demandée, dédoublonnée, dans l'ordre de la demande : <c>Found</c> avec
    ''' l'image, ou <c>NotFound</c>.
    ''' </summary>
    Function ReadMany(missionIds As IReadOnlyCollection(Of Guid)) As IReadOnlyList(Of ClSignatureBatchItemDtoOut)

End Interface
