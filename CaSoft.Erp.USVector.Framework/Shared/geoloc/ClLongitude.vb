Public Class ClLongitude
    Inherits ClGeoPointBase

    Public Sub New(dblLongitude As Double)
        SetValue(dblLongitude)
    End Sub
    Public Overrides Sub SetValue(dblCoordonate As Double)
        If IsValidValue(dblCoordonate) Then
            _value = dblCoordonate
        Else
            Throw New ClGeographicCoordonateNotValid()
        End If
    End Sub

    'Public ReadOnly Property IsValid As Boolean
    '    Get
    '        Dim result As Boolean = False
    '        If Not MyBase.IsEmpty Then
    '            Return IsLongitudeValueValid(Value)
    '        End If

    '        Return result
    '    End Get
    'End Property

    Private Function IsValidValue(dblLongitude As Double) As Boolean
        Dim result As Boolean
        If dblLongitude > -90 And dblLongitude < 90 Then

            result = True
        End If

        Return result

    End Function

    Public Overloads Function ToString() As String
        Return Value.ToString()
    End Function


End Class

