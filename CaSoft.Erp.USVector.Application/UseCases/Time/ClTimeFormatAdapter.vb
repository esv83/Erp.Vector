Public Class ClTimeFormatAdapter

    Private _dteDateTime As DateTime? = Nothing
    Public Sub New(dteTime As DateTime?)
        _dteDateTime = dteTime

    End Sub

    Public Sub New(strTime As String)
        If Not String.IsNullOrWhiteSpace(strTime) Then
            Dim bufferTime As DateTimeOffset
            If DateTimeOffset.TryParse(strTime, bufferTime) Then
                _dteDateTime = bufferTime.UtcDateTime
            Else
                _dteDateTime = Nothing
            End If
        End If


    End Sub

    Public Overrides Function ToString() As String
        Dim result = Nothing
        If _dteDateTime.HasValue Then
            result = _dteDateTime.Value.ToString("yyyy-MM-ddTHH:mm:ssZ")
        End If

        Return result

    End Function

    Public Function ToDateTime() As DateTime?
        Return _dteDateTime
    End Function

End Class
