Public Class ClDateOfBirth
    Private _birthday As DateOnly?
    Public Sub New()
        _birthday = Nothing
    End Sub
    Public Sub New(dteDate As DateOnly?)
        _birthday = dteDate
    End Sub
    Public Sub New(dteDate As DateTime?)
        If dteDate.HasValue Then
            _birthday = DateOnly.FromDateTime(dteDate.Value)
        End If

    End Sub
    Public Property EditValue As DateOnly?
        Get
            Return _birthday
        End Get
        Set(value As DateOnly?)
            _birthday = value
        End Set

    End Property
    Public ReadOnly Property IsValid As Boolean
        Get
            Return _birthday.HasValue AndAlso (_birthday.Value.Year > 1900 And (_birthday.HasValue < DateTime.Now.AddYears(10).Year))
        End Get
    End Property
    Public ReadOnly Property Age As String
        Get
            Dim result As String = String.Empty
            If _birthday.HasValue Then

                Dim ts As TimeSpan = DateTime.Now.Subtract(_birthday.Value.ToDateTime(TimeOnly.MinValue))
                Dim intAgeInDays As Integer = CInt(ts.TotalDays)

                If intAgeInDays > 0 Then

                    If intAgeInDays < 365 Then
                        result = String.Format("{0} mois", Math.Floor(intAgeInDays / 30))
                    Else
                        result = String.Format("{0} ans", Math.Floor(intAgeInDays / 365))
                    End If

                End If

            End If

            Return result

        End Get
    End Property
    Public Overrides Function ToString() As String

        Dim result As String = String.Empty

        If _birthday.HasValue Then

            Dim ts As TimeSpan = DateTime.Now.Subtract(_birthday.Value.ToDateTime(TimeOnly.MinValue))
            Dim intAge As Integer = CInt(ts.TotalDays)

            If intAge > 0 Then

                result = String.Format("{0}, {1}", _birthday.Value.ToShortDateString, Me.Age)

            End If

        End If

        Return result

    End Function

End Class

