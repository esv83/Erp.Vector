Public Class ClDateTimePeriod
    Private _internal As SortedSet(Of DateTime)
    Private _utcData As Boolean
    Private Sub New(utcData As Boolean)
        _internal = New SortedSet(Of DateTime)
        _utcData = utcData
    End Sub
    Public ReadOnly Property ToleranceInMn As Integer
        Get
            Dim result As Integer = 0

            'If _internal.Count = 1 Then  'il n'y a que le scheduling
            '    result = 4 * 60 ' 4 heures

            'Else
            '    result = 0
            'End If

            Return result

        End Get
    End Property
    Public Sub AddUtcTime(utcTime As DateTime?)
        If Not utcTime.HasValue Then
            Exit Sub
        End If

        If _utcData Then
            _internal.Add(utcTime)
        Else
            Dim localTime As DateTime = utcTime.Value.ToLocalTime
            _internal.Add(localTime)
        End If

    End Sub
    Public Sub AddLocalTime(localTime As DateTime?)
        If Not localTime.HasValue Then
            Exit Sub
        End If

        If _utcData Then
            Dim utcTime As DateTime = localTime.Value.ToUniversalTime
            _internal.Add(utcTime)
        Else
            _internal.Add(localTime)
        End If

    End Sub
    Public ReadOnly Property EndTime As DateTime
        Get

            Dim maxJobTime = _internal.Max
            Dim result = maxJobTime.AddMinutes(ToleranceInMn)
            Return result
        End Get
    End Property
    Public ReadOnly Property StartTime As DateTime
        Get
            Dim minJobTime = _internal.Min
            Dim result = minJobTime.AddMinutes(ToleranceInMn * -1)
            Return result


        End Get
    End Property
    Public ReadOnly Property StartAndEndOnSameDay As Boolean
        Get
            Return StartTime.Day = EndTime.Day
        End Get
    End Property
    Public ReadOnly Property IsEmpty As Boolean
        Get
            Return _internal.Count = 0
        End Get
    End Property
    Public Shared Function GetBuilder(bWithUtcData As Boolean) As ClPeriodBuilder
        Return ClPeriodBuilder.GetBuilder(bWithUtcData)
    End Function
    Public Overrides Function ToString() As String
        If IsEmpty Then
            Return "Pas d'horaires"
        Else
            Return $"De {StartTime.ToShortTimeString} à {EndTime.ToShortTimeString}"
        End If

    End Function
    Public Class ClPeriodBuilder

        Private _period As ClDateTimePeriod
        Private Sub New(utcData As Boolean)
            _period = New ClDateTimePeriod(utcData)
        End Sub
        Public Function WithUtcTime(utcTime As DateTime?) As ClPeriodBuilder

            _period.AddUtcTime(utcTime)

            Return Me

        End Function
        Public Function WithLocalTime(localTime As DateTime?) As ClPeriodBuilder

            If localTime.HasValue Then
                _period.AddLocalTime(localTime)
            End If

            Return Me
        End Function
        Public Function Build() As ClDateTimePeriod
            Return _period
        End Function
        Public Shared Function GetBuilder(bWithUtcData As Boolean) As ClPeriodBuilder
            Return New ClPeriodBuilder(bWithUtcData)
        End Function

    End Class

End Class
