Namespace Port

    ''' <summary>
    ''' Persistance des anomalies terrain (TRF-8, BD Mobile). Rattachées à la mission ; historisées.
    ''' Non bloquantes — transférées dans le paquet field-data, arbitrées par la facturation.
    ''' </summary>
    Public Interface IAnomalyRepository

        ''' <summary>Enregistre une anomalie (l'Id est porté par <paramref name="anomaly"/>).</summary>
        Sub Save(anomaly As ClAnomaly)

        ''' <summary>Anomalies d'une mission, de la plus récente à la plus ancienne.</summary>
        Function ListByMission(missionId As Guid) As IReadOnlyList(Of ClAnomaly)

    End Interface

End Namespace
