Imports CaSoft.Erp.USVector.Application.Dto

Public Class ClJobListModel
    Public Sub New(pjobList As List(Of ClJobListItemModel), pInstructionList As List(Of ClInstructionListItemModel))
        JobList = pjobList
        InstructionList = pInstructionList

    End Sub
    Public Property JobList As List(Of ClJobListItemModel)
    Public Property InstructionList As List(Of ClInstructionListItemModel)

End Class
