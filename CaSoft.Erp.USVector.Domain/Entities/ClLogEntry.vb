Imports CaSoft.Framework

Public Class ClLogEntry
    Inherits ClBusinessBase(Of ClLogEntry)



    Private Sub New()

    End Sub

    Friend idProperty = RegisterProperty(Of Integer)(Function(f) f.Id)
    Public Shadows Property Id As Integer
        Get
            Return GetProperty(idProperty)
        End Get
        Friend Set(value As Integer)
            SetProperty(idProperty, value)
        End Set
    End Property


    Private logDateProperty = RegisterProperty(Of DateTime)(Function(f) f.LogDate)
    Public Property LogDate As DateTime
        Get
            Return GetProperty(logDateProperty)
        End Get
        Set(value As DateTime)
            SetProperty(logDateProperty, value)
        End Set
    End Property

    Friend lastStateDateProperty = RegisterProperty(Of DateTime)(Function(f) f.LastStateDate)
    Public Property LastStateDate As DateTime
        Get
            Return GetProperty(lastStateDateProperty)
        End Get
        Set(value As DateTime)
            SetProperty(lastStateDateProperty, value)
        End Set
    End Property

    Friend vehicleProperty = RegisterProperty(Of ClVehicle)(Function(f) f.Vehicle)
    Public Property Vehicle As ClVehicle
        Get
            Return GetProperty(vehicleProperty)
        End Get
        Friend Set(value As ClVehicle)
            SetProperty(vehicleProperty, value)
        End Set
    End Property
    Public ReadOnly Property State As String
        Get
            Dim result As String = "Nouveaux"
            If HasAnalyze Then

                If Analyze.ActionsList.Count > 0 Then

                    'au moins une action en cours
                    If Analyze.ActionsList.OpenActions > 0 Then
                        result = "En Cours"
                    ElseIf Analyze.ActionsList.IsAllActionsClosed Then
                        result = "Cloturé"
                    End If
                Else
                    ' pas encore d'action
                    result = "Pris en compte"

                End If

            End If

            Return result

        End Get

    End Property

    Friend analyzeProperty = RegisterProperty(Of ClLogAnalyze)(Function(f) f.Analyze, RelationshipTypes.Child)
    Public ReadOnly Property Analyze As ClLogAnalyze

    Public ReadOnly Property HasAnalyze As Boolean
        Get
            Return Analyze IsNot Nothing
        End Get
    End Property

    Friend crewProperty = RegisterProperty(Of String)(Function(f) f.Crew)
    Public Property Crew As ClCrew
        Get
            Return GetProperty(crewProperty)
        End Get
        Set(value As ClCrew)
            SetProperty(crewProperty, value)
        End Set
    End Property
    Public Property Report As String

    Public Sub AddAnalyze(pAnalyze As ClLogAnalyze)
        _Analyze = pAnalyze
    End Sub

    Public Shared Function GetBuilder() As ClLogEntryBuilder
        Return New ClLogEntryBuilder()
    End Function

    Public Class ClLogEntryBuilder
        Inherits ClBuilderBase(Of ClLogEntry)
        Public Sub New()
            MyBase.New(New ClLogEntry)
        End Sub
        Public Function WithId(intId As Integer) As ClLogEntryBuilder
            _businessBase.Id = intId
            Return Me
        End Function
        Public Function WithLogDate(dteLogDate As DateTime) As ClLogEntryBuilder
            _businessBase.LogDate = dteLogDate
            Return Me
        End Function
        Public Function WithVehicle(vehicle As ClVehicle) As ClLogEntryBuilder
            _businessBase.Vehicle = vehicle
            Return Me
        End Function
        Public Function WithReport(strReport As String) As ClLogEntryBuilder
            _businessBase.Report = strReport
            Return Me
        End Function

        Public Function WithCrew(crew As ClCrew) As ClLogEntryBuilder
            _businessBase.Crew = crew
            Return Me
        End Function
    End Class

End Class
