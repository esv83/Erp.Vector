Public Class ClAcknoledgeTrameSirus
    Inherits ClTrameBase

    Public Sub New(ValidationData As Byte())
        MyBase.New(4)
        ValidationData.CopyTo(_trameData, 0)
    End Sub

    Public ReadOnly Property Conforme As Boolean
        Get

            Dim digit As Integer = CInt(_trameData(4))


            Dim result As Boolean = (digit = 1)

            Return result

        End Get

    End Property


End Class
