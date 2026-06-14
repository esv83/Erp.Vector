

Imports CaSoft.Framework

Public Class ClJobTimeData
    Inherits ClBusinessBase(Of ClJobTimeData)

    Private Sub New()
        MyBase.MarkAsNew()
    End Sub

    Public Shared Function GetBuilder() As ClJobTimeDataBuilder
        Return New ClJobTimeDataBuilder
    End Function

    Protected Friend jobIdProperty = RegisterProperty(Of Guid)(Function(f) f.JobId)
    Public Property JobId As Guid
        Get
            Return GetProperty(jobIdProperty)
        End Get
        Set(value As Guid)
            SetProperty(jobIdProperty, value)
        End Set
    End Property

    Private ackTimeProperty = RegisterProperty(Of DateTime?)(Function(f) f.AckTime)
    Public Property AckTime As DateTime?
        Get
            Return GetProperty(ackTimeProperty)
        End Get
        Set(value As DateTime?)
            SetProperty(ackTimeProperty, value)
        End Set
    End Property

    Private readTimeProperty = RegisterProperty(Of DateTime?)(Function(f) f.ReadTime)
    Public Property ReadTime As DateTime?
        Get
            Return GetProperty(readTimeProperty)
        End Get
        Set(value As DateTime?)
            SetProperty(readTimeProperty, value)
        End Set
    End Property

    Private goTimeProperty = RegisterProperty(Of DateTime?)(Function(f) f.goTime)
    Public Property GoTime As DateTime?
        Get
            Return GetProperty(goTimeProperty)
        End Get
        Set(value As DateTime?)
            SetProperty(goTimeProperty, value)
        End Set
    End Property

    Private onSiteTimeProperty = RegisterProperty(Of DateTime?)(Function(f) f.onSiteTime)
    Public Property OnSiteTime As DateTime?
        Get
            Return GetProperty(onSiteTimeProperty)
        End Get
        Set(value As DateTime?)
            SetProperty(onSiteTimeProperty, value)
        End Set
    End Property

    Private terminateTimeProperty = RegisterProperty(Of DateTime?)(Function(f) f.TerminateTime)
    Public Property TerminateTime As DateTime?
        Get
            Return GetProperty(terminateTimeProperty)
        End Get
        Set(value As DateTime?)
            SetProperty(terminateTimeProperty, value)
        End Set
    End Property

    Public Class ClJobTimeDataBuilder
        Private _timeData As ClJobTimeData
        Public Sub New()
            _timeData = New ClJobTimeData
        End Sub
        Public Function WithId(gJobId As Guid) As ClJobTimeDataBuilder
            _timeData.JobId = gJobId
            Return Me
        End Function
        Public Function WithAckTime(dteDateTime As DateTime?) As ClJobTimeDataBuilder

            If dteDateTime.HasValue Then
                _timeData.AckTime = dteDateTime
            End If

            Return Me

        End Function
        Public Function WithReadTime(dteDateTime As DateTime?) As ClJobTimeDataBuilder

            If dteDateTime.HasValue Then
                _timeData.ReadTime = dteDateTime
            End If

            Return Me

        End Function
        Public Function WithGoTime(dteDateTime As DateTime?) As ClJobTimeDataBuilder

            If dteDateTime.HasValue Then
                _timeData.GoTime = dteDateTime
            End If

            Return Me

        End Function
        Public Function WithOnSiteTime(dteDateTime As DateTime?) As ClJobTimeDataBuilder

            If dteDateTime.HasValue Then
                _timeData.OnSiteTime = dteDateTime
            End If

            Return Me

        End Function
        Public Function WithTerminateTime(dteDateTime As DateTime?) As ClJobTimeDataBuilder

            If dteDateTime.HasValue Then
                _timeData.TerminateTime = dteDateTime
            End If

            Return Me

        End Function
        Public Function WithPersistentOrigine() As ClJobTimeDataBuilder

            _timeData.MarkFromPersistentSource()
            _timeData.ByPass()
            Return Me

        End Function
        Public Function Build() As ClJobTimeData
            _timeData.EndByPass()
            Return _timeData
        End Function

    End Class


End Class
