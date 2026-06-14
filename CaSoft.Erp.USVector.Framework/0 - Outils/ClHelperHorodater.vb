
<Serializable> Public Class ClHelperHorodater
    Implements IEntityHorodater
    Public Sub HorodateInsert(entity As IHorodatableEntity) Implements IEntityHorodater.SetCreationDate

        With entity

            'Dim inst = ClBllSetting.GetInstance
            'If inst.Login IsNot Nothing Then
            '    .Creator = inst.Login.Abrege
            'Else
            '    .Creator = "N_A"
            'End If

            .Created = Date.Now

        End With

    End Sub
    Public Sub HorodateUpdate(Entity As IHorodatableEntity) Implements IEntityHorodater.SetModificationDate
        'Dim inst = ClBllSetting.GetInstance

        'With Entity
        '    If inst.Login IsNot Nothing Then
        '        .Updator = inst.Login.Abrege
        '    Else
        '        .Updator = "N_A"
        '    End If

        '    .Updated = Date.Now
        'End With

    End Sub

    Public Shared Function GetHorodater() As IEntityHorodater
        Return New ClHelperHorodater
    End Function
End Class
