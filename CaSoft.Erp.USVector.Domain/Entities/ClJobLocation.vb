
' Lieu détaillé (prise en charge / dépose) exposé à l'UI ligne par ligne. Champs mappés « au mieux »
' depuis l'ERP (ErpStageDto). L'UI n'affiche que les champs non vides. Pour un lieu non référencé
' (structuré absent côté ERP), seul Nom est renseigné (= label figé).
Public Class ClJobLocation
    Public Property Nom As String = String.Empty
    ''' <summary>Service médical (ex. « Cardiologie ») — établissement de santé / lieu FreeText. DET-1 : champ dédié (avant, concaténé dans BatEtage).</summary>
    Public Property Service As String = String.Empty
    Public Property Adresse As String = String.Empty
    Public Property Residence As String = String.Empty
    Public Property BatEtage As String = String.Empty
    Public Property Commune As String = String.Empty
    Public Property Complement As String = String.Empty
    ''' <summary>Lignes prêtes à afficher (ordre d'affichage, champs vides filtrés) — homogène quel que soit le
    ''' type de lieu (établissement ou non référencé). L'UI rend ces lignes telles quelles, sans logique
    ''' champ-à-champ ni cas « une seule ligne ».</summary>
    Public Property DisplayLines As List(Of String) = New List(Of String)
End Class
