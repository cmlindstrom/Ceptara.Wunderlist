﻿Imports System.Diagnostics
Imports System.Security.Cryptography.X509Certificates
Imports System.IO
Imports System.Text
Imports System.Runtime.Serialization
Imports System.Windows.Forms

Imports Newtonsoft.Json

Public Class Client

#Region "Fields"

    Private Const _rootClass As String = "Wunderlist.Client"

    Private WithEvents wb_OAuth As WebBrowser
    Private WithEvents frmAuthorizationForm As Windows.Forms.Form

    ''' <summary>
    ''' Container for accessing the keys issued by the Wunderlist Developer
    ''' web client
    ''' </summary>
    ''' <remarks></remarks>
    Dim _appKeys As AppConfig = Nothing

#End Region

#Region "Properties"

    ''' <summary>
    ''' The oAuthAccessToken including the accessToken string and the
    ''' issued and expiration dates.
    ''' </summary>
    ''' <value>Wunderlist.Client.oAuthAccessToken:</value>
    ''' <returns>Wunderlist.Client.oAuthAccessToken:</returns>
    ''' <remarks></remarks>
    Public Property AccessToken As oAuthAccessToken
        Get
            Return myAccessToken
        End Get
        Set(value As oAuthAccessToken)
            myAccessToken = value
        End Set
    End Property
    Dim myAccessToken As oAuthAccessToken = Nothing

    ''' <summary>
    ''' The accessToken returned by the service.
    ''' </summary>
    ''' <value>String:</value>
    ''' <returns>String:</returns>
    ''' <remarks></remarks>
    Public ReadOnly Property AccessTokenStored As String
        Get
            If Not IsNothing(myAccessToken) Then
                Return myAccessToken.GetStorageString
            Else
                Return String.Empty
            End If
        End Get
    End Property

    ''' <summary>
    ''' the oAuth code used to retrieve the access token.
    ''' </summary>
    ''' <value></value>
    ''' <returns>String:</returns>
    ''' <remarks></remarks>
    Public ReadOnly Property AuthCode As String
        Get
            Return _authCode
        End Get
    End Property
    Dim _authCode As String = String.Empty

    ''' <summary>
    ''' REST Helper class - used for authorizing a user.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public ReadOnly Property RestInterface As RESTHelper
        Get
            Return _rh
        End Get
    End Property
    Dim _rh As RESTHelper = Nothing

#End Region

#Region "Events"

    Public Event Connected As EventHandler

    Public Event CollectionUpdated As EventHandler(Of CollectionEventArgs)

    Public Event ItemAdded As EventHandler(Of ItemEventArgs)
    Public Event ItemUpdated As EventHandler(Of ItemEventArgs)
    Public Event ItemDeleted As EventHandler(Of ItemEventArgs)

#End Region

#Region "Event Handlers"

#Region "Authorization Events"

    Private Sub frmAuthorizationForm_FormClosing(ByVal sender As Object, _
                                                 ByVal e As System.Windows.Forms.FormClosingEventArgs) _
                                                    Handles frmAuthorizationForm.FormClosing
        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":frmAuthorizationForm_FormClosing"
        Try
            wb_OAuth.Dispose()
            wb_OAuth = Nothing
        Catch ex As Exception
            Debug.Print("Error: " & strTrace & " " & ex.Message)
        End Try
    End Sub

    Private Sub wb_OAuth_Navigated(ByVal sender As Object, _
                                   ByVal e As System.Windows.Forms.WebBrowserNavigatedEventArgs) _
                                        Handles wb_OAuth.Navigated

        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":wb_OAuth_Navigated"
        Dim strDefault As String = "Method failed."
        Try

            ' http://com.ceptara.app/oauth2redirect?state=001&code=197811cdfddaee00fdfc

            Dim strTitle As String = wb_OAuth.DocumentTitle
            Dim myURI As Uri = e.Url
            Dim strURLPath As String = e.Url.AbsoluteUri

            strTrace = "Returned Title: '" & strTitle & "' and URL '" & strURLPath & "'."

            If myURI.AbsoluteUri.ToLower.Contains("oauth2redirect") Then

                ' Close the request form
                frmAuthorizationForm.Close()

                ' Capture the authToken so that an accessToken can be retrieved.
                Dim code As String = GetKeyValueFromString(myURI.Query, "code")
                If Not String.IsNullOrEmpty(code) Then
                    _authCode = code
                    ExchangeTokensAsync()
                Else

                End If
            End If

        Catch ex As Exception
            LogErr(strTrace, ex, strRoutine)
        End Try

    End Sub

    Private Function GetKeyValueFromString(ByVal httpQueryString As String, ByVal VariableName As String) As String

        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":GetKeyValueFromString"
        Dim strDefault As String = "Method failed."
        Try
            Dim params As System.Collections.Specialized.NameValueCollection = _
                System.Web.HttpUtility.ParseQueryString(httpQueryString)

            Dim strReturn As String = params.Get(VariableName)

            Return strReturn

        Catch ex As Exception
            LogErr(strTrace, ex, strRoutine)
            Return String.Empty
        End Try

    End Function


#End Region

#End Region

#Region "Constructor"

    ''' <summary>
    ''' Constructs a basic Client
    ''' </summary>
    ''' <remarks>Need to set the AccessToken property to connect to
    ''' the Wunderlist service.</remarks>
    Public Sub New()
        Initialize()
    End Sub

    ''' <summary>
    ''' Constructs a client using the specific configuration
    ''' </summary>
    ''' <param name="myAppConfig"></param>
    ''' <remarks></remarks>
    Public Sub New(ByVal myAppConfig As AppConfig)
        _appKeys = myAppConfig
        Initialize()
    End Sub

    ''' <summary>
    ''' Constructs a client using a Json string representing 
    ''' a Client.oAuthAccessToken object instance
    ''' </summary>
    ''' <param name="JsonFormattedAccessTokenString">String: Json formatted</param>
    ''' <remarks></remarks>
    Public Sub New(ByVal JsonFormattedAccessTokenString As String)
        myAccessToken = New oAuthAccessToken
        myAccessToken.RestoreFromStorageString(JsonFormattedAccessTokenString)
        Initialize()
    End Sub

    ''' <summary>
    ''' Constructs a client using an oAuthAccessToken
    ''' </summary>
    ''' <param name="CurrentAccessToken">Client.oAuthAccessToken:</param>
    ''' <remarks></remarks>
    Public Sub New(ByVal CurrentAccessToken As oAuthAccessToken)
        myAccessToken = CurrentAccessToken
        Initialize()
    End Sub

    ''' <summary>
    ''' Constructs a client using a specified application configuration (ClientID, ClientSecret, CallBackURL)
    ''' and oAuthAccessToken (if new use blank, other used saved AccessToken)
    ''' </summary>
    ''' <param name="myAppConfig">Wunderlist.AppConfig:</param>
    ''' <param name="CurrentAccessToken">Client.oAuthAccessToken:</param>
    ''' <remarks></remarks>
    Public Sub New(ByVal myAppConfig As AppConfig, ByVal CurrentAccessToken As oAuthAccessToken)
        myAccessToken = CurrentAccessToken
        _appKeys = myAppConfig
        Initialize()
    End Sub

    Private Sub Initialize()

        If IsNothing(myAccessToken) Then myAccessToken = New oAuthAccessToken

        ' Container for keeping the ClientID, ClientSecret and call back URL
        If IsNothing(_appKeys) Then _appKeys = New AppConfig

        ' HTTP RESTful access helper
        _rh = New RESTHelper(_appKeys.ClientID)
        _rh.AccessToken = myAccessToken.Token

    End Sub

#End Region

#Region "Methods"

    ''' <summary>
    ''' Establishes a connection to the service using the 
    ''' calling user's credentials.
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks>Stores an access token to be used in subsequent calls.</remarks>
    Public Function Connect() As Boolean

        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":Connect"
        Dim strDefault As String = "Method failed."
        Try

            AuthorizeConnection()

            Return True
        Catch ex As Exception
            Debug.Print("Error: " & strTrace & " " & ex.Message)
            Return False
        End Try

    End Function

    ''' <summary>
    ''' Returns True if the current saved token is still valid.,
    ''' </summary>
    ''' <returns>Boolean:</returns>
    ''' <remarks></remarks>
    Public Function IsTokenValid() As Boolean

        Dim strResponse As String = _rh.APIRequest("GET", "lists")

        If String.IsNullOrEmpty(strResponse) Then
            Return False
        Else
            Return True
        End If

    End Function

