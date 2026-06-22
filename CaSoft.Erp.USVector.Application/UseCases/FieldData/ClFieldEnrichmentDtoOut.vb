''' <summary>
''' TRF-6 — Paquet d'enrichissement terrain consolidé d'une mission, tiré par le module
''' Certification au transfert en facturation. Agrège tous les silos BD Mobile + rattachement
''' commande. <see cref="UpdatedAt"/> = watermark global (re-synchro : re-tirer si modifié et
''' mission pas encore Transférée). Les binaires (signature/mutuelle/documents) sont servis par
''' Vector.Api via leurs URL relatives.
''' </summary>
Public Class ClFieldEnrichmentDtoOut
    Public Property MissionId As Guid
    Public Property OrderId As Guid
    ''' <summary>Version du schéma du paquet (contrat stable/versionné).</summary>
    Public Property SchemaVersion As Integer = 1
    ''' <summary>Max des horodatages des silos. Nothing si aucun enrichissement.</summary>
    Public Property UpdatedAt As DateTime?

    Public Property Timeline As ClFieldTimelineDto
    Public Property Signature As ClFieldSignatureDto
    Public Property Attributes As ClFieldAttributesDto
    ''' <summary>Carte mutuelle courante du bénéficiaire (Nothing si aucune).</summary>
    Public Property Mutuelle As ClMutuelleCardDtoOut
    ''' <summary>
    ''' Kilométrage : <c>Nothing</c> au MVP. Le km est crew/véhicule-scoped (odomètre du véhicule
    ''' de l'équipage, cf. KilometersController), pas un attribut de mission — surfacé séparément.
    ''' </summary>
    Public Property Kilometers As Integer?
    Public Property Documents As IReadOnlyList(Of ClDocumentDtoOut)
    Public Property Anomalies As IReadOnlyList(Of ClAnomalyDtoOut)
End Class

''' <summary>Jalons opérationnels terrain (TRF-3, BD Mobile).</summary>
Public Class ClFieldTimelineDto
    Public Property AckAt As DateTime?
    Public Property ReadAt As DateTime?
    Public Property GoAt As DateTime?
    Public Property OnsiteAt As DateTime?
    Public Property TerminateAt As DateTime?
End Class

''' <summary>Signature patient (présence + url du binaire).</summary>
Public Class ClFieldSignatureDto
    Public Property Exists As Boolean
    Public Property SignedAt As DateTime?
    Public Property ImageUrl As String
End Class

''' <summary>Attributs de facturation dynamiques saisis (overlay MOB-13).</summary>
Public Class ClFieldAttributesDto
    Public Property ContractId As Integer
    Public Property ContractDisplay As String
    Public Property Values As IReadOnlyList(Of ClFieldAttributeValueDto)
End Class

Public Class ClFieldAttributeValueDto
    Public Property Name As String
    Public Property Value As String
End Class
