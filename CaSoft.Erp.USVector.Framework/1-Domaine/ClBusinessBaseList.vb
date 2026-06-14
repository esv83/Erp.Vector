Imports System.Collections.ObjectModel



Public Class ClBusinessListBase(Of T, E)
        Inherits ObservableCollection(Of E)


        Private _parent As Object
        'Public Sub New(parent As Object)
        '    _parent = parent
        'End Sub
        Public Overloads Sub Add(child As E)

            ' child.SetParent(_parent)
            '  MyBase.Add(child)

        End Sub

        'Public Sub Synchronyze(otherList As ClBusinessListBase(Of E))
        '    For Each newItem In otherList
        '        Dim oldItem = Me.SingleOrDefault(Function(f) f.Id = newItem.Id)
        '        If oldItem IsNot Nothing Then

        '            oldItem.Merge(newItem)
        '        Else
        '            Me.Add(newItem)
        '        End If

        '    Next

        '    For Each oldItem In Me
        '        Dim newItem = otherList.SingleOrDefault(Function(f) f.Id = oldItem.Id)
        '        If newItem IsNot Nothing Then

        '            oldItem.MarkAsDeleted()

        '        End If

        '    Next
        'End Sub


    End Class


' Surcharge à un seul paramètre générique — attendue par le code porté de MobApp.Domaine
' (ClActionsList, ...). Collection observable standard, sans le Add() no-op de la variante (Of T, E).
Public Class ClBusinessListBase(Of E)
    Inherits System.Collections.ObjectModel.ObservableCollection(Of E)

    ' Reconstitution du Synchronyze du framework legacy (commenté dans la V2) :
    ' remplace le contenu par celui de la liste fournie.
    Public Overridable Sub Synchronyze(otherList As Object)
        Dim source = TryCast(otherList, IEnumerable)
        If source Is Nothing Then Return

        Me.Clear()
        For Each element In source
            If TypeOf element Is E Then Me.Add(DirectCast(element, E))
        Next
    End Sub
End Class
