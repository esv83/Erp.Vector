''' <summary>DTO d'entrée pour signaler une anomalie terrain (TRF-8).</summary>
Public Class ClReportAnomalyDtoIn
    ''' <summary>Nature (cf. EnAnomalyType : 1=tél, 2=adresse, 3=patient, 4=admin, 5=impossibilité).</summary>
    Public Property Type As Integer
    Public Property Text As String
    ''' <summary>Équipage déclarant (traçabilité). Optionnel.</summary>
    Public Property CrewId As Guid?
End Class
