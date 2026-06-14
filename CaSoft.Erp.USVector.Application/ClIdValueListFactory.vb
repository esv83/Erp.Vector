Public Module ModDataList

    Public Property ActorsList As ClIdValueList
    Public Property ConcerningList As ClIdValueList
    Public Property LogNatureList As ClIdValueList
    Public Property ActionTypeList As ClIdValueList

    Public Sub GenerateList()
        Dim actorData As New List(Of ClIdValue)
        actorData.Add(New ClIdValue(1, "Garage DIVOZZO"))
        actorData.Add(New ClIdValue(2, "Garage GROS PIN"))
        ActorsList = ClIdValueList.GetInstance(actorData)

        Dim concerningData As New List(Of ClIdValue)
        concerningData.Add(New ClIdValue(1, "Véhicule"))
        concerningData.Add(New ClIdValue(2, "Materiel"))
        ConcerningList = ClIdValueList.GetInstance(concerningData)

        Dim logNatureData As New List(Of ClIdValue)
        logNatureData.Add(New ClIdValue(1, "Entretien"))
        logNatureData.Add(New ClIdValue(2, "Casse"))
        logNatureData.Add(New ClIdValue(3, "Perte"))
        LogNatureList = ClIdValueList.GetInstance(logNatureData)


        Dim actionTypeData As New List(Of ClIdValue)
        actionTypeData.Add(New ClIdValue(1, "Aller au garage"))
        actionTypeData.Add(New ClIdValue(2, "Controle technique"))
        actionTypeData.Add(New ClIdValue(3, "Pneu"))
        ActionTypeList = ClIdValueList.GetInstance(actionTypeData)


    End Sub

End Module
