Imports System.Threading

Public Interface ISignatureRepository

    Function FetchAsync(jobId As Guid, ct As CancellationToken) As Task(Of ClSignatureDtoOut)
    Function InsertAsync(gJobId As Guid, strSignData As String, ct As CancellationToken) As Task
    Function UpdateAsync(gJobId As Guid, strSignData As String, ct As CancellationToken) As Task
    Function DeleteAsync(gJobId As Guid, strSignData As String, ct As CancellationToken) As Task

    ''' <summary>MOB-8 — Présence d'une signature (clé seule, sans charger le base64).</summary>
    Function ExistsAsync(jobId As Guid, ct As CancellationToken) As Task(Of Boolean)

    ''' <summary>MOB-8 — Sous-ensemble des missions disposant d'une signature (overlay liste, 1 requête).</summary>
    Function ExistingForAsync(jobIds As IEnumerable(Of Guid), ct As CancellationToken) As Task(Of HashSet(Of Guid))

End Interface
