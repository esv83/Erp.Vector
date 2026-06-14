Public Class ClChildBusinessObject(Of T)
    Inherits ClBusinessBase(Of T)

    Implements IChildBusinessObject

    Private _parent As Object
    Public ReadOnly Property Parent As Object Implements IChildBusinessObject.Parent
        Get
            Return _parent
        End Get
    End Property

    Public Sub SetParent(objParent As Object) Implements IChildBusinessObject.SetParent
        _parent = objParent
    End Sub
End Class

