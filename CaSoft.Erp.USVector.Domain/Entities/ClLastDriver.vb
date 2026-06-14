Public Class ClLastDriver

    Public Sub New(Driver As ClEmployee, dteFrom As DateTime)
        _Employee = Driver
        _From = dteFrom
    End Sub

    Public ReadOnly Property Employee As ClEmployee
    Public ReadOnly Property From As DateTime

End Class
