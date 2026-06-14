
Imports System.ComponentModel

Public Class ClEntityBase

    ' Identifiant générique attendu par le code porté (merge/synchronisation par Id,
    ' cf. ClUpdateLogAnalyzeUseCase). Les entités qui déclarent leur propre Id typé
    ' (Integer/Guid) le masquent explicitement avec Shadows.
    Public Property Id As Object

End Class

