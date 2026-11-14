''' <summary>
''' J-127 — Le patient de la mission est né anonyme : son état d'identification, tel qu'Order le
''' tient. Absent (Nothing) pour une commande ordinaire.
''' </summary>
Public Class ClBeneficiaryIdentification
    ''' <summary>1 anonyme · 2 à vérifier (saisie terrain en attente) · 3 identifié.</summary>
    Public Property State As Integer
    ''' <summary>Signalement de la régulation ; peut être Nothing.</summary>
    Public Property Description As String
    ''' <summary>Faux dès que la régulation a tranché : formulaire d'identité masqué (J-129).</summary>
    Public Property IdentityEditableByField As Boolean
End Class
