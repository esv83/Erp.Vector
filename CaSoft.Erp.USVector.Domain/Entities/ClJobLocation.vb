
' Lieu détaillé (prise en charge / dépose) exposé à l'UI ligne par ligne. Champs mappés « au mieux »
' depuis l'ERP (ErpStageDto). L'UI n'affiche que les champs non vides. Pour un lieu non référencé
' (structuré absent côté ERP), seul Nom est renseigné (= label figé).
Public Class ClJobLocation
    Public Property Nom As String = String.Empty
    Public Property Adresse As String = String.Empty
    Public Property Residence As String = String.Empty
    Public Property BatEtage As String = String.Empty
    Public Property Commune As String = String.Empty
    Public Property Complement As String = String.Empty
End Class
