''' <summary>
''' MOB-13.8 — Type de contrat proposé à la sélection pour une mission.
''' </summary>
Public Class ClContractChoiceDto
    Public Property Id As Integer
    Public Property Display As String
    ''' <summary>Contrat effectif de la mission (choisi, ou défaut si aucun choix explicite).</summary>
    Public Property IsSelected As Boolean

    ''' <summary>
    ''' OC-3a — Ajout <b>additif</b> (D14) : vrai si le context est gelé côté ERP, auquel cas
    ''' l'ambulancier n'a pas la main et une sélection serait refusée (409).
    ''' <para>
    ''' Le verrou porte sur <b>la mission</b>, pas sur le type : tous les items d'une même mission
    ''' portent donc la même valeur. Elle est répétée sur chaque item pour que le front puisse
    ''' griser la liste sans second appel — <c>GET api/Contract/{jobId}/state</c> reste disponible
    ''' pour qui veut l'état seul, avec la provenance en plus.
    ''' </para>
    ''' </summary>
    Public Property Locked As Boolean
End Class
