Imports CaSoft.Framework
Public Class ClAnalyzeAction
    Inherits ClBusinessBase(Of ClAnalyzeAction)
    Public ReadOnly Property DeadLine As Date?

    Public ReadOnly Property IsClosed As Boolean
        Get
            Return StateDate.HasValue
        End Get
    End Property

    Public ReadOnly Property HasDeadLine As Boolean
        Get
            Return DeadLine.HasValue
        End Get
    End Property

    Friend analyzeIdProperty = RegisterProperty(Of Integer)(Function(f) f.AnalyzeId)
    Public Property AnalyzeId As Integer
        Get
            Return GetProperty(analyzeIdProperty)
        End Get
        Set(value As Integer)
            SetProperty(analyzeIdProperty, value)
        End Set
    End Property

    Friend actorProperty = RegisterProperty(Of ClIdValue)(Function(f) f.Actor)
    Public Property Actor As ClIdValue
        Get
            Return GetProperty(actorProperty)
        End Get
        Set(value As ClIdValue)
            SetProperty(actorProperty, value)
        End Set
    End Property

    Friend actionTypeProperty = RegisterProperty(Of ClIdValue)(Function(f) f.ActionType)
    Public Property ActionType As ClIdValue
        Get
            Return GetProperty(actionTypeProperty)
        End Get
        Set(value As ClIdValue)
            SetProperty(actionTypeProperty, value)
        End Set
    End Property

    Friend constraintProperty = RegisterProperty(Of ClConstraintType)(Function(f) f.Constraint)
    Public Property Constraint As ClConstraintType
        Get
            Return GetProperty(constraintProperty)
        End Get
        Set(value As ClConstraintType)
            SetProperty(constraintProperty, value)
        End Set
    End Property

    Friend callBackProperty = RegisterProperty(Of DateTime?)(Function(f) f.CallBack)
    Public Property CallBack As DateTime?
        Get
            Return GetProperty(callBackProperty)
        End Get
        Set(value As DateTime?)
            SetProperty(callBackProperty, value)
        End Set
    End Property

    Friend dueDateProperty = RegisterProperty(Of DateTime?)(Function(f) f.DueDate)
    Public Property DueDate As DateTime?
        Get
            Return GetProperty(dueDateProperty)
        End Get
        Set(value As DateTime?)
            SetProperty(dueDateProperty, value)
        End Set
    End Property

    Friend creationProperty = RegisterProperty(Of DateTime)(Function(f) f.Creation)
    Public Property Creation As DateTime
        Get
            Return GetProperty(creationProperty)
        End Get
        Set(value As DateTime)
            SetProperty(creationProperty, value)
        End Set
    End Property

    Friend commentProperty = RegisterProperty(Of String)(Function(f) f.Comment)
    Public Property Comment() As String
        Get
            Return GetProperty(commentProperty)
        End Get
        Set(value As String)
            SetProperty(commentProperty, value)
        End Set
    End Property

    Friend closedProperty = RegisterProperty(Of DateTime?)(Function(f) f.StateDate)
    Public Property StateDate() As DateTime?
        Get
            Return GetProperty(closedProperty)
        End Get
        Set(value As DateTime?)
            SetProperty(closedProperty, value)
        End Set
    End Property

    Public Property Crew As String
End Class
