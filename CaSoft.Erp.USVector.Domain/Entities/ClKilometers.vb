Public Class ClKilometers

    Public Sub New()
        _IsEmpty = True
    End Sub
    Public Sub New(dteDate As DateTime, intKilometers As Integer, strInputBy As String)
        _InputBy = strInputBy
        _IsEmpty = False
        _Kilometers = intKilometers
        _Date = dteDate
    End Sub
    Public Property Kilometers As Integer

    Public Property [Date] As DateTime

    Public ReadOnly Property IsEmpty
    Public ReadOnly Property InputBy As String


End Class
