Imports System.Threading

Namespace Port

    ''' <summary>
    ''' J-144 / J-154 — L'équipage complète le patient de sa mission : identité d'un patient né
    ''' anonyme, téléphone et e-mail de tout patient. <b>Écrit chez Order</b>, par sa bordure
    ''' (<c>publicaccess/vector/…</c>), avec le jeton de service de Vector.
    ''' <para>
    ''' <b>Un relais, aucune règle.</b> Qui peut écrire (l'équipage affecté), jusqu'à quand (deux jours
    ''' après la mission), ce qui est verrouillé (identité tranchée par la régulation) : tout se juge
    ''' chez Order, et son motif traverse tel quel jusqu'au front.
    ''' </para>
    ''' </summary>
    Public Interface IBeneficiaryFieldEntryService

        ''' <summary>Identité du patient d'une commande née anonyme. Refus : 400, 403, 404, 409.</summary>
        ''' <param name="setBy">Le <c>sub</c> Keycloak de l'ambulancier, tiré de SON jeton.</param>
        Function RecordIdentityAsync(missionId As Guid,
                                     identity As ClBeneficiaryIdentityDtoIn,
                                     setBy As String,
                                     ct As CancellationToken) As Task(Of ClBeneficiaryEntryResult)

        ''' <summary>Téléphone et/ou e-mail ajoutés au patient, jamais à la place du principal. Refus : 400, 403, 404.</summary>
        ''' <param name="setBy">Le <c>sub</c> Keycloak de l'ambulancier, tiré de SON jeton.</param>
        Function RecordContactAsync(missionId As Guid,
                                    contact As ClBeneficiaryContactDtoIn,
                                    setBy As String,
                                    ct As CancellationToken) As Task(Of ClBeneficiaryEntryResult)

    End Interface

End Namespace
