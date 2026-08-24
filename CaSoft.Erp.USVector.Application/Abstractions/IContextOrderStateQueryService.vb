Imports System.Threading

Namespace Port

    ''' <summary>
    ''' OC-3a — Lecture de l'état du <b>context de la mission</b> côté ERP (verrou + provenance),
    ''' pour que le terrain sache s'il a la main <b>avant</b> de tenter d'écrire.
    ''' <para>
    ''' Sans cette lecture, l'ambulancier découvrirait le verrou par un refus : il choisit un type,
    ''' l'API répond 409, et rien dans l'écran ne lui avait dit que le choix était gelé.
    ''' </para>
    ''' </summary>
    Public Interface IContextOrderStateQueryService

        ''' <summary>
        ''' État du context de la mission, ou Nothing si la mission est introuvable côté ERP.
        ''' </summary>
        Function GetAsync(missionId As Guid, ct As CancellationToken) As Task(Of ClContextOrderStateDtoOut)

    End Interface

End Namespace
