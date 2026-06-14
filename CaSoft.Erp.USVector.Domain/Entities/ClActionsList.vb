Imports CaSoft.Framework

Public Class ClActionsList
    Inherits ClBusinessListBase(Of ClAnalyzeAction)



    Public ReadOnly Property HasActions As Boolean
        Get
            Return Count > 0
        End Get
    End Property
    Public ReadOnly Property OpenActions As Integer
        Get
            Return Me.Where(Function(x) Not x.IsClosed).Count
        End Get
    End Property
    Public ReadOnly Property ClosedActions As Integer
        Get
            Return Me.Where(Function(x) x.IsClosed).Count
        End Get
    End Property
    Public ReadOnly Property IsAllActionsClosed As Boolean
        Get
            Return Me.All(Function(x) x.IsClosed)
        End Get
    End Property
    Public ReadOnly Property HasDeadLine As Boolean
        Get
            Dim x = Me.Any(Function(f) f.DeadLine.HasValue)
            Return x
        End Get
    End Property
    Public ReadOnly Property NextDeadLine As Date?
        Get
            Dim x = Me.Max(Function(f) f.DeadLine)
            Return x
        End Get
    End Property
    Public Function GetActionText() As String
        Dim result As String = "Aucune action"

        Dim nbActions = Me.Count
        If nbActions > 0 Then
            Dim nbClosed = Me.LongCount(Function(f) f.IsClosed)
            Dim nbEnCours = nbActions - nbClosed

            If nbClosed > 0 Then
                result = String.Format("{0} action(s) en cours - {1} actions terminée(s)", nbEnCours.ToString, nbClosed.ToString)
            Else
                result = String.Format("{0} action(s) en cours", nbEnCours.ToString)
            End If

        End If

        Return result

    End Function
    Public Function GetDeadLineText() As String
        Dim result As String = String.Empty

        If Me.HasActions Then
            If HasDeadLine Then
                Dim strDeadLine = Me.NextDeadLine.Value.ToShortDateString
                result = String.Format("Prochaine échéance le {0}", strDeadLine)
            End If

        End If

        Return result

    End Function

    Public Sub SynchronizeWith(otherList As Object)
        MyBase.Synchronyze(otherList)
    End Sub

End Class
