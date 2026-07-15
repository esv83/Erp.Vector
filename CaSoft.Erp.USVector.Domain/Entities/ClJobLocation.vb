
' Lieu détaillé (prise en charge / dépose) exposé à l'UI ligne par ligne. Champs mappés « au mieux »
' depuis l'ERP (ErpStageDto). L'UI n'affiche que les champs non vides. Pour un lieu non référencé
' (structuré absent côté ERP), seul Nom est renseigné (= label figé).
Public Class ClJobLocation
    Public Property Nom As String = String.Empty
    ''' <summary>Service médical (ex. « Cardiologie »). Vide pour une adresse bénéficiaire.</summary>
    Public Property Service As String = String.Empty
    Public Property Adresse As String = String.Empty
    Public Property Residence As String = String.Empty
    Public Property BatEtage As String = String.Empty
    Public Property Commune As String = String.Empty
    Public Property Complement As String = String.Empty

    ' Coordonnées du lieu source, hors du flux d'affichage ligne par ligne (l'UI les
    ' consomme à part). Nothing tant que l'ERP n'a pas géocodé le lieu.
    Public Property Latitude As Double?
    Public Property Longitude As Double?
End Class
