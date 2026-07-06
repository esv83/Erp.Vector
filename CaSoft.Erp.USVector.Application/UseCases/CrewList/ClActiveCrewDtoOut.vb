''' <summary>
''' MOB — Un équipage actif présentable dans le sélecteur de crew (choix au login ET changement
''' d'équipage en cours de journée). Tout est <b>prêt à afficher</b> : l'UI ne recompose rien
''' (ni libellé, ni fenêtre de service, ni « lequel couvre maintenant »). Objectif : dépendance
''' minimale au dev Web.
''' </summary>
Public Class ClActiveCrewDtoOut
    Public Property CrewId As Guid
    ''' <summary>Libellé composé prêt à afficher, ex. « AB-123-CD · DUPONT Jean / MARTIN Paul ».</summary>
    Public Property DisplayLabel As String = String.Empty
    Public Property VehicleImmat As String = String.Empty
    ''' <summary>Membres de l'équipage, déjà joints, ex. « DUPONT Jean / MARTIN Paul ».</summary>
    Public Property Members As String = String.Empty
    ''' <summary>Fenêtre de vacation déjà formatée, ex. « 06:00 – 14:00 » (fin ouverte : « 06:00 – … »).</summary>
    Public Property ServiceWindow As String = String.Empty
    ''' <summary>Vrai si la vacation couvre l'instant présent (véhicule « du moment » → à pré-cocher).</summary>
    Public Property IsCurrent As Boolean
    ''' <summary>Vrai si la vacation est terminée (équipage en lecture seule : toute modif est refusée côté ERP).</summary>
    Public Property IsClosed As Boolean
End Class
