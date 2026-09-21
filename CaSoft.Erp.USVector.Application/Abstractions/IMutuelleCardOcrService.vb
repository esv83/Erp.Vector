Imports System.Threading

Namespace Port

    ''' <summary>
    ''' P3 — Lit une carte mutuelle et <b>propose</b> ses quatre champs. L'implémentation appelle un
    ''' modèle de vision ; l'Application ne sait ni lequel, ni où il tourne.
    ''' </summary>
    Public Interface IMutuelleCardOcrService

        ''' <summary>
        ''' Rend les champs lus sur l'image. <b>Ne lève pas pour une carte illisible</b> : une photo
        ''' floue rend une proposition vide, pas une panne — seul un échec technique (réseau, service
        ''' indisponible) lève, et c'est la file qui décide alors de retenter.
        ''' </summary>
        Function ExtractAsync(image As Byte(), contentType As String, ct As CancellationToken) As Task(Of ClMutuelleCardOcrProposal)

    End Interface

End Namespace
