
Imports System.ComponentModel

Public Class ClBusinessBase(Of E)
    Inherits ClEntityBase
    Implements INotifyPropertyChanged
    Implements IPersistable

    Private _isChild As Boolean
    Private _byPass As Boolean = False

    Protected Friend _isNew As Boolean = False
    Protected Friend _isDirty As Boolean = False
    Protected Friend _isDeleted As Boolean = False
    Protected Friend _fieldManager As ClFieldManager

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Public Sub New()

        _fieldManager = New ClFieldManager
        _isNew = False
        _isDirty = False
        _isDeleted = False

    End Sub

#Region "properties"

    Public ReadOnly Property FieldManager As ClFieldManager
        Get
            Return _fieldManager
        End Get
    End Property
    Public ReadOnly Property IsNew As Boolean Implements IPersistable.IsNew
        Get
            Return _isNew
        End Get
    End Property
    Public ReadOnly Property IsDirty As Boolean Implements IPersistable.IsDirty
        Get
            Return _isDirty
        End Get
    End Property
    Public ReadOnly Property IsDeleted As Boolean Implements IPersistable.IsDeleted
        Get
            Return _isDeleted
        End Get
    End Property

#End Region

#Region "Methodes"

    Public Sub MarkAsDeleted()

        _isDeleted = True

    End Sub
    Public Sub MarkAsNew()
        _isNew = True
        _isDirty = True
        _isDeleted = False
    End Sub
    Public Sub MarkFromPersistentSource()
        _isNew = False
        _isDirty = False
        _isDeleted = False
    End Sub
    Public Sub MarkAsDirty()
        _isNew = False
        _isDirty = True
    End Sub

    ' Réactivé lors du portage MobileApp (était commenté dans la V2) : utilisé par
    ' ClUpdateLogAnalyzeUseCase pour fusionner les entités par FieldManager.
    Public Sub Merge(obj As ClBusinessBase(Of E))

        For Each field In obj.FieldManager

            Dim propInfo = FieldManager(field.Key)
            Dim propInfoValue = propInfo.Value
            Dim newValue = field.Value.Value


            If propInfo.Relation = RelationshipTypes.Child Then
                Dim child = CType(propInfo.Value, IMergeableEntity)
                child.Merge(newValue)

            ElseIf propInfo.Relation = RelationshipTypes.ChildList Then

                Dim synchroMethod = propInfo.Type.GetMethod("SynchronizeWith")
                If synchroMethod IsNot Nothing Then
                    synchroMethod.Invoke(propInfoValue, {newValue})
                End If

            Else
                If Not Object.Equals(newValue, propInfo.Value) Then
                    SetProperty(propInfo, newValue)
                End If
            End If


        Next

    End Sub

    'Public Sub Synchro(oldList As ClBusinessListBase(Of ClEntityBase), newList As ClBusinessListBase(Of ClEntityBase))

    '    For Each newItem In newList
    '        Dim oldItem = oldList.SingleOrDefault(Function(f) f.Id = newItem.Id)
    '        If oldItem IsNot Nothing Then

    '            oldItem.Merge(newItem)
    '        Else
    '            oldList.Add(newItem)
    '        End If

    '    Next

    '    For Each oldItem In oldList
    '        Dim newItem = newList.SingleOrDefault(Function(f) f.Id = oldItem.Id)
    '        If newItem IsNot Nothing Then

    '            oldItem.MarkAsDeleted()

    '        End If

    '    Next


    'End Sub
    Protected Overridable Sub InitEntityAsChild()
        _isChild = False
        _isDirty = False
    End Sub
    Protected Overridable Sub InitEntityAsRoot()
        _isChild = True
        _isDirty = False
    End Sub

    Public Function RegisterProperty(Of T)([property] As Expressions.Expression(Of Func(Of E, Object)), Optional relation As RelationshipTypes = RelationshipTypes.None, Optional defaultValue As Object = Nothing) As ClPropertyInfo
        Dim propertyName = ClReflexionHelper.GetPropertyName([property].Body)
        Dim PropertyInfo = CreatePropertyInfo(propertyName, relation, GetType(T), Nothing)

        _fieldManager.Add(PropertyInfo.Name, PropertyInfo)

        Return PropertyInfo

    End Function
    Public Function RegisterProperty(Of T, C)([property] As Expressions.Expression(Of Func(Of C, Object)), Optional relationShip As RelationshipTypes = RelationshipTypes.None, Optional defaultValue As Object = Nothing) As ClPropertyInfo
        Dim propertyName = ClReflexionHelper.GetPropertyName([property].Body)
        Dim PropertyInfo = CreatePropertyInfo(propertyName, relationShip, GetType(T), defaultValue)

        _fieldManager.Add(PropertyInfo.Name, PropertyInfo)

        Return PropertyInfo

    End Function
    Private Function CreatePropertyInfo(strName As String, relationShip As ModEnumeration.RelationshipTypes, type As Type, defaultValue As Object) As ClPropertyInfo

        Dim result As New ClPropertyInfo
        With result
            .Name = strName
            .Relation = relationShip
            .Type = type
            .DefaultValue = defaultValue
        End With

        Return result

    End Function
    Public Sub SetProperty(strPropertyName As String, value As Object)

        Dim PropertyInfo = _fieldManager(strPropertyName)

        SetProperty(PropertyInfo, value)


    End Sub
    Public Sub SetProperty(prop As ClPropertyInfo, value As Object)

        Dim PropertyInfo = _fieldManager(prop.Name)

        If value Is Nothing Then
            If PropertyInfo.Value Is Nothing Then
                Exit Sub
            End If
        End If

        If PropertyInfo.HasValue Then
            If PropertyInfo.Value.Equals(value) Then
                Exit Sub
            End If
        End If

        If PropertyInfo.Relation = RelationshipTypes.Child Then
            'SetParent(value)
        End If

        PropertyInfo.Value = value
        If Not _byPass Then
            _isDirty = True
        End If

        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(prop.Name))

    End Sub
    Private Sub SetParent(value As Object)
        Dim Child = TryCast(value, IChildBusinessObject)
        If Child IsNot Nothing Then
            Child.SetParent(Me)
        Else
            Throw New InvalidCastException("l'objet enfant doit heriter de IChildBusinessObject")
        End If
    End Sub
    Public Sub LoadProperty(prop As ClPropertyInfo, value As Object)
        Dim PropertyInfo = _fieldManager(prop.Name)
        PropertyInfo.Value = value
    End Sub
    Public Function GetProperty(prop As ClPropertyInfo) As Object
        Dim result As Object
        Dim PropertyInfo = _fieldManager(prop.Name)
        result = PropertyInfo.Value
        Return result
    End Function
    Public Sub ByPass()
        _byPass = True
    End Sub
    Public Sub EndByPass()
        _byPass = False
    End Sub

#End Region

End Class



