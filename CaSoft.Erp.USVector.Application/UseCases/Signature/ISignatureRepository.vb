Public Interface ISignatureRepository

    Function Fetch(jobId As Guid) As ClSignatureDto
    Sub Insert(gJobId As Guid, strSignData As String)
    Sub Update(gJobId As Guid, strSignData As String)
    Sub Delete(gJobId As Guid, strSignData As String)

    ''' <summary>MOB-8 — Présence d'une signature (clé seule, sans charger le base64).</summary>
    Function Exists(jobId As Guid) As Boolean

    ''' <summary>MOB-8 — Sous-ensemble des missions disposant d'une signature (overlay liste, 1 requête).</summary>
    Function ExistingFor(jobIds As IEnumerable(Of Guid)) As HashSet(Of Guid)

End Interface
