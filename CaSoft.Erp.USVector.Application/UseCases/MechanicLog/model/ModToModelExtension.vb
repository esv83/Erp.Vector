Imports System.Runtime.CompilerServices

Public Module ModToModelExtension

#Region "Mechanic"
    <Extension()>
    Public Function ToLogAnalyze(model As ClEditLogAnalyzeModel) As ClLogAnalyze
        Dim result As New ClLogAnalyze
        With result
            .Analyze = model.Report
            .AnalyzeBy = model.AnalyzeBy
            .Concerning = ModDataList.ConcerningList.GetById(model.ConcerningId)
            '.Crew = model.Crew
            .ImmobilizeVehicle = model.ImmobilizeVehicle
            .LogId = model.LogId
            .Nature = ModDataList.LogNatureList.GetById(model.NatureId)
            .AddActionRange(model.Actions.ToActionList)
        End With

        Return result

    End Function

    <Extension()>
    Public Function ToLogAnalyze(model As ClGetLogAnalyzeModel) As ClLogAnalyze
        Dim result As New ClLogAnalyze
        With result
            .Analyze = model.Report
            .AnalyzeBy = model.AnalyzeBy
            '.Concerning = model.ConcerningId
            '.Crew = model.Crew
            .Date = model.Date
            .ImmobilizeVehicle = model.ImmobilizeVehicle
            .LogId = model.LogId
            ' .Nature = model.Nature
            '.AddRange(model.Actions.to)
        End With

        Return result

    End Function

    <Extension()>
    Public Function ToLogAnalyzeModel(model As ClLogAnalyze) As ClGetLogAnalyzeModel
        Dim result As New ClGetLogAnalyzeModel
        With result
            .Report = model.Analyze
            .AnalyzeBy = model.AnalyzeBy
            .ConcerningId = model.Concerning.Id
            .Crew = model.Crew.ToString
            .Date = model.Date
            .Immatriculation = model.Crew.Vehicle.Immatriculation
            .ImmobilizeVehicle = model.ImmobilizeVehicle
            .LogId = model.LogId
            .NatureId = model.Nature.Id
            .Actions = model.ActionsList.ToGetActionListModel  '.AddRange(model.Actions.ToActionListModel)
        End With

        Return result




    End Function

    <Extension()>
    Public Function ToConstraintModel(constraint As ClConstraintType) As ClConstraintModel
        Dim result As New ClConstraintModel(constraint.Id, constraint.Label, constraint.DateRequired)



        Return result
    End Function


    <Extension()>
    Public Function ToGetActionListModel(actionList As ClActionsList) As List(Of ClGetActionModel)
        Dim actionListModel As New List(Of ClGetActionModel)
        For Each action In actionList

            Dim actionTypeModel = action.ActionType.ToIdValueModel
            Dim actorModel = action.Actor.ToIdValueModel
            Dim constraintModel = action.Constraint.ToConstraintModel
            Dim actionModel = action.ToGetActionModel(actionTypeModel, actorModel, constraintModel)
            actionListModel.Add(actionModel)

        Next

        Return actionListModel
    End Function





    <Extension()>
    Public Function ToLogEntry(obj As ClLogEntryModel) As ClLogEntry
        Dim log = ClLogEntry.GetBuilder.WithId(obj.LogId) _
            .WithLogDate(obj.LogDate) _
            .WithReport(obj.Report) _
            .Build

        Return log

    End Function


    <Extension()>
    Public Function ToLogEntryModel(obj As ClLogEntry) As ClLogEntryModel
        Dim model As New ClLogEntryModel
        With model

            '    .Constat = log.Report
            '    .DeclaredDate = log.LogDate
            '    .Id = log.Id
            '    .LastStateDate = log.LastStateDate

            .LogId = obj.Id
            .Immatriculation = obj.Crew.Vehicle.Immatriculation
            .Crew = obj.Crew.ToString
            .LogDate = obj.LogDate
            .Report = obj.Report
            .ReportState = obj.State
            If obj.HasAnalyze Then
                .Analyse = obj.Analyze.Detail
                .Action = obj.Analyze.ActionsList.GetActionText()
                .NextDeadLine = obj.Analyze.ActionsList.NextDeadLine
            End If

        End With

        Return model
    End Function


    <Extension()>
    Public Function ToActionList(model As List(Of ClEditActionModel)) As ClActionsList
        Dim result As New ClActionsList
        For Each actionModel In model
            result.Add(actionModel.ToAction)
        Next
        Return result

    End Function


    <Extension()>
    Public Function ToGetActionModel(action As ClAnalyzeAction, actionType As ClIdValueModel, actor As ClIdValueModel, constraint As ClConstraintModel) As ClGetActionModel
        Dim result As New ClGetActionModel
        With result
            .ActionType = actionType
            .Actor = actor
            .AnalyzeId = action.AnalyzeId
            .CallBack = action.CallBack
            .Closed = action.StateDate
            .comment = action.Comment
            .Constraint = constraint
            .Creation = action.Creation
            .DueDate = action.DueDate

        End With

        Return result

    End Function


    <Extension()>
    Public Function ToIdValueModel(item As ClIdValue) As ClIdValueModel

        Return New ClIdValueModel(item.Id, item.Display)

    End Function

    <Extension()>
    Public Function ToAction(model As ClEditActionModel) As ClAnalyzeAction
        Dim result As New ClAnalyzeAction
        With result
            .ActionType = ModDataList.ActionTypeList.GetById(model.ActionTypeId)
            .Actor = ModDataList.ActorsList.GetById(model.ActorId)
            .AnalyzeId = 1
            .CallBack = model.CallBackDate
            .StateDate = model.StateDate
            .Comment = model.Comment
            '.Constraint = model.Constraint
            .DueDate = model.DueDate
        End With

        Return result

    End Function

    <Extension()>
    Public Function ToIdValue(obj As ClIdValueModel) As ClIdValue
        Dim result As New ClIdValue(obj.Id, obj.Value)

        Return result

    End Function

#End Region



    <Extension()>
    Public Function ToJobTimeModel(jobTime As ClJobTimeData) As ClJobTimeModel
        Dim jobTimeModel As New ClJobTimeModel
        With jobTimeModel
            .GoTime = New ClTimeFormatAdapter(jobTime.GoTime).ToString
            .OnSiteTime = New ClTimeFormatAdapter(jobTime.OnSiteTime).ToString
            .TerminatedTime = New ClTimeFormatAdapter(jobTime.TerminateTime).ToString
        End With
        Return jobTimeModel
    End Function

End Module
