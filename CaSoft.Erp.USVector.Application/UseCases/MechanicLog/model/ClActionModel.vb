Public Class ClGetActionModel
    Public Property Id As Integer
    Public Property Actor As ClIdValueModel
    Public Property ActionType As ClIdValueModel
    Public Property Constraint As ClConstraintModel
    Public Property Rappel As DateTime?
    Public Property DueDate As DateTime?
    Public Property Creation As Date
    Public Property comment() As String
    Public Property Closed() As Date?
    Public Property CallBack As Date?
    Public Property AnalyzeId As Integer
End Class

Public Class ClEditActionModel
    Public Property Id As Integer? 'peut etre null lors de la creation
    Public Property ActorId As Integer
    Public Property ActionTypeId As Integer
    Public Property ConstraintId As Integer
    Public Property DueDate As DateTime?
    Public Property CallBackDate As Date?
    Public Property Comment() As String
    Public Property StateId() As Integer
    Public Property StateDate() As Date?

End Class
