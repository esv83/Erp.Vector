Imports CaSoft.Framework

Public Class ClLogAnalyze
    Inherits ClBusinessBase(Of ClLogAnalyze)

    Public Sub New()
        ByPass()
        SetProperty(ActionsProperty, New ClActionsList)
        EndByPass()
    End Sub

    Friend analyzeIdProperty = RegisterProperty(Of Integer)(Function(f) f.AnalyzeId, RelationshipTypes.None, True)
    Public ReadOnly Property AnalyzeId As Integer
        Get
            Return GetProperty(analyzeIdProperty)
        End Get
    End Property

    Friend detailProperty = RegisterProperty(Of String)(Function(f) f.Detail)
    Public Property Detail As String
        Get
            Return GetProperty(detailProperty)
        End Get
        Set(value As String)
            SetProperty(detailProperty, value)
        End Set
    End Property

    'Friend immatriculationProperty = RegisterProperty(Of String)(Function(f) f.Immatriculation)
    'Public Property Immatriculation As String
    '    Get
    '        Return GetProperty(immatriculationProperty)
    '    End Get
    '    Set(value As String)
    '        SetProperty(immatriculationProperty, value)
    '    End Set
    'End Property

    Friend crewProperty = RegisterProperty(Of ClCrew)(Function(f) f.Crew)
    Public Property Crew As ClCrew
        Get
            Return GetProperty(crewProperty)
        End Get
        Set(value As ClCrew)
            SetProperty(crewProperty, value)
        End Set
    End Property

    Friend AnalyzeProperty = RegisterProperty(Of String)(Function(f) f.Analyze)
    Public Property Analyze As String
        Get
            Return GetProperty(AnalyzeProperty)
        End Get
        Set(value As String)
            SetProperty(AnalyzeProperty, value)
        End Set
    End Property

    Friend AnalyzeByProperty = RegisterProperty(Of String)(Function(f) f.AnalyzeBy)
    Public Property AnalyzeBy As String
        Get
            Return GetProperty(AnalyzeByProperty)
        End Get
        Set(value As String)
            SetProperty(AnalyzeByProperty, value)
        End Set
    End Property

    Friend ConcerningProperty = RegisterProperty(Of ClIdValue)(Function(f) f.Concerning)

    Public Property Concerning As ClIdValue
        Get
            Return GetProperty(ConcerningProperty)
        End Get
        Set(value As ClIdValue)
            SetProperty(ConcerningProperty, value)
        End Set
    End Property
    Friend LogIdProperty = RegisterProperty(Of String)(Function(f) f.LogId)
    Public Property LogId As Integer
        Get
            Return GetProperty(LogIdProperty)
        End Get
        Set(value As Integer)
            SetProperty(LogIdProperty, value)
        End Set
    End Property

    Friend NatureProperty = RegisterProperty(Of ClIdValue)(Function(f) f.Nature)
    Public Property Nature As ClIdValue
        Get
            Return GetProperty(NatureProperty)
        End Get
        Set(value As ClIdValue)
            SetProperty(NatureProperty, value)
        End Set
    End Property

    Friend immobilizeProperty = RegisterProperty(Of Boolean)(Function(f) f.ImmobilizeVehicle)
    Public Property ImmobilizeVehicle As Boolean
        Get
            Return GetProperty(immobilizeProperty)
        End Get
        Set(value As Boolean)
            SetProperty(immobilizeProperty, value)
        End Set
    End Property

    Friend dateProperty = RegisterProperty(Of Date)(Function(f) f.Date)
    Public Property [Date] As Date
        Get
            Return GetProperty(dateProperty)
        End Get
        Set(value As Date)
            SetProperty(dateProperty, value)
        End Set
    End Property

    Friend ActionsProperty = RegisterProperty(Of ClActionsList)(Function(f) f.ActionsList, RelationshipTypes.ChildList)
    Public ReadOnly Property ActionsList As ClActionsList
        Get
            Return GetProperty(ActionsProperty)
        End Get
    End Property

    Public Sub AddAction(pAction As ClAnalyzeAction)
        ActionsList.Add(pAction)
    End Sub

    Public Sub AddActionRange(actionList As ClActionsList)

        For Each action In actionList
            ActionsList.Add(action)
        Next

    End Sub


End Class
