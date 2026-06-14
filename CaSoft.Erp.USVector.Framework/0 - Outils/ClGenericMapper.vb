Imports System.Reflection

Namespace Helper
    ''' <summary>
    ''' Map Entity To Dto and Dto To Entity
    ''' </summary>
    ''' <typeparam name="E">Entity Type</typeparam>
    ''' <typeparam name="D">Dto Type</typeparam>
    Public Class ClGenericMapper(Of E, D)
        'Implements IEntityMapper

        Private _ManualMappingDictionnary As Dictionary(Of ClPropertyName, ClPropertyName)
        Private _ManualComplexMappingDictionnary As Dictionary(Of ClPropertyName, IComplexMapping(Of Object, D))
        Private _entityType As Type
        Private _entityProps As PropertyInfo()
        Private _dtoType As Type
        Private _dtoProps As PropertyInfo()

        Public Sub New()

            _entityType = GetType(E) 'EntityObjectType
            _dtoType = GetType(D)

            _ManualMappingDictionnary = New Dictionary(Of ClPropertyName, ClPropertyName)
            _ManualComplexMappingDictionnary = New Dictionary(Of ClPropertyName, IComplexMapping(Of Object, D))

            _entityProps = _entityType.GetProperties()
            _dtoProps = _dtoType.GetProperties

        End Sub

        Public Sub AddMapping(EntityProperty As Expressions.Expression(Of Func(Of E, Object)), DtoProperty As Expressions.Expression(Of Func(Of D, Object)))
            Dim strEntityPropertyName = ClReflexionHelper.GetPropertyName(EntityProperty.Body)
            Dim strDtoPropertyName = ClReflexionHelper.GetPropertyName(DtoProperty.Body)

            _ManualMappingDictionnary.Add(New ClPropertyName(strEntityPropertyName), New ClPropertyName(strDtoPropertyName))

        End Sub

        Public Sub AddComplexMapping(EntityProperty As Expressions.Expression(Of Func(Of E, Object)), ComplexMapping As IComplexMapping(Of Object, D))
            Dim strEntityPropertyName = ClReflexionHelper.GetPropertyName(EntityProperty.Body)


            _ManualComplexMappingDictionnary.Add(New ClPropertyName(strEntityPropertyName), ComplexMapping)

        End Sub

        ''' <summary>
        ''' Transforme une entity E en DTO D
        ''' </summary>
        ''' <param name="entity">as E</param>
        ''' <returns>Entity as D</returns>
        Public Function GetDto(entity As E) As D 'Implements IEntityMapper.GetDto

            Dim Dto As D = Activator.CreateInstance(GetType(D))

            For Each PropertyItem In _ManualMappingDictionnary

                Dim EntityPropertyName = PropertyItem.Key
                Dim EntityPropertie = _entityProps.Single(Function(f) f.Name.ToUpper = EntityPropertyName.ToString.ToUpper)
                Dim EntityPropValue = EntityPropertie.GetValue(entity)

                Dim DtoPropertyName = PropertyItem.Value
                Dim DtoPropertie = _dtoProps.Single(Function(f) f.Name = DtoPropertyName.Value)

                DtoPropertie.SetValue(Dto, EntityPropValue)

            Next

            For Each PropertyItem In _ManualComplexMappingDictionnary

                Dim EntityPropertyName = PropertyItem.Key
                Dim EntityPropertie = _entityProps.Single(Function(f) f.Name.ToUpper = EntityPropertyName.ToString.ToUpper)
                Dim EntityPropValue = EntityPropertie.GetValue(entity)


                Dim ComplexObjectAdapter = PropertyItem.Value

                ComplexObjectAdapter.LoadDto(EntityPropValue, Dto)

            Next


            Return Dto

        End Function



        Public Function GetDtoList(Query As IQueryable(Of E)) As List(Of D)
            Dim result As New List(Of D)

            For Each ent In Query
                result.Add(GetDto(ent))
            Next

            Return result

        End Function
        Public Function GetEntity(Dto As D) As E

            Dim result As E = Activator.CreateInstance(_entityType)

            For Each PropertieMapping In _ManualMappingDictionnary

                Dim DtoPropertyName = PropertieMapping.Value
                Dim DtoPropertie = _dtoProps.Single(Function(f) f.Name = DtoPropertyName.ToString)
                Dim DtoPropValue = DtoPropertie.GetValue(Dto)

                Dim EntityPropertyName = PropertieMapping.Key
                Dim EntityPropertie = _entityProps.Single(Function(f) f.Name = EntityPropertyName.ToString)

                EntityPropertie.SetValue(result, DtoPropValue)

            Next


            For Each PropertyItem In _ManualComplexMappingDictionnary

                Dim EntityPropertyName = PropertyItem.Key
                Dim EntityPropertie = _entityProps.Single(Function(f) f.Name.ToUpper = EntityPropertyName.ToString.ToUpper)

                Dim ComplexObjectAdapter = PropertyItem.Value
                Dim ComplexObject = ComplexObjectAdapter.GetComplexObject(Dto)

                EntityPropertie.SetValue(result, ComplexObject)

            Next

            Return result

        End Function

    End Class
    Public Class ClPropertyName

        Private _propertyName As String
        Public Sub New(strPropertyName)
            _propertyName = strPropertyName
        End Sub
        Public ReadOnly Property Value As String
            Get
                Return _propertyName
            End Get
        End Property
        Public Overrides Function ToString() As String
            Return _propertyName
        End Function

        Public Function IsEqual(PropertyName As ClPropertyName) As Boolean
            Return (Me.ToString = PropertyName.ToString)
        End Function

    End Class

End Namespace