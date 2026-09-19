Imports System.Threading

Namespace Port

    Public Interface IFieldDataReader

        ''' <summary>Paquet d'une mission. Nothing si la mission est inconnue d'Orders.</summary>
        Function GetAsync(missionId As Guid, ct As CancellationToken) As Task(Of ClFieldEnrichmentDtoOut)

        ''' <summary>
        ''' Paquets de plusieurs missions (B5) : une entrée par mission demandée, dédoublonnée, avec son
        ''' statut. Un échec sur une mission n'emporte pas le lot.
        ''' </summary>
        Function GetManyAsync(missionIds As IReadOnlyCollection(Of Guid), ct As CancellationToken) As Task(Of IReadOnlyList(Of ClFieldEnrichmentBatchItemDtoOut))

    End Interface

End Namespace
