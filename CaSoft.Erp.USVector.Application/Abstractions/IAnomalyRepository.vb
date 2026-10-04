Imports System.Threading

Namespace Port

    ''' <summary>
    ''' Persistance des anomalies terrain (TRF-8, BD Mobile). Rattachées à la mission ; historisées.
    ''' Non bloquantes — transférées dans le paquet field-data, arbitrées par la facturation.
    ''' </summary>
    Public Interface IAnomalyRepository

        ''' <summary>Enregistre une anomalie (l'Id est porté par <paramref name="anomaly"/>).</summary>
        Function SaveAsync(anomaly As ClAnomaly, ct As CancellationToken) As Task

        ''' <summary>Anomalies d'une mission, de la plus récente à la plus ancienne.</summary>
        Function ListByMissionAsync(missionId As Guid, ct As CancellationToken) As Task(Of IReadOnlyList(Of ClAnomaly))

    End Interface

End Namespace
