Imports System.Threading

Namespace Port

    ''' <summary>
    ''' TRF-6 — Assemble le paquet d'enrichissement terrain consolidé d'une mission (tous silos BD
    ''' Mobile + rattachement commande), tiré par le module Certification au transfert en facturation.
    ''' </summary>
    Public Interface IFieldDataReader

        ''' <summary>Paquet consolidé de la mission, ou Nothing si la mission est introuvable côté ERP.</summary>
        Function GetAsync(missionId As Guid, ct As CancellationToken) As Task(Of ClFieldEnrichmentDtoOut)

    End Interface

End Namespace
