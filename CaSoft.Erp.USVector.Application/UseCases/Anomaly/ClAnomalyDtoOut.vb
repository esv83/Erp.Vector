''' <summary>DTO de sortie d'une anomalie terrain (TRF-8).</summary>
Public Class ClAnomalyDtoOut
    Public Property Id As Guid
    Public Property MissionId As Guid
    ''' <summary>Nature (cf. EnAnomalyType : 1=tél, 2=adresse, 3=patient, 4=admin, 5=impossibilité).</summary>
    Public Property Type As Integer
    Public Property Text As String
    Public Property ReportedAt As DateTime
End Class