#Region "Synchronous Calls"

    ' --- User Management

    ''' <summary>
    ''' Retrieve the currently logged in user's profile information
    ''' </summary>
    ''' <returns>Wunderlist.User:</returns>
    ''' <remarks></remarks>
    Public Function GetMyUser() As User

        ' GET a.wunderlist.com/api/v1/user. Response = 200
        ' 
        ' {
        '  "id": 6234958,
        '  "name": "BENCHMARK",
        '  "email": "benchmark@example.com",
        '  "created_at": "2013-08-30T08:25:58.000Z",
        '  "revision": 1
        ' }

        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":GetMyUser"
        Dim strDefault As String = "Method failed."
        Try
            strTrace = "Make the request."
            Dim strResponse As String = _rh.APIRequest("GET", "user")
            If String.IsNullOrEmpty(strResponse) Then
                strTrace = "Failed to retrieve the current user's profile information" & _
                                " - HTTP status code: " & _rh.StatusCode & ", service msg: " & _
                                _rh.ErrorMessage
                Throw New Exception(strDefault)
            End If

            strTrace = "Process the response."
            Dim myUser As New User
            myUser = Newtonsoft.Json.JsonConvert.DeserializeObject(strResponse, myUser.GetType)

            strTrace = "Inform listeners."
            RaiseEvent ItemUpdated(Me, New ItemEventArgs(myUser, ItemEventArgs.enuAction.None))

            ' Log Action
            strTrace = "Retrieved user: " & myUser.ID & "."
            LogAction("GUSR", strTrace, strRoutine)

            Return myUser

        Catch ex As Exception
            LogAction("GUSR_FAIL", strTrace & " " & ex.Message, strRoutine)
            LogErr(strTrace, ex, strRoutine)
            Return Nothing
        End Try


    End Function

    ' --- List Management

    ''' <summary>
    ''' Returns a collection of lists from the service.
    ''' </summary>
    ''' <returns>List(of Wunderlist.List):</returns>
    ''' <remarks></remarks>
    Public Function GetLists() As List(Of List)

        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":GetLists"
        Dim strDefault As String = "Method failed."
        Try

            strTrace = "Make the request."
            Dim strResponse As String = _rh.APIRequest("GET", "lists")
            If String.IsNullOrEmpty(strResponse) Then
                strTrace = "Failed to retrieve the user's list of lists" & _
                                " - HTTP status code: " & _rh.StatusCode & ", service msg: " & _
                                _rh.ErrorMessage
                Throw New Exception(strDefault)
            End If

            strTrace = "Process the response."
            Dim myLists As New List(Of List)
            myLists = Newtonsoft.Json.JsonConvert.DeserializeObject(strResponse, myLists.GetType)

            strTrace = "Inform listeners."
            Dim cev As New CollectionEventArgs(myLists)
            RaiseEvent CollectionUpdated(Me, cev)

            ' Log Action
            strTrace = "Retrieved " & myLists.Count.ToString & " lists."
            LogAction("GLSTCOLL", strTrace, strRoutine)

            Return myLists

        Catch ex As Exception
            LogAction("GLSTCOLL_FAIL", strTrace & " " & ex.Message, strRoutine)
            LogErr(strTrace, ex, strRoutine)
            Return Nothing
        End Try

    End Function

    ''' <summary>
    ''' Returns the specified list.
    ''' </summary>
    ''' <param name="listId">String: List identifier</param>
    ''' <returns>Wunderlist.List:</returns>
    ''' <remarks></remarks>
    Public Function GetList(ByVal listId As String) As List

        ' GET a.wunderlist.com/api/v1/lists/:id

        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":GetList"
        Dim strDefault As String = "Method failed."
        Try
            If String.IsNullOrEmpty(listId) Then
                strTrace = "A list identifier is required."
                Throw New Exception(strDefault)
            End If

            strTrace = "Make the request."
            Dim strResponse As String = _rh.APIRequest("GET", "lists/" & listId)
            If String.IsNullOrEmpty(strResponse) Then
                strTrace = "Failed to retrieve the specified list, id: " & listId & _
                                " - HTTP status code: " & _rh.StatusCode & ", service msg: " & _
                                _rh.ErrorMessage
                Throw New Exception(strDefault)
            End If

            strTrace = "Process the response."
            Dim myList As New List
            myList = Newtonsoft.Json.JsonConvert.DeserializeObject(strResponse, myList.GetType)

            strTrace = "Inform listeners."
            RaiseEvent ItemUpdated(Me, New ItemEventArgs(myList, ItemEventArgs.enuAction.None))

            ' Log Action
            strTrace = "Retrieved list: " & myList.ID & "."
            LogAction("GLST", strTrace, strRoutine)

            Return myList

        Catch ex As Exception
            LogAction("GLST_FAIL", strTrace & " " & ex.Message, strRoutine)
            LogErr(strTrace, ex, strRoutine)
            Return Nothing
        End Try

    End Function

    ''' <summary>
    ''' Creates a new list on the service.
    ''' </summary>
    ''' <param name="ListTitle">String:</param>
    ''' <returns>WunderList.List:</returns>
    ''' <remarks></remarks>
    Public Function CreateList(ByVal ListTitle As String) As List

        ' POST a.wunderlist.com/api/v1/lists
        '
        ' Request Body
        '   {
        '       "title": "Hallo"
        '   }
        ' Returns
        '   List Json

        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":CreateList"
        Dim strDefault As String = "Method failed."
        Try
            If String.IsNullOrEmpty(ListTitle) Then
                strTrace = "Cannot create a list without a Title."
                Throw New Exception(strDefault)
            End If

            strTrace = "Construct the request body."
            Dim strPost As String = "{""title"":""" & ListTitle & """}"

            strTrace = "Make the request."
            Dim strResponse As String = _rh.APIRequest("POST", "lists", strPost)
            If String.IsNullOrEmpty(strResponse) Then
                strTrace = "Failed to create a new list" & _
                                " - HTTP status code: " & _rh.StatusCode & ", service msg: " & _
                                _rh.ErrorMessage
                Throw New Exception(strDefault)
            End If

            strTrace = "Process the response."
            Dim myList As New List
            myList = Newtonsoft.Json.JsonConvert.DeserializeObject(strResponse, myList.GetType)

            strTrace = "Inform listeners."
            RaiseEvent ItemAdded(Me, New ItemEventArgs(myList, ItemEventArgs.enuAction.Insert))

            ' Log Action
            strTrace = "Created list: " & myList.ID & "."
            LogAction("ADDLST", strTrace, strRoutine)

            Return myList

        Catch ex As Exception
            LogAction("ADDLST_FAIL", strTrace & " " & ex.Message, strRoutine)
            LogErr(strTrace, ex, strRoutine)
            Return Nothing
        End Try

    End Function

    ''' <summary>
    ''' Updates a list's title
    ''' </summary>
    ''' <param name="updList">Wunderlist.List:</param>
    ''' <returns>Wunderlist.List:</returns>
    ''' <remarks></remarks>
    Public Function UpdateList(ByVal updList As List) As List

        ' PATCH a.wunderlist.com/api/v1/lists/:id, Response = 200
        '
        '   Request Body
        ' {
        '  "revision": 1000,
        '  "title": "Hallo"
        ' }
        '
        '   Response - 200
        ' {
        '  "id": 409233670,
        '  "revision": 1001,   <--- incremented??
        '  "title": "Hello",
        '  "type": "list"
        ' }

        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":UpdateList"
        Dim strDefault As String = "Method failed."
        Try
            If IsNothing(updList) Then
                strTrace = "A list is required."
                Throw New Exception(strDefault)
            End If

            strTrace = "Construct the request body."
            Dim strPost As String = "{""revision"":" & updList.revision.ToString & _
                                    ",""title"":""" & updList.title & _
                                    """}"

            strTrace = "Make the request."
            Dim strResponse As String = _rh.APIRequest("PATCH", "lists/" & updList.ID, strPost)
            If String.IsNullOrEmpty(strResponse) Then
                strTrace = "Failed to update the user's list " & _
                                " - HTTP status code: " & _rh.StatusCode & ", service msg: " & _
                                _rh.ErrorMessage

                Throw New Exception(strDefault)
            End If

            strTrace = "Process the response."
            Dim myList As New List
            myList = Newtonsoft.Json.JsonConvert.DeserializeObject(strResponse, myList.GetType)

            strTrace = "Inform listeners."
            RaiseEvent ItemUpdated(Me, New ItemEventArgs(myList, ItemEventArgs.enuAction.Update))

            ' Log Action
            strTrace = "Updated list: " & myList.ID & "."
            LogAction("UPDLST", strTrace, strRoutine)

            Return myList

        Catch ex As Exception
            LogAction("UPDLST_FAIL", strTrace & " " & ex.Message, strRoutine)
            LogErr(strTrace, ex, strRoutine)
            Return Nothing
        End Try

    End Function

    ''' <summary>
    ''' Removes the specified list from the service
    ''' </summary>
    ''' <param name="ListId">String: Uniqued id for the list</param>
    ''' <param name="Revision">Long: Revision of the list</param>
    ''' <returns>Boolean: Returns True if successful</returns>
    ''' <remarks></remarks>
    Public Function RemoveList(ByVal ListId As String, ByVal Revision As Long) As Boolean

        ' DELETE a.wunderlist.com/api/v1/lists/:id?revision=nn, Response = 204

        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":RemoveList"
        Dim strDefault As String = "Method failed."
        Try
            If String.IsNullOrEmpty(ListId) Then
                strTrace = "No list was identified."
                Throw New Exception(strDefault)
            End If
            If Revision <= 0 Then
                strTrace = "Invalid revision."
                Throw New Exception(strDefault)
            End If

            Dim query As New Dictionary(Of String, Object)
            query.Add("revision", Revision)

            ' returns an empty response and status code = 204 when successful
            Dim strResponse As String = _rh.APIRequest("DELETE", "lists/" & ListId, query)
            If _rh.StatusCode <> 204 Then
                strTrace = "Failed to delete the specified list '" & ListId & _
                                " - HTTP status code: " & _rh.StatusCode & ", service msg: " & _
                                _rh.ErrorMessage

                Throw New Exception(strDefault)
            Else
                strTrace = "Successfully removed list, id:" & ListId & "."
            End If

            ' Inform listeners.
            Dim myDesc As New ObjectDescriptor(enuObjectClass.List, ListId)
            RaiseEvent ItemDeleted(Me, New ItemEventArgs(myDesc, ItemEventArgs.enuAction.Delete))

            ' Log Action
            LogAction("DELLST", strTrace, strRoutine)

            Return True

        Catch ex As Exception
            LogAction("DELLST_FAIL", strTrace & " " & ex.Message, strRoutine)
            LogErr(strTrace, ex, strRoutine)
            Return False
        End Try

    End Function

    ' --- Task Management

    ''' <summary>
    ''' Retrieves the active tasks from the specified list.
    ''' </summary>
    ''' <param name="ListID">String: List identifier</param>
    ''' <returns>List(Of Task):</returns>
    ''' <remarks>Does not get the 'completed' tasks - use GetTasks(list_id, completed) query
    ''' to return the completed tasks.</remarks>
    Public Function GetTasks(ByVal ListID As String) As List(Of Task)

        ' GET a.wunderlist.com/api/v1/tasks?list_id=nn, Response = 200

        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":GetTasks_1"
        Dim strDefault As String = "Method failed."
        Try
            If String.IsNullOrEmpty(ListID) Then
                strTrace = "A list identifier is required."
                Throw New Exception(strDefault)
            End If

            strTrace = "Construct the query string."
            Dim query As New Dictionary(Of String, Object)
            query.Add("list_id", ListID)

            strTrace = "Make the request."
            Dim strResponse As String = _rh.APIRequest("GET", "tasks", query)
            If String.IsNullOrEmpty(strResponse) Then
                strTrace = "Failed to retrieve the user's tasks from list id: " & ListID & _
                                " - HTTP status code: " & _rh.StatusCode & ", service msg: " & _
                                _rh.ErrorMessage

                Throw New Exception(strDefault)
            End If

            strTrace = "Process the response."
            Dim myTasks As New List(Of Task)
            myTasks = Newtonsoft.Json.JsonConvert.DeserializeObject(strResponse, myTasks.GetType)

            strTrace = "Inform listeners."
            Dim cev As New CollectionEventArgs(myTasks)
            RaiseEvent CollectionUpdated(Me, cev)

            ' Log Action
            strTrace = "Retrieved " & myTasks.Count.ToString & " tasks."
            LogAction("GTCOLL", strTrace, strRoutine)

            Return myTasks

        Catch ex As Exception
            LogAction("GTCOLL_FAIL", strTrace & " " & ex.Message, strRoutine)
            LogErr(strTrace, ex, strRoutine)
            Return Nothing
        End Try

    End Function

    ''' <summary>
    ''' Retrieves the complete or incomplete tasks from the specified list.
    ''' </summary>
    ''' <param name="ListID">String: List Identifier</param>
    ''' <param name="completed">Boolean: TRUE = complete</param>
    ''' <returns>List(Of Task):</returns>
    ''' <remarks></remarks>
    Public Function GetTasks(ByVal ListID As String, ByVal completed As Boolean) As List(Of Task)

        ' GET a.wunderlist.com/api/v1/tasks?list_id=nn&completed=true, Response = 200

        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":GetTasks_2"
        Dim strDefault As String = "Method failed."
        Try
            If String.IsNullOrEmpty(ListID) Then
                strTrace = "A list identifier is required."
                Throw New Exception(strDefault)
            End If

            strTrace = "Construct the query string."
            Dim query As New Dictionary(Of String, Object)
            query.Add("list_id", ListID)
            query.Add("completed", completed)

            strTrace = "Make the request."
            Dim strResponse As String = _rh.APIRequest("GET", "tasks", query)
            If String.IsNullOrEmpty(strResponse) Then
                strTrace = "Failed to retrieve the user's tasks from list id: " & ListID & _
                                " - HTTP status code: " & _rh.StatusCode & ", service msg: " & _
                                _rh.ErrorMessage

                Throw New Exception(strDefault)
            End If

            strTrace = "Process the response."
            Dim myTasks As New List(Of Task)
            myTasks = Newtonsoft.Json.JsonConvert.DeserializeObject(strResponse, myTasks.GetType)

            strTrace = "Inform listeners."
            Dim cev As New CollectionEventArgs(myTasks)
            RaiseEvent CollectionUpdated(Me, cev)

            ' Log Action
            strTrace = "Retrieved " & myTasks.Count.ToString & " tasks."
            LogAction("GTCOLL", strTrace, strRoutine)

            Return myTasks

        Catch ex As Exception
            LogAction("GTCOLL_FAIL", strTrace & " " & ex.Message, strRoutine)
            LogErr(strTrace, ex, strRoutine)
            Return Nothing
        End Try

    End Function

    ''' <summary>
    ''' Retrieves the specified task from the service.
    ''' </summary>
    ''' <param name="TaskId">String: task identifier</param>
    ''' <returns>Wunderlist.Task:</returns>
    ''' <remarks></remarks>
    Public Function GetTask(ByVal TaskId As String) As Task

        ' GET a.wunderlist.com/api/v1/tasks/:id, Response = 200

        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":GetTask"
        Dim strDefault As String = "Method failed."
        Try
            If String.IsNullOrEmpty(TaskId) Then
                strTrace = "Invalid task identifier"
                Throw New Exception(strDefault)
            End If

            strTrace = "Make the request."
            Dim strResponse As String = _rh.APIRequest("GET", "tasks/" & TaskId)
            If String.IsNullOrEmpty(strResponse) Then
                strTrace = "Failed to retrieve the specified task, id: " & TaskId & _
                                " - HTTP status code: " & _rh.StatusCode & ", service msg: " & _
                                _rh.ErrorMessage
                Throw New Exception(strDefault)
            End If

            strTrace = "Process the response."
            Dim myTask As New Task
            myTask = Newtonsoft.Json.JsonConvert.DeserializeObject(strResponse, myTask.GetType)

            strTrace = "Inform listeners."
            RaiseEvent ItemUpdated(Me, New ItemEventArgs(myTask, ItemEventArgs.enuAction.None))

            ' Log Action
            strTrace = "Retrieved task: " & myTask.ID & "."
            LogAction("GTSK", strTrace, strRoutine)

            Return myTask

        Catch ex As Exception
            LogAction("GTSK_FAIL", strTrace & " " & ex.Message, strRoutine)
            LogErr(strTrace, ex, strRoutine)
            Return Nothing
        End Try

    End Function

    ''' <summary>
    ''' Adds a new task to the list specified by the insTask parameter
    ''' </summary>
    ''' <param name="insTask">Wunderlist.Task:</param>
    ''' <returns>Wunderlist.Task:</returns>
    ''' <remarks></remarks>
    Public Function InsertTask(ByVal insTask As Task) As Task

        ' POST a.wunderlist.com/api/v1/tasks, Response = 201
        '   Request Body
        ' {
        '  "list_id": -12345,
        '  "title": "Hallo",
        '  "assignee_id": 123,
        '  "completed": true,
        '  "due_date": "2013-08-30",
        '  "starred": false
        ' }
        '   Response
        ' {
        '  "id": 409233670,
        '  "assignee_id": 123,
        '  "created_at": "2013-08-30T08:36:13.273Z",
        '  "created_by_id": 6234958,
        '  "due_date": "2013-08-30",
        '  "list_id": -12345,
        '  "revision": 1,
        '  "starred": false,
        '  "title": "Hello"
        ' }

        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":InsertTask"
        Dim strDefault As String = "Method failed."
        Try
            If IsNothing(insTask) Then
                strTrace = "A null task is not allowed."
                Throw New Exception(strDefault)
            End If

            strTrace = "Prepare the request body."
            Dim strPost As String = insTask.ToStringJson

            strTrace = "Make the request."
            Dim strResponse As String = _rh.APIRequest("POST", "tasks", strPost)
            If String.IsNullOrEmpty(strResponse) Then
                strTrace = "Failed to create a new list" & _
                                " - HTTP status code: " & _rh.StatusCode & ", service msg: " & _
                                _rh.ErrorMessage
                Throw New Exception(strDefault)
            End If

            strTrace = "Process the response."
            Dim myTask As New Task
            myTask = Newtonsoft.Json.JsonConvert.DeserializeObject(strResponse, myTask.GetType)

            strTrace = "Inform listeners."
            RaiseEvent ItemAdded(Me, New ItemEventArgs(myTask, ItemEventArgs.enuAction.Insert))

            ' Log Action
            strTrace = "Inserted task: " & myTask.ID & "."
            LogAction("ADDTSK", strTrace, strRoutine)

            Return myTask

        Catch ex As Exception
            LogAction("ADDTSK_FAIL", strTrace & " " & ex.Message, strRoutine)
            LogErr(strTrace, ex, strRoutine)
            Return Nothing
        End Try

    End Function

    ''' <summary>
    ''' Updates an existing task using the properties specified
    ''' in the updTask parameter
    ''' </summary>
    ''' <param name="updTask">Wunderlist.Task:</param>
    ''' <returns>Wunderlist.Task:</returns>
    ''' <remarks></remarks>
    Public Function UpdateTask(ByVal updTask As Task) As Task

        ' PATCH a.wunderlist.com/api/v1/tasks/:id, Response = 200
        '   Request Body
        ' {
        '  "revision": 999,   <--- required
        '  "title": "Hallo",
        '  "completed": true,
        '  "due_date": "2013-08-30",
        '  "starred": false,
        '  "remove": ["assignee_id"]
        ' }
        '   Response Body
        ' {
        '  "id": 409233670,
        '  "created_at": "2013-08-30T08:36:13.273Z",
        '  "created_by_id": 6234958,
        '  "due_date": "2013-08-30",
        '  "list_id": -12345,
        '  "revision": 1001,
        '  "starred": false,
        '  "title": "Hello"
        ' }

        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":UpdateTask"
        Dim strDefault As String = "Method failed."
        Try
            If IsNothing(updTask) Then
                strTrace = "A task is required."
                Throw New Exception(strDefault)
            End If

            strTrace = "Construct the updated properties Json."
            Dim strPost As String = updTask.ToStringJson

            strTrace = "Make the request."
            Dim strResponse As String = _rh.APIRequest("PATCH", "tasks/" & updTask.ID, strPost)
            If String.IsNullOrEmpty(strResponse) Then
                strTrace = "Failed to update the user's list " & _
                                " - HTTP status code: " & _rh.StatusCode & ", service msg: " & _
                                _rh.ErrorMessage

                Throw New Exception(strDefault)
            End If

            strTrace = "Process the response,"
            Dim myTask As New Task
            myTask = Newtonsoft.Json.JsonConvert.DeserializeObject(strResponse, myTask.GetType)

            strTrace = "Inform listeners."
            RaiseEvent ItemUpdated(Me, New ItemEventArgs(myTask, ItemEventArgs.enuAction.Update))

            ' Log Action
            strTrace = "Updated task: " & myTask.ID & "."
            LogAction("UPDTSK", strTrace, strRoutine)

            Return myTask

        Catch ex As Exception
            LogAction("UPDTSK_FAIL", strTrace & " " & ex.Message, strRoutine)
            LogErr(strTrace, ex, strRoutine)
            Return Nothing
        End Try

    End Function

    ''' <summary>
    ''' Removes the specified task from the service
    ''' </summary>
    ''' <param name="TaskId">String: Uniqued id for the task</param>
    ''' <param name="Revision">Long: Revision of the task</param>
    ''' <returns>Boolean: Returns True if successful</returns>
    ''' <remarks></remarks>
    Public Function RemoveTask(ByVal TaskId As String, ByVal Revision As Long) As Boolean

        ' DELETE a.wunderlist.com/api/v1/tasks/:id&revision=nn, Response = 204
        '
        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":RemoveTask"
        Dim strDefault As String = "Method failed."
        Try
            If String.IsNullOrEmpty(TaskId) Then
                strTrace = "No list was identified."
                Throw New Exception(strDefault)
            End If
            If Revision <= 0 Then
                strTrace = "Invalid revision."
                Throw New Exception(strDefault)
            End If

            strTrace = "Construct the query string."
            Dim query As New Dictionary(Of String, Object)
            query.Add("revision", Revision)

            ' returns an empty response and status code = 204 when successful
            strTrace = "Make the request"
            Dim strResponse As String = _rh.APIRequest("DELETE", "tasks/" & TaskId, query)
            If _rh.StatusCode <> 204 Then
                strTrace = "Failed to delete the specified task '" & TaskId & _
                                " - HTTP status code: " & _rh.StatusCode & ", service msg: " & _
                                _rh.ErrorMessage

                Throw New Exception(strDefault)
            Else
                strTrace = "Successfully removed task, id:" & TaskId & "."
            End If

            ' Inform listeners
            Dim myDesc As New ObjectDescriptor(enuObjectClass.Task, TaskId)
            RaiseEvent ItemDeleted(Me, New ItemEventArgs(myDesc, ItemEventArgs.enuAction.Delete))

            ' Log Action
            LogAction("DELTSK", strTrace, strRoutine)

            Return True

        Catch ex As Exception
            LogAction("DELTSK_FAIL", strTrace & " " & ex.Message, strRoutine)
            LogErr(strTrace, ex, strRoutine)
            Return False
        End Try

    End Function

#End Region

#Region "Asynchronous calls."

    ' --- User Management

    ''' <summary>
    ''' Retrieve the currently logged in user's profile information off
    ''' the UI thread.
    ''' </summary>
    ''' <remarks></remarks>
    Public Async Sub GetMyUserAsync()

        ' GET a.wunderlist.com/api/v1/user. Response = 200
        ' 
        ' {
        '  "id": 6234958,
        '  "name": "BENCHMARK",
        '  "email": "benchmark@example.com",
        '  "created_at": "2013-08-30T08:25:58.000Z",
        '  "revision": 1
        ' }

        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":GetMyUserAsync"
        Dim strDefault As String = "Method failed."
        Try
            strTrace = "Make the request."
            Dim strResponse As String = Await _rh.APIRequestAsync("GET", "user")
            If String.IsNullOrEmpty(strResponse) Then
                strTrace = "Failed to retrieve the current user's profile information" & _
                                " - HTTP status code: " & _rh.StatusCode & ", service msg: " & _
                                _rh.ErrorMessage
                Throw New Exception(strDefault)
            End If

            strTrace = "Process the response."
            Dim myUser As New User
            myUser = Newtonsoft.Json.JsonConvert.DeserializeObject(strResponse, myUser.GetType)

            strTrace = "Inform listeners."
            RaiseEvent ItemUpdated(Me, New ItemEventArgs(myUser, ItemEventArgs.enuAction.None))

            ' Log Action
            strTrace = "Retrieved user: " & myUser.ID & "."
            LogAction("GUSR", strTrace, strRoutine)

        Catch ex As Exception
            LogAction("GUSR_FAIL", strTrace & " " & ex.Message, strRoutine)
            LogErr(strTrace, ex, strRoutine)
        End Try


    End Sub

    ' --- List Management

    ''' <summary>
    ''' Returns a collection of lists from the service asynchronously
    ''' </summary>
    ''' <remarks></remarks>
    Public Async Sub GetListsAsync()

        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":GetListsAsync"
        Dim strDefault As String = "Method failed."
        Try

            strTrace = "Make the request."
            Dim strResponse As String = Await _rh.APIRequestAsync("GET", "lists")
            If String.IsNullOrEmpty(strResponse) Then
                strTrace = "Failed to retrieve the user's list of lists" & _
                        " - HTTP status code: " & _rh.StatusCode & ", service msg: " & _
                          _rh.ErrorMessage
                Throw New Exception(strDefault)
            End If

            strTrace = "Process the response."
            Dim myLists As New List(Of List)
            myLists = Newtonsoft.Json.JsonConvert.DeserializeObject(strResponse, myLists.GetType)

            strTrace = "Inform listeners."
            Dim cev As New CollectionEventArgs(myLists)
            RaiseEvent CollectionUpdated(Me, cev)

            ' Log Action
            strTrace = "Retrieved " & myLists.Count.ToString & " lists."
            LogAction("GLSTCOLL", strTrace, strRoutine)

        Catch ex As Exception
            LogAction("GLSTCOLL_FAIL", strTrace & " " & ex.Message, strRoutine)
            LogErr(strTrace, ex, strRoutine)
        End Try

    End Sub

    ''' <summary>
    ''' Returns the specified list.
    ''' </summary>
    ''' <param name="listId">String: List identifier</param>
    ''' <remarks></remarks>
    Public Async Sub GetListAsync(ByVal listId As String)

        ' GET a.wunderlist.com/api/v1/lists/:id

        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":GetListAync"
        Dim strDefault As String = "Method failed."
        Try
            If String.IsNullOrEmpty(listId) Then
                strTrace = "A list identifier is required."
                Throw New Exception(strDefault)
            End If

            strTrace = "Make the request."
            Dim strResponse As String = Await _rh.APIRequestAsync("GET", "lists/" & listId)
            If String.IsNullOrEmpty(strResponse) Then
                strTrace = "Failed to retrieve the specified list, id: " & listId & _
                                " - HTTP status code: " & _rh.StatusCode & ", service msg: " & _
                                _rh.ErrorMessage
                Throw New Exception(strDefault)
            End If

            strTrace = "Process the response."
            Dim myList As New List
            myList = Newtonsoft.Json.JsonConvert.DeserializeObject(strResponse, myList.GetType)

            strTrace = "Inform listeners."
            RaiseEvent ItemUpdated(Me, New ItemEventArgs(myList, ItemEventArgs.enuAction.None))

            ' Log Action
            strTrace = "Retrieved list: " & myList.ID & "."
            LogAction("GLST", strTrace, strRoutine)

        Catch ex As Exception
            LogAction("GLST_FAIL", strTrace & " " & ex.Message, strRoutine)
            LogErr(strTrace, ex, strRoutine)
        End Try
    End Sub

    ''' <summary>
    ''' Creates a new list on the service.
    ''' </summary>
    ''' <param name="ListTitle">String:</param>
    ''' <remarks></remarks>
    Public Async Sub CreateListAsync(ByVal ListTitle As String)

        ' POST a.wunderlist.com/api/v1/lists
        '
        ' Request Body
        '   {
        '       "title": "Hallo"
        '   }
        ' Returns
        '   List Json

        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":CreateListAsync"
        Dim strDefault As String = "Method failed."
        Try
            If String.IsNullOrEmpty(ListTitle) Then
                strTrace = "Cannot create a list without a Title."
                Throw New Exception(strDefault)
            End If

            strTrace = "Construct the request body."
            Dim strPost As String = "{""title"":""" & ListTitle & """}"

            strTrace = "Make the request."
            Dim strResponse As String = Await _rh.APIRequestAsync("POST", "lists", strPost)
            If String.IsNullOrEmpty(strResponse) Then
                strTrace = "Failed to create a new list" & _
                                " - HTTP status code: " & _rh.StatusCode & ", service msg: " & _
                                _rh.ErrorMessage
                Throw New Exception(strDefault)
            End If

            strTrace = "Process the response."
            Dim myList As New List
            myList = Newtonsoft.Json.JsonConvert.DeserializeObject(strResponse, myList.GetType)

            strTrace = "Inform listeners."
            RaiseEvent ItemAdded(Me, New ItemEventArgs(myList, ItemEventArgs.enuAction.Insert))

            ' Log Action
            strTrace = "Created list: " & myList.ID & "."
            LogAction("ADDLST", strTrace, strRoutine)

        Catch ex As Exception
            LogAction("ADDLST_FAIL", strTrace & " " & ex.Message, strRoutine)
            LogErr(strTrace, ex, strRoutine)
        End Try

    End Sub

    ''' <summary>
    ''' Updates a list's title
    ''' </summary>
    ''' <param name="updList">Wunderlist.List:</param>
    ''' <remarks></remarks>
    Public Async Sub UpdateListAsync(ByVal updList As List)

        ' PATCH a.wunderlist.com/api/v1/lists/:id, Response = 200
        '
        '   Request Body
        ' {
        '  "revision": 1000,
        '  "title": "Hallo"
        ' }
        '
        '   Response - 200
        ' {
        '  "id": 409233670,
        '  "revision": 1001,   <--- incremented??
        '  "title": "Hello",
        '  "type": "list"
        ' }

        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":UpdateListAsync"
        Dim strDefault As String = "Method failed."
        Try
            If IsNothing(updList) Then
                strTrace = "A list is required."
                Throw New Exception(strDefault)
            End If

            strTrace = "Construct the request body."
            Dim strPost As String = "{""revision"":" & updList.revision.ToString & _
                                    ",""title"":""" & updList.title & _
                                    """}"

            strTrace = "Make the request."
            Dim strResponse As String = Await _rh.APIRequestAsync("PATCH", "lists/" & updList.ID, strPost)
            If String.IsNullOrEmpty(strResponse) Then
                strTrace = "Failed to update the user's list " & _
                                " - HTTP status code: " & _rh.StatusCode & ", service msg: " & _
                                _rh.ErrorMessage

                Throw New Exception(strDefault)
            End If

            strTrace = "Process the response."
            Dim myList As New List
            myList = Newtonsoft.Json.JsonConvert.DeserializeObject(strResponse, myList.GetType)

            strTrace = "Inform listeners."
            RaiseEvent ItemUpdated(Me, New ItemEventArgs(myList, ItemEventArgs.enuAction.Update))

            ' Log Action
            strTrace = "Updated list: " & myList.ID & "."
            LogAction("UPDLST", strTrace, strRoutine)

        Catch ex As Exception
            LogAction("UPDLST_FAIL", strTrace & " " & ex.Message, strRoutine)
            LogErr(strTrace, ex, strRoutine)
        End Try

    End Sub

    ''' <summary>
    ''' Removes the specified list from the service
    ''' </summary>
    ''' <param name="ListId">String: Uniqued id for the list</param>
    ''' <param name="Revision">Long: Revision of the list</param>
    ''' <remarks></remarks>
    Public Async Sub RemoveListAsync(ByVal ListId As String, ByVal Revision As Long)

        ' DELETE a.wunderlist.com/api/v1/lists/:id?revision=nn, Response = 204

        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":RemoveListAsync"
        Dim strDefault As String = "Method failed."
        Try
            If String.IsNullOrEmpty(ListId) Then
                strTrace = "No list was identified."
                Throw New Exception(strDefault)
            End If
            If Revision <= 0 Then
                strTrace = "Invalid revision."
                Throw New Exception(strDefault)
            End If

            Dim query As New Dictionary(Of String, Object)
            query.Add("revision", Revision)

            ' returns an empty response and status code = 204 when successful
            Dim strResponse As String = Await _rh.APIRequestAsync("DELETE", "lists/" & ListId, query)
            If _rh.StatusCode <> 204 Then
                strTrace = "Failed to delete the specified list '" & ListId & _
                                " - HTTP status code: " & _rh.StatusCode & ", service msg: " & _
                                _rh.ErrorMessage

                Throw New Exception(strDefault)
            Else
                strTrace = "Successfully removed list, id:" & ListId & "."
            End If

            ' Inform listeners.
            Dim myDesc As New ObjectDescriptor(enuObjectClass.List, ListId)
            RaiseEvent ItemDeleted(Me, New ItemEventArgs(myDesc, ItemEventArgs.enuAction.Delete))

            ' Log Action
            LogAction("DELLST", strTrace, strRoutine)

        Catch ex As Exception
            LogAction("DELLST_FAIL", strTrace & " " & ex.Message, strRoutine)
            LogErr(strTrace, ex, strRoutine)
        End Try

    End Sub

    ' --- Task Management

    ''' <summary>
    ''' Retrieves the active tasks from the specified list asynchrously.
    ''' </summary>
    ''' <param name="ListID">String: List identifier</param>
    ''' <remarks></remarks>
    Public Async Sub GetTasksAsync(ByVal ListID As String)

        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":GetTasksAsync_1"
        Dim strDefault As String = "Method failed."
        Try
            If String.IsNullOrEmpty(ListID) Then
                strTrace = "A list identifier is required."
                Throw New Exception(strDefault)
            End If

            strTrace = "Construct the query string."
            Dim query As New Dictionary(Of String, Object)
            query.Add("list_id", ListID)

            strTrace = "Make the request."
            Dim strResponse As String = Await _rh.APIRequestAsync("GET", "tasks", query)
            If String.IsNullOrEmpty(strResponse) Then
                strTrace = "Failed to retrieve the user's tasks from list: " & ListID & _
                          " - HTTP status code: " & _rh.StatusCode & ", service msg: " & _
                          _rh.ErrorMessage
                Throw New Exception(strDefault)
            End If

            strTrace = "Process the response"
            Dim myTasks As New List(Of Task)
            myTasks = Newtonsoft.Json.JsonConvert.DeserializeObject(strResponse, myTasks.GetType)

            strTrace = "Inform listeners."
            Dim cev As New CollectionEventArgs(myTasks)
            RaiseEvent CollectionUpdated(Me, cev)

            ' Log Action
            strTrace = "Retrieved " & myTasks.Count.ToString & " tasks."
            LogAction("GTCOLL", strTrace, strRoutine)

        Catch ex As Exception
            LogAction("GTCOLL_FAIL", strTrace & " " & ex.Message, strRoutine)
            LogErr(strTrace, ex, strRoutine)
        End Try

    End Sub

    ''' <summary>
    ''' Retrieves the complete or incomplete tasks from the specified list.
    ''' </summary>
    ''' <param name="ListID">String: List Identifier</param>
    ''' <param name="completed">Boolean: TRUE = complete</param>
    ''' <remarks></remarks>
    Public Async Sub GetTasksAsync(ByVal ListID As String, ByVal completed As Boolean)

        ' GET a.wunderlist.com/api/v1/tasks?list_id=nn&completed=true, Response = 200

        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":GetTasksAsync_2"
        Dim strDefault As String = "Method failed."
        Try
            If String.IsNullOrEmpty(ListID) Then
                strTrace = "A list identifier is required."
                Throw New Exception(strDefault)
            End If

            strTrace = "Construct the query string."
            Dim query As New Dictionary(Of String, Object)
            query.Add("list_id", ListID)
            query.Add("completed", completed)

            strTrace = "Make the request."
            Dim strResponse As String = Await _rh.APIRequestAsync("GET", "tasks", query)
            If String.IsNullOrEmpty(strResponse) Then
                strTrace = "Failed to retrieve the user's tasks from list id: " & ListID & _
                                " - HTTP status code: " & _rh.StatusCode & ", service msg: " & _
                                _rh.ErrorMessage

                Throw New Exception(strDefault)
            End If

            strTrace = "Process the response."
            Dim myTasks As New List(Of Task)
            myTasks = Newtonsoft.Json.JsonConvert.DeserializeObject(strResponse, myTasks.GetType)

            strTrace = "Inform listeners."
            Dim cev As New CollectionEventArgs(myTasks)
            RaiseEvent CollectionUpdated(Me, cev)

            ' Log Action
            strTrace = "Retrieved " & myTasks.Count.ToString & " tasks."
            LogAction("GTCOLL", strTrace, strRoutine)

        Catch ex As Exception
            LogAction("GTCOLL_FAIL", strTrace & " " & ex.Message, strRoutine)
            LogErr(strTrace, ex, strRoutine)
        End Try
    End Sub

    ''' <summary>
    ''' Retrieves the specified task from the service.
    ''' </summary>
    ''' <param name="TaskId">String: task identifier</param>
    ''' <remarks></remarks>
    Public Async Sub GetTaskAsync(ByVal TaskId As String)

        ' GET a.wunderlist.com/api/v1/tasks/:id, Response = 200

        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":GetTaskAsync"
        Dim strDefault As String = "Method failed."
        Try
            If String.IsNullOrEmpty(TaskId) Then
                strTrace = "Invalid task identifier"
                Throw New Exception(strDefault)
            End If

            strTrace = "Make the request."
            Dim strResponse As String = Await _rh.APIRequestAsync("GET", "tasks/" & TaskId)
            If String.IsNullOrEmpty(strResponse) Then
                strTrace = "Failed to retrieve the specified task, id: " & TaskId & _
                                " - HTTP status code: " & _rh.StatusCode & ", service msg: " & _
                                _rh.ErrorMessage
                Throw New Exception(strDefault)
            End If

            strTrace = "Process the response."
            Dim myTask As New Task
            myTask = Newtonsoft.Json.JsonConvert.DeserializeObject(strResponse, myTask.GetType)

            strTrace = "Inform listeners."
            RaiseEvent ItemUpdated(Me, New ItemEventArgs(myTask, ItemEventArgs.enuAction.None))

            ' Log Action
            strTrace = "Retrieved task: " & myTask.ID & "."
            LogAction("GTSK", strTrace, strRoutine)

        Catch ex As Exception
            LogAction("GTSK_FAIL", strTrace & " " & ex.Message, strRoutine)
            LogErr(strTrace, ex, strRoutine)
        End Try


    End Sub

    ''' <summary>
    ''' Adds a new task to the list specified by the insTask parameter
    ''' </summary>
    ''' <param name="insTask">Wunderlist.Task:</param>
    ''' <remarks></remarks>
    Public Async Sub InsertTaskAsync(ByVal insTask As Task)

        ' POST a.wunderlist.com/api/v1/tasks, Response = 201
        '   Request Body
        ' {
        '  "list_id": -12345,
        '  "title": "Hallo",
        '  "assignee_id": 123,
        '  "completed": true,
        '  "due_date": "2013-08-30",
        '  "starred": false
        ' }
        '   Response
        ' {
        '  "id": 409233670,
        '  "assignee_id": 123,
        '  "created_at": "2013-08-30T08:36:13.273Z",
        '  "created_by_id": 6234958,
        '  "due_date": "2013-08-30",
        '  "list_id": -12345,
        '  "revision": 1,
        '  "starred": false,
        '  "title": "Hello"
        ' }

        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":InsertTaskAsync"
        Dim strDefault As String = "Method failed."
        Try
            If IsNothing(insTask) Then
                strTrace = "A null task is not allowed."
                Throw New Exception(strDefault)
            End If

            strTrace = "Prepare the request body."
            Dim strPost As String = insTask.ToStringJson

            strTrace = "Make the request."
            Dim strResponse As String = Await _rh.APIRequestAsync("POST", "tasks", strPost)
            If String.IsNullOrEmpty(strResponse) Then
                strTrace = "Failed to create a new list" & _
                                " - HTTP status code: " & _rh.StatusCode & ", service msg: " & _
                                _rh.ErrorMessage
                Throw New Exception(strDefault)
            End If

            strTrace = "Process the response."
            Dim myTask As New Task
            myTask = Newtonsoft.Json.JsonConvert.DeserializeObject(strResponse, myTask.GetType)

            strTrace = "Inform listeners."
            RaiseEvent ItemAdded(Me, New ItemEventArgs(myTask, ItemEventArgs.enuAction.Insert))

            ' Log Action
            strTrace = "Inserted task: " & myTask.ID & "."
            LogAction("ADDTSK", strTrace, strRoutine)

        Catch ex As Exception
            LogAction("ADDTSK_FAIL", strTrace & " " & ex.Message, strRoutine)
            LogErr(strTrace, ex, strRoutine)
        End Try
    End Sub

    ''' <summary>
    ''' Updates an existing task using the properties specified
    ''' in the updTask parameter
    ''' </summary>
    ''' <param name="updTask">Wunderlist.Task:</param>
    ''' <remarks></remarks>
    Public Async Sub UpdateTaskAsync(ByVal updTask As Task)

        ' PATCH a.wunderlist.com/api/v1/tasks/:id, Response = 200
        '   Request Body
        ' {
        '  "revision": 999,   <--- required
        '  "title": "Hallo",
        '  "completed": true,
        '  "due_date": "2013-08-30",
        '  "starred": false,
        '  "remove": ["assignee_id"]
        ' }
        '   Response Body
        ' {
        '  "id": 409233670,
        '  "created_at": "2013-08-30T08:36:13.273Z",
        '  "created_by_id": 6234958,
        '  "due_date": "2013-08-30",
        '  "list_id": -12345,
        '  "revision": 1001,
        '  "starred": false,
        '  "title": "Hello"
        ' }

        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":UpdateTaskAsync"
        Dim strDefault As String = "Method failed."
        Try
            If IsNothing(updTask) Then
                strTrace = "A task is required."
                Throw New Exception(strDefault)
            End If

            strTrace = "Construct the updated properties Json."
            Dim strPost As String = updTask.ToStringJson

            strTrace = "Make the request."
            Dim strResponse As String = Await _rh.APIRequestAsync("PATCH", "tasks/" & updTask.ID, strPost)
            If String.IsNullOrEmpty(strResponse) Then
                strTrace = "Failed to update the user's list " & _
                                " - HTTP status code: " & _rh.StatusCode & ", service msg: " & _
                                _rh.ErrorMessage

                Throw New Exception(strDefault)
            End If

            strTrace = "Process the response,"
            Dim myTask As New Task
            myTask = Newtonsoft.Json.JsonConvert.DeserializeObject(strResponse, myTask.GetType)

            strTrace = "Inform listeners."
            RaiseEvent ItemUpdated(Me, New ItemEventArgs(myTask, ItemEventArgs.enuAction.Update))

            ' Log Action
            strTrace = "Updated task: " & myTask.ID & "."
            LogAction("UPDTSK", strTrace, strRoutine)

        Catch ex As Exception
            LogAction("UPDTSK_FAIL", strTrace & " " & ex.Message, strRoutine)
            LogErr(strTrace, ex, strRoutine)
        End Try

    End Sub

    ''' <summary>
    ''' Removes the specified task from the service
    ''' </summary>
    ''' <param name="TaskId">String: Uniqued id for the task</param>
    ''' <param name="Revision">Long: Revision of the task</param>
    ''' <remarks></remarks>
    Public Async Sub RemoveTaskAsync(ByVal TaskId As String, ByVal Revision As Long)


        ' DELETE a.wunderlist.com/api/v1/tasks/:id&revision=nn, Response = 204
        '
        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":RemoveTaskAsync"
        Dim strDefault As String = "Method failed."
        Try
            If String.IsNullOrEmpty(TaskId) Then
                strTrace = "No list was identified."
                Throw New Exception(strDefault)
            End If
            If Revision <= 0 Then
                strTrace = "Invalid revision."
                Throw New Exception(strDefault)
            End If

            strTrace = "Construct the query string."
            Dim query As New Dictionary(Of String, Object)
            query.Add("revision", Revision)

            ' returns an empty response and status code = 204 when successful
            strTrace = "Make the request"
            Dim strResponse As String = Await _rh.APIRequestAsync("DELETE", "tasks/" & TaskId, query)
            If _rh.StatusCode <> 204 Then
                strTrace = "Failed to delete the specified task '" & TaskId & _
                                " - HTTP status code: " & _rh.StatusCode & ", service msg: " & _
                                _rh.ErrorMessage

                Throw New Exception(strDefault)
            Else
                strTrace = "Successfully removed task, id:" & TaskId & "."
            End If

            ' Inform listeners
            Dim myDesc As New ObjectDescriptor(enuObjectClass.Task, TaskId)
            RaiseEvent ItemDeleted(Me, New ItemEventArgs(myDesc, ItemEventArgs.enuAction.Delete))

            ' Log Action
            LogAction("DELTSK", strTrace, strRoutine)

        Catch ex As Exception
            LogAction("DELTSK_FAIL", strTrace & " " & ex.Message, strRoutine)
            LogErr(strTrace, ex, strRoutine)
        End Try

    End Sub

#End Region

#End Region

#Region "Supporting Methods"

    Private Sub AuthorizeConnection()
        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = "clsSkyDrive:AuthorizeConnection"
        Try
            Dim bReturn As Boolean = False

            strTrace = "Set up a Windows Form"
            frmAuthorizationForm = New Windows.Forms.Form
            frmAuthorizationForm.Width = 900
            frmAuthorizationForm.Height = 700
            frmAuthorizationForm.Text = "Authorize " & My.Application.Info.ProductName & " to connect to Wunderlist."

            strTrace = "Set up a web browser control."
            wb_OAuth = New WebBrowser
            wb_OAuth.Name = "wbService"

            strTrace = "Add web browser to the form."
            frmAuthorizationForm.Controls.Add(wb_OAuth)
            frmAuthorizationForm.Controls("wbService").Dock = DockStyle.Fill

            strTrace = "Present the Service OAuth dialog."
            Dim uriCallBack As New Uri(_appKeys.CallbackURL)
            Dim myURI As Uri = GetAuthorizationEndpoint(_appKeys.ClientID, uriCallBack, "001")

            strTrace = myURI.AbsoluteUri

            Dim strURI As String = strTrace
            If strURI.Length > 0 Then
                strTrace = "Access Request."
                '   SyncLogger("SD_AuthRequest", strTrace, strRoutine)

                strTrace = "Access Request URL: '" & strURI & "'."
                '     TraceLogger(strTrace, strRoutine)

                strTrace = "Navigate web browser control to auth URI."
                wb_OAuth.Navigate(strURI)
                strTrace = "Show the form."
                frmAuthorizationForm.Show()
            End If

        Catch ex As Exception
            LogErr(strTrace, ex, strRoutine)
        End Try

    End Sub

    Private Function GetAuthorizationEndpoint(ByVal ClientID As String, _
                                              ByVal urlCallBack As Uri, _
                                              ByVal state As String) As Uri

        ' https://www.wunderlist.com/oauth/authorize?client_id=ID&redirect_uri=URL&state=RANDOM

        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":GetAuthorizationEndpoint"
        Dim strDefault As String = "Method failed."
        Try

            Dim strURL As String = "https://www.wunderlist.com/oauth/authorize?"

            strTrace = urlCallBack.AbsoluteUri
            Dim strCallBack As String = System.Web.HttpUtility.UrlEncode(strTrace)

            Dim sb As New StringBuilder
            sb.Append("client_id=" & ClientID & "&")
            sb.Append("redirect_uri=" & strCallBack & "&")
            sb.Append("state=" & state)

            strURL = strURL & sb.ToString

            Dim retURI As New Uri(strURL)

            Return retURI

        Catch ex As Exception
            LogErr(strTrace, ex, strRoutine)
            Return Nothing
        End Try

    End Function

    Private Async Sub ExchangeTokensAsync()

        ' POST https://www.wunderlist.com/oauth/access_token, Response = 200

        '   Request Body
        '   {
        '       "client_id": "CLIENT_IDENTIFIER"
        '       "client_secret": "CLIENT_SECRET"
        '       "code": "CODE_FROM_AUTHORIZECONNECTION"
        '   }
        '
        '   Response Body
        '   {
        '       "access_token": "SOME_TOKEN"
        '   }

        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":ExchangeTokensAsync"
        Dim strDefault As String = "Method failed."
        Try

            strTrace = "Construct request body."
            Dim sb As New StringBuilder
            sb.Append("{")
            sb.Append("""client_id"":""" & _appKeys.ClientID & """,")
            sb.Append("""client_secret"":""" & _appKeys.ClientSecret & """,")
            sb.Append("""code"":""" & _authCode & """")
            sb.Append("}")

            Dim postData As String = sb.ToString

            strTrace = "Make the request."
            _rh.BaseURL = "https://www.wunderlist.com/"
            Dim strResponse As String = Await _rh.APIRequestAsync("POST", "oauth/access_token", postData)
            If String.IsNullOrEmpty(strResponse) Then
                strTrace = "Access token REST call failed."
                Throw New Exception(strDefault)
            End If

            strTrace = "Process the response."
            Dim token As String = RESTHelper.GetKeyValueFromJSON(strResponse, "access_token")

            If Not String.IsNullOrEmpty(token) Then
                myAccessToken = New oAuthAccessToken(token, Now)
                _rh.AccessToken = token
                _rh.ResetBaseURL()
                RaiseEvent Connected(Me, New EventArgs)
            Else
                If _rh.StatusCode = 200 Then
                    strTrace = "Failed to retrieve an access token from a successful request."
                Else
                    strTrace = "No access token returned - see HTTP error."
                End If
                Throw New Exception(strDefault)
            End If

        Catch ex As Exception
            LogErr(strTrace, ex, strRoutine)
        End Try

    End Sub

    Private Sub LogErr(ByVal Message As String, ByVal myEx As Exception, ByVal Method As String)
        Debug.Print(Method & "|ERROR|" & Message & " " & myEx.Message)
    End Sub

    Private Sub LogAction(ByVal Action As String, ByVal Message As String, ByVal Method As String)
        Debug.Print(Method & "|" & Action & "|" & Message)
    End Sub

#End Region

#Region "Supporting Classes"

    Public Class CollectionEventArgs
        Inherits EventArgs

        Public Property collection As Object = Nothing

        Public Sub New(ByVal newColl As Object)
            _collection = newColl
        End Sub

    End Class

    Public Class ItemEventArgs

        Public Enum enuAction
            None = 0
            Save = 1
            Delete = 2
            Update = 3
            Insert = 4
        End Enum

        Public Property Item As Object = Nothing
        Public Property Action As enuAction = enuAction.None

        Public Sub New()
            MyBase.New()
        End Sub

        Public Sub New(ByVal Item As Object, ByVal Action As enuAction)
            _Item = Item
            _Action = Action
        End Sub

    End Class

    Public Class oAuthAccessToken

#Region "Fields"

        Public Const _rootClass As String = "Client.oAuthAccessToken"

        ''' <summary>
        ''' Default expiration period from Wunderlist
        ''' </summary>
        ''' <remarks>Not declared on their site, assuming 48 hrs</remarks>
        Private Const expirationPeriod As Long = 48 * 60 * 60 ' in seconds

#End Region

#Region "Properties"

        ''' <summary>
        ''' The access token issued by the service
        ''' </summary>
        ''' <value>String:</value>
        ''' <returns>String:</returns>
        ''' <remarks></remarks>
        Public Property Token As String

        ''' <summary>
        ''' The date and time the token was issued.
        ''' </summary>
        ''' <value>Date:</value>
        ''' <returns>Date:</returns>
        ''' <remarks></remarks>
        Public Property Issued As Date = ObjectBase.DefaultDate

        ''' <summary>
        ''' The date and time the token expires
        ''' </summary>
        ''' <value>Date:</value>
        ''' <returns>Date:</returns>
        ''' <remarks>If the token is expired, should as the user to 
        ''' reenter their credentials.</remarks>
        Public Property Expires As Date = ObjectBase.DefaultDate

#End Region

#Region "Events"

#End Region

#Region "Event Handlers"

#End Region

#Region "Constructor"

        Public Sub New()

        End Sub

        Public Sub New(ByVal token As String, ByVal Issued As Date)

            Try
                _Token = token
                _Issued = Issued
                _Expires = DateAdd(DateInterval.Second, expirationPeriod, _Issued)
            Catch ex As Exception
                Debug.Print("oAuthAccessToken.New|" & "Error: " & ex.Message & ", ")
            End Try

        End Sub

#End Region

#Region "Methods"

        ''' <summary>
        ''' Serializes the object instance to a Json formatted string
        ''' </summary>
        ''' <returns>String:</returns>
        ''' <remarks></remarks>
        Public Function GetStorageString() As String
            Dim strJson As String = JsonConvert.SerializeObject(Me)
            Return strJson
        End Function

        ''' <summary>
        ''' Fills the object instance with the specified
        ''' Json formatted string.
        ''' </summary>
        ''' <param name="strJson">String:</param>
        ''' <remarks></remarks>
        Public Sub RestoreFromStorageString(ByVal strJson As String)

            Dim myToken As New oAuthAccessToken
            myToken = JsonConvert.DeserializeObject(strJson, Me.GetType)

            Me.Token = myToken.Token
            Me.Issued = myToken.Issued
            Me.Expires = myToken.Expires

        End Sub

#End Region

#Region "Supporting Methods"

#End Region

#Region "Supporting Classes"

#End Region

    End Class

    Public Class RESTHelper

#Region "Fields"

        Private Const _rootClass As String = "WL.Client.RESTHelper"

        Dim _clientID As String = String.Empty

        Private Const defaultBaseURL As String = "http://a.wunderlist.com/api/v1/"
#End Region

#Region "Properties"

        ''' <summary>
        ''' Base URL to be used the RESTful call
        ''' </summary>
        ''' <value>String:</value>
        ''' <returns>String:</returns>
        ''' <remarks>Defaults to http://a.wunderlist.com/ </remarks>
        Public Property BaseURL As String
            Get
                Return _baseURL
            End Get
            Set(value As String)
                _baseURL = value
            End Set
        End Property
        Dim _baseURL As String = defaultBaseURL

        ''' <summary>
        ''' Access token to be used in all calls subsequent to the authorization calls.
        ''' </summary>
        ''' <value>String:</value>
        ''' <returns>String:</returns>
        ''' <remarks></remarks>
        Public Property AccessToken As String
            Get
                Return _accessToken
            End Get
            Set(value As String)
                _accessToken = value
            End Set
        End Property
        Dim _accessToken As String = String.Empty

        ''' <summary>
        ''' Last HTTP Status code returned.
        ''' </summary>
        ''' <value></value>
        ''' <returns>System.Net.HttpStatusCode:</returns>
        ''' <remarks></remarks>
        Public ReadOnly Property StatusCode As System.Net.HttpStatusCode
            Get
                Return _statusCode
            End Get
        End Property
        Dim _statusCode As System.Net.HttpStatusCode = Net.HttpStatusCode.Unused

        ''' <summary>
        ''' Returns the last web exception message.
        ''' </summary>
        ''' <value></value>
        ''' <returns>String:</returns>
        ''' <remarks></remarks>
        Public ReadOnly Property ErrorMessage As String
            Get
                Return _errorMessage
            End Get
        End Property
        Dim _errorMessage As String = String.Empty

#End Region

#Region "Events"

#End Region

#Region "Event Handlers"

#End Region

#Region "Constructor"

        Public Sub New(ByVal Client_ID As String)
            _clientID = Client_ID
        End Sub

#End Region

#Region "Methods"

        Public Sub ResetBaseURL()
            _baseURL = defaultBaseURL
        End Sub

#Region "Asynchronous Methods"

        Public Async Function APIRequestAsync(ByVal method As String, _
                                    ByVal resource As String, _
                                    ByVal Params As Dictionary(Of String, Object)) As Task(Of String)

            Dim sb As New StringBuilder
            For Each kvp As KeyValuePair(Of String, Object) In Params
                sb.Append(kvp.Key & "=" & kvp.Value.ToString & "&")
            Next

            Dim query As String = sb.ToString.Substring(0, sb.ToString.Length - 1)

            Dim strReturn As String = Await APIWebRequestAsync(method, resource & "?" & query, "")
            Return strReturn

        End Function

        Public Async Function APIRequestAsync(ByVal method As String, _
                                                ByVal resource As String) As Task(Of String)

            Dim strReturn As String = Await APIWebRequestAsync(method, resource, "")
            Return strReturn
        End Function

        Public Async Function APIRequestAsync(ByVal method As String, _
                                                ByVal resource As String, _
                                                ByVal postData As String) As Task(Of String)

            Dim strReturn As String = Await APIWebRequestAsync(method, resource, postData)
            Return strReturn
        End Function

#End Region

#Region "Synchronous Methods"

        Public Function APIRequest(ByVal method As String, _
                                    ByVal resource As String, _
                                    ByVal Params As Dictionary(Of String, Object)) As String


            Dim sb As New StringBuilder
            For Each kvp As KeyValuePair(Of String, Object) In Params
                sb.Append(kvp.Key & "=" & kvp.Value.ToString & "&")
            Next

            Dim query As String = sb.ToString.Substring(0, sb.ToString.Length - 1)

            Dim strReturn As String = APIWebRequest(method, resource & "?" & query, "")

            Return strReturn

        End Function

        Public Function APIRequest(ByVal method As String, _
                          ByVal resource As String) As String
            Dim strReturn As String = APIWebRequest(method, resource, "")
            Return strReturn
        End Function

        Public Function APIRequest(ByVal method As String, _
                                       ByVal resource As String, _
                                       ByVal postData As String) As String

            Dim strReturn As String = APIWebRequest(method, resource, postData)
            Return strReturn
        End Function

#End Region

        ''' <summary>
        ''' Get a collection of objects (JSON Array) from a JSON String.
        ''' </summary>
        ''' <param name="jsonInput">JSON String</param>
        ''' <param name="ArrayName">Array Name</param>
        ''' <returns>ArrayList</returns>
        ''' <remarks></remarks>
        Public Shared Function GetArrayListJSON(ByVal jsonInput As String, ByVal ArrayName As String) As ArrayList
            Dim strTrace As String = "General Fault."
            Dim strRoutine As String = "clsData:Database:GetArrayListJSON"
            Try
                Using reader As New Newtonsoft.Json.JsonTextReader(New IO.StringReader(jsonInput))
                    Dim ser As New Newtonsoft.Json.JsonSerializer
                    Dim o As Newtonsoft.Json.Linq.JObject = DirectCast(ser.Deserialize(reader), Newtonsoft.Json.Linq.JObject)
                    Dim t = o(ArrayName)

                    If reader.Read() AndAlso reader.TokenType <> Newtonsoft.Json.JsonToken.Comment Then
                        Throw New Exception("Additional text found in the JSON string after deserialization.")
                    End If

                    Dim retArray As New ArrayList

                    If TypeOf t Is Newtonsoft.Json.Linq.JValue Then
                        strTrace = "Found a single value associated with the Key."
                        Dim v As Newtonsoft.Json.Linq.JValue = TryCast(t, Newtonsoft.Json.Linq.JValue)
                        retArray.Add(v.Value.ToString)
                        Return retArray
                    ElseIf TypeOf t Is Newtonsoft.Json.Linq.JObject Then
                        strTrace = "Found a single object for the specified key."
                        retArray.Add(t.ToString) ' get its string

                        Return retArray

                        ' strTrace = "Found a table of values associated with the Key."
                        'If t.Count >= 1 Then
                        '    For i = 0 To t.Count - 1
                        '        retArray.Add(t.Item(i))
                        '    Next
                        '    Return retArray
                        'Else
                        '    Return Nothing
                        'End If
                    ElseIf TypeOf t Is Newtonsoft.Json.Linq.JArray Then
                        Dim a As Newtonsoft.Json.Linq.JArray = t
                        strTrace = "Found a table of values associated with the Key."
                        If a.Count >= 1 Then
                            For i = 0 To a.Count - 1
                                retArray.Add(a.Item(i).ToString)
                            Next
                            Return retArray
                        Else
                            Return Nothing
                        End If
                    Else
                        Return Nothing
                    End If

                End Using
            Catch ex As Exception
                'TraceLogger(strTrace & " " & ex.StackTrace, strRoutine)
                'ErrorLogger(ex.Message & " " & strTrace, Err.Number, strRoutine)
                Return Nothing
            End Try
        End Function

        ''' <summary>
        ''' Get Value from a specified Key from a JSON string.
        ''' </summary>
        ''' <param name="jsonInput">JSON String</param>
        ''' <param name="Key">Key of interest</param>
        ''' <returns>Object</returns>
        ''' <remarks></remarks>
        Public Shared Function GetKeyValueFromJSON(ByVal jsonInput As String, ByVal Key As String) As Object
            Dim strTrace As String = "General Fault."
            Dim strRoutine As String = "clsData:Database:GetKeyValueJSON"
            Try
                Using reader As New Newtonsoft.Json.JsonTextReader(New IO.StringReader(jsonInput))
                    Dim ser As New Newtonsoft.Json.JsonSerializer
                    Dim o As Newtonsoft.Json.Linq.JObject = DirectCast(ser.Deserialize(reader), Newtonsoft.Json.Linq.JObject)
                    Dim t = o(Key)

                    If reader.Read() AndAlso reader.TokenType <> Newtonsoft.Json.JsonToken.Comment Then
                        Throw New Exception("Additional text found in the JSON string after deserialization.")
                    End If

                    If TypeOf t Is Newtonsoft.Json.Linq.JValue Then
                        strTrace = "Found a single value associated with the Key."
                        Return t
                    ElseIf TypeOf t Is Newtonsoft.Json.Linq.JObject Then
                        strTrace = "Found a single object for the specified key."
                        Return t.ToString ' get its string

                        strTrace = "Found a table of values associated with the Key."
                        If t.Count >= 1 Then
                            strTrace = "Return the value in the first row of the table."
                            Return t.Item(0).Value(Of String)(Key)
                        Else
                            Return Nothing
                        End If
                    ElseIf TypeOf t Is Newtonsoft.Json.Linq.JArray Then
                        strTrace = "Found a table of values associated with the Key."
                        Dim a As Newtonsoft.Json.Linq.JArray = DirectCast(t, Newtonsoft.Json.Linq.JArray)
                        If a.Count >= 1 Then
                            strTrace = "Return the value in the first row of the table."
                            Return a.Item(0).Value(Of String)(Key)
                        Else
                            Return Nothing
                        End If
                    Else
                        Return Nothing
                    End If

                End Using
            Catch ex As Exception
                'TraceLogger(strTrace & " " & ex.StackTrace, strRoutine)
                'ErrorLogger(ex.Message & " " & strTrace, Err.Number, strRoutine)
                Return Nothing
            End Try
        End Function

#End Region

#Region "Supporting Methods"

        ' Synchronous Calls

        Private Function APIWebRequest(ByVal method As String, ByVal resource As String, _
                                         ByVal postData As String) As String


            Dim webRequest As Net.HttpWebRequest = Nothing

            Dim strTrace As String = "General Fault."
            Dim strRoutine As String = _rootClass & ":APIWebRequest"
            Try

                Dim url As String = _baseURL & resource

                webRequest = TryCast(System.Net.WebRequest.Create(url), Net.HttpWebRequest)

                If method.ToUpper = "POST" Or method.ToUpper = "PUT" Or method.ToUpper = "PATCH" Then
                    webRequest.ContentType = "application/json"
                End If

                strTrace = "The default timeout is: " & webRequest.Timeout.ToString
                webRequest.Timeout = 10000

                '' Add Proxy if necessary - if a proxy is used, by default .Net knows the proxy settings and will use
                ''   the default credentials.  This code is only useful if Windows credentials do not match what is
                ''   expected by the proxy
                'Dim myProxy As System.Net.WebProxy = ...GetMyProxy()
                'If Not IsNothing(myProxy) Then
                '    webRequest.Proxy = myProxy
                'End If

                ' Add the Header
                If resource.ToLower.Contains("oauth") Then
                    ' Ignore the headers - authorizing
                Else
                    ' X-Access-Token: OAUTH-TOKEN X-Client-ID: CLIENT-ID
                    webRequest.Headers.Add("X-Access-Token", _accessToken)
                    webRequest.Headers.Add("X-Client-ID", _clientID)
                End If

                webRequest.Method = method
                ' webRequest.Credentials = CredentialCache.DefaultCredentials
                '   webRequest.AllowWriteStreamBuffering = True

                '  webRequest.PreAuthenticate = False 'True
                ' webRequest.ServicePoint.Expect100Continue = False
                ' ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3

                strTrace = "Use this callback to 'skip' certificate errors."
                Net.ServicePointManager.ServerCertificateValidationCallback = _
                    Function(obj As [Object], certificate As X509Certificate, chain As X509Chain, errors As System.Net.Security.SslPolicyErrors) (True)

                If postData IsNot Nothing AndAlso postData.Length > 0 Then

                    Dim fileToSend As Byte() = System.Text.Encoding.UTF8.GetBytes(postData)
                    webRequest.ContentLength = fileToSend.Length

                    Using reqStream As Stream = webRequest.GetRequestStream
                        reqStream.Write(fileToSend, 0, fileToSend.Length)
                        reqStream.Close()
                    End Using

                End If

                Dim returned As String = WebResponseGet(webRequest)

                Return returned

            Catch webex As Net.WebException
                strTrace = webex.Message
                '_errorCode = webex.Status
                '_errorMessage = webex.Message
                '_exception = webex
                Dim rsp As System.Net.WebResponse = webex.Response
                '  ErrorLogger(strTrace, webex.Status, strRoutine)
                Return String.Empty
            Catch ex As Exception
                ' _exception = ex
                'TraceLogger(strTrace & " " & ex.StackTrace, strRoutine)
                'ErrorLogger(ex.Message & " " & strTrace, Err.Number, strRoutine)
                Return String.Empty
            End Try


        End Function

        Private Function WebResponseGet(ByVal webRequest As Net.HttpWebRequest) As String


            Dim strTrace As String = "General Fault."
            Dim strRoutine As String = _rootClass & ":WebResponseGet"
            Dim responseReader As StreamReader = Nothing
            Try
                ' Preset the return code
                _statusCode = Net.HttpStatusCode.Unused
                _errorMessage = String.Empty

                strTrace = "Use this callback to 'skip' certificate errors."
                Net.ServicePointManager.ServerCertificateValidationCallback = _
                    Function(obj As [Object], certificate As X509Certificate, chain As X509Chain, errors As System.Net.Security.SslPolicyErrors) (True)

                Dim responseData As String = ""
                '  _errorCode = Net.WebExceptionStatus.Success

                Dim myResponse As System.Net.HttpWebResponse = webRequest.GetResponse
                _statusCode = myResponse.StatusCode

                responseReader = New StreamReader(myResponse.GetResponseStream())
                responseData = responseReader.ReadToEnd()
                responseReader.Close()

                ' Dim i As System.Net.WebExceptionStatus
                '   _errorCode = i

                Return responseData

            Catch webex As Net.WebException
                '_errorCode = webex.Status
                _errorMessage = webex.Message
                '_exception = webex
                _statusCode = webex.Status

                Dim rsp As System.Net.WebResponse = webex.Response
                '   TraceLogger(rsp, strRoutine)
                ' If Not IsNothing(rsp) Then _statusCode = 0

                '     ErrorLogger(webex.Message, webex.Status, strRoutine)
                Return webex.Message
            Catch ex As Exception
                '_exception = ex
                'TraceLogger(strTrace & " " & ex.StackTrace, strRoutine)
                'ErrorLogger(ex.Message & " " & strTrace, Err.Number, strRoutine)
                Return ex.Message
            Finally
                webRequest.GetResponse().GetResponseStream().Close()
                responseReader.Close()
                responseReader = Nothing
            End Try

        End Function

        ' Asynchronous Calls

        Private Async Function APIWebRequestAsync(ByVal method As String, ByVal resource As String, _
                                                ByVal postData As String) As Task(Of String)

            Dim webRequest As Net.HttpWebRequest = Nothing

            Dim strTrace As String = "General Fault."
            Dim strRoutine As String = _rootClass & ":APIWebRequestAsync"
            Try

                Dim url As String = _baseURL & resource

                webRequest = TryCast(System.Net.WebRequest.Create(url), Net.HttpWebRequest)

                If method.ToUpper = "POST" Or method.ToUpper = "PUT" Or method.ToUpper = "PATCH" Then
                    webRequest.ContentType = "application/json"
                End If

                strTrace = "The default timeout is: " & webRequest.Timeout.ToString
                '   TraceLogger(strTrace, strRoutine)
                webRequest.Timeout = 10000 ' ThisAddIn.AddinConfig.FocusSyncTimeout

                '' Add Proxy if necessary - if a proxy is used, by default .Net knows the proxy settings and will use
                ''   the default credentials.  This code is only useful if Windows credentials do not match what is
                ''   expected by the proxy
                'Dim myProxy As System.Net.WebProxy = FocusMe_for_Outlook_v4.Ceptara.OutlookTools.GetMyProxy()
                'If Not IsNothing(myProxy) Then
                '    webRequest.Proxy = myProxy
                'End If

                ' Add the Header
                If resource.ToLower.Contains("oauth") Then
                    ' Ignore the headers - authorizing
                Else
                    ' X-Access-Token: OAUTH-TOKEN X-Client-ID: CLIENT-ID
                    webRequest.Headers.Add("X-Access-Token", _accessToken)
                    webRequest.Headers.Add("X-Client-ID", _clientID)
                End If


                webRequest.Method = method
                ' webRequest.Credentials = CredentialCache.DefaultCredentials
                '   webRequest.AllowWriteStreamBuffering = True

                '  webRequest.PreAuthenticate = False 'True
                ' webRequest.ServicePoint.Expect100Continue = False
                ' ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3

                strTrace = "Use this callback to 'skip' certificate errors."
                Net.ServicePointManager.ServerCertificateValidationCallback = _
                    Function(obj As [Object], certificate As X509Certificate, chain As X509Chain, errors As System.Net.Security.SslPolicyErrors) (True)

                If postData IsNot Nothing AndAlso postData.Length > 0 Then

                    Dim fileToSend As Byte() = System.Text.Encoding.UTF8.GetBytes(postData)
                    webRequest.ContentLength = fileToSend.Length

                    Using reqStream As Stream = webRequest.GetRequestStream
                        reqStream.Write(fileToSend, 0, fileToSend.Length)
                        reqStream.Close()
                    End Using

                End If

                Dim returned As String = Await WebResponseGetAsync(webRequest)

                Return returned

            Catch webex As Net.WebException
                strTrace = webex.Message
                '_errorCode = webex.Status
                '_errorMessage = webex.Message
                '_exception = webex
                Dim rsp As System.Net.WebResponse = webex.Response
                '  ErrorLogger(strTrace, webex.Status, strRoutine)
                Return String.Empty
            Catch ex As Exception
                ' _exception = ex
                'TraceLogger(strTrace & " " & ex.StackTrace, strRoutine)
                'ErrorLogger(ex.Message & " " & strTrace, Err.Number, strRoutine)
                Return String.Empty
            End Try

        End Function

        ''' <summary>
        ''' Process the web response.
        ''' </summary>
        ''' <param name="webRequest">HTTPWebRequest</param>
        ''' <returns>The response data.</returns>
        Private Async Function WebResponseGetAsync(ByVal webRequest As Net.HttpWebRequest) As Task(Of String)

            Dim strTrace As String = "General Fault."
            Dim strRoutine As String = _rootClass & ":WebResponseGetAsync"
            Dim responseReader As StreamReader = Nothing
            Try
                ' Preset the return code
                _statusCode = Net.HttpStatusCode.Unused
                _errorMessage = String.Empty

                strTrace = "Use this callback to 'skip' certificate errors."
                Net.ServicePointManager.ServerCertificateValidationCallback = _
                    Function(obj As [Object], certificate As X509Certificate, chain As X509Chain, errors As System.Net.Security.SslPolicyErrors) (True)

                Dim responseData As String = ""

                Dim myResponse As System.Net.HttpWebResponse = Await webRequest.GetResponseAsync
                _statusCode = myResponse.StatusCode

                responseReader = New StreamReader(myResponse.GetResponseStream())
                responseData = responseReader.ReadToEnd()
                responseReader.Close()

                Return responseData

            Catch webex As Net.WebException
                '_errorCode = webex.Status
                _errorMessage = webex.Message
                '_exception = webex
                _statusCode = webex.Status

                Dim rsp As System.Net.WebResponse = webex.Response

                '   TraceLogger(rsp, strRoutine)

                '     ErrorLogger(webex.Message, webex.Status, strRoutine)
                Return webex.Message
            Catch ex As Exception
                '_exception = ex
                'TraceLogger(strTrace & " " & ex.StackTrace, strRoutine)
                'ErrorLogger(ex.Message & " " & strTrace, Err.Number, strRoutine)
                Return ex.Message
            Finally
                webRequest.GetResponse().GetResponseStream().Close()
                responseReader.Close()
                responseReader = Nothing
            End Try
        End Function

#End Region

#Region "Supporting Classes"

#End Region

    End Class

#End Region

End Class

Public Enum enuObjectClass
    None = 0
    List = 1
    Task = 2
    User = 3
End Enum

Public Class ObjectBase

#Region "Fields"

    Public Const _rootClass As String = "Wunderlist.ObjectBase"

    Public Const DefaultDate As Date = #1/1/1970#

#End Region

#Region "Properties"

    '   Public Overloads Property id As String = String.Empty

    ''' <summary>
    ''' Allows the use of the ID property in the Data.ObjectBase class
    ''' versus reiterating here.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks>Ceptara.Data.ObjectBase exists in the Ceptara.Core library</remarks>
    <JsonProperty(propertyname:="id")> _
    Public Shadows Property ID As String = String.Empty

    <JsonProperty(PropertyName:="created_at")> _
    Public Shadows Property CreatedDate As Date

    <JsonProperty(PropertyName:="updated_at")> _
    Public Shadows Property ModifiedDate As Date

    Public Property revision As Integer = 0
    Public Property type As String = String.Empty

    '   Public Property created_at As Date = DefaultDate
    '   Public Property updated_at As Date = DefaultDate

#End Region

#Region "Events"

#End Region

#Region "Event Handlers"

#End Region

#Region "Constructor"

#End Region

#Region "Methods"

    ''' <summary>
    ''' Returns a Json string representing this object.
    ''' </summary>
    ''' <returns>String:</returns>
    ''' <remarks></remarks>
    Public Function ToStringJson() As String
        Return ToStringJson(Me)
    End Function

    ''' <summary>
    ''' Creates a formatted JSON string containing the object's properties and values.
    ''' </summary>
    ''' <param name="obj">Object: e.g. Project, Task, etc.</param>
    ''' <returns>String</returns>
    ''' <remarks></remarks>
    Public Shared Function ToStringJSON(ByVal obj As Object) As String
        Dim strTrace As String = "General Fault."
        Dim strRoutine As String = _rootClass & ":ToStringJSON"
        Dim strDefault As String = "Unable to produce an JSON record string."
        Try
            If IsNothing(obj) Then
                strTrace = "A null object as passed."
                Throw New Exception(strDefault)
            End If

            Dim strReturn As String = Newtonsoft.Json.JsonConvert.SerializeObject(obj)

            Return strReturn
        Catch ex As Exception
            Debug.Print(strRoutine & "|ERROR|" & strTrace & " " & ex.Message)
            Return String.Empty
        End Try
    End Function

#End Region

#Region "Supporting Methods"

#End Region

#Region "Supporting Classes"

#End Region

End Class

Public Class ObjectDescriptor

    Public Property ItemClass As enuObjectClass = enuObjectClass.None
    Public Property id As String = String.Empty

    Public Sub New()

    End Sub

    Public Sub New(ByVal itemClass As enuObjectClass, ByVal objectId As String)
        _ItemClass = itemClass
        _id = objectId
    End Sub

End Class

Public Class List
    Inherits ObjectBase

    Public Property title As String = String.Empty
    Public Property list_type As String = String.Empty

    ''' <summary>
    ''' public is a reserved word in vb.net - so redirect
    ''' the Json public property to the local sensitivity property
    ''' during serialization and deserialization.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    <JsonProperty(PropertyName:="public")> _
    Public Property sensitivity As Boolean = True

    Public Property owner_type As String = String.Empty
    Public Property owner_id As String = String.Empty

End Class

Public Class Task
    Inherits ObjectBase

    Public Property title As String = String.Empty
    Public Property assignee_id As Integer = 0
    Public Property created_by_id As Integer = 0
    Public Property due_date As Date = ObjectBase.DefaultDate
    Public Property list_id As Integer = 0
    Public Property starred As Boolean = False
    Public Property completed As Boolean = False

End Class

Public Class User
    Inherits ObjectBase

    ' {
    '  "id": 6234958,
    '  "name": "BENCHMARK",
    '  "email": "benchmark@example.com",
    '  "created_at": "2013-08-30T08:25:58.000Z",
    '  "revision": 1
    ' }

    Public Property email As String = String.Empty

End Class



