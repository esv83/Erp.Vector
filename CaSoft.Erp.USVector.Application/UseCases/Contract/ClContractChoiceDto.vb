''' <summary>
''' MOB-13.8 — Type de contrat proposé à la sélection pour une mission.
''' </summary>
Public Class ClContractChoiceDto
    Public Property Id As Integer
    Public Property Display As String
    ''' <summary>Contrat effectif de la mission (choisi, ou défaut si aucun choix explicite).</summary>
    Public Property IsSelected As Boolean
End Class
