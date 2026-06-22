Imports System.Runtime.CompilerServices

''' <summary>Mapping Application : métier → DTO (anomalie terrain).</summary>
Public Module ModAnomalyMapping

    <Extension>
    Public Function ToDtoOut(anomaly As ClAnomaly) As ClAnomalyDtoOut
        Return New ClAnomalyDtoOut With {
            .Id = anomaly.Id,
            .MissionId = anomaly.MissionId,
            .Type = CInt(anomaly.Type),
            .Text = anomaly.Text,
            .ReportedAt = anomaly.ReportedAt
        }
    End Function

End Module
