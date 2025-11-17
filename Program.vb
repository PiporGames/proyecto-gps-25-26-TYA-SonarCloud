Imports System
Imports System.Net
Imports System.IO
Imports System.Text
Imports System.Threading.Tasks
Imports System.Text.Json
Imports Npgsql

Module Program

    '==========================================================================
    ' Microservicio TEMAS y AUTORES proyecto OverSounds - GPS 2025-2026
    ' Creado por: José Manuel de Torres Dominguez
    '==========================================================================
    ' PARÁMETROS DE CONFIGURACIÓN
    Dim host_ip As String = "+"
    Dim host_port As Integer = 8081
    Dim connectionString = "Host=pgnweb.ddns.net;Username=tya_admin;Password=12345;Database=tya"
    Dim db As NpgsqlDataSource = Nothing
    Dim ip_auth As String = "10.1.1.4:8080" ' IP del servicio de autenticación
    '==========================================================================

    Sub Main(args As String())
        ' Conectarse a la base de datos PostgreSQL
        Try
            db = NpgsqlDataSource.Create(connectionString)
            Console.WriteLine("Conexión a la base de datos PostgreSQL establecida correctamente.")
        Catch ex As Exception
            Console.WriteLine("Error al conectar a la base de datos PostgreSQL: " & ex.Message)
            Return
        End Try

        ' Iniciar el servidor de manera asíncrona
        StartServerAsync(host_ip, host_port).GetAwaiter().GetResult()
    End Sub


    Async Function StartServerAsync(host_ip As String, host_port As Integer) As Task
        ' Crear el servidor HTTP
        Dim listener As New HttpListener()
        ' Configurar el prefijo (URL base) donde escuchar
        listener.Prefixes.Add("http://" + host_ip + ":" + host_port.ToString() + "/")

        Try
            ' Iniciar el servidor
            listener.Start()
            Console.WriteLine("Servidor HTTP iniciado en http://" + host_ip + ":" + host_port.ToString())
            Console.WriteLine("Presiona Ctrl+C para detener el servidor")
            Console.WriteLine()

            ' Bucle principal para manejar peticiones
            While True
                ' Esperar por una petición de manera asíncrona
                Dim context As HttpListenerContext = Await listener.GetContextAsync()

                ' Manejar cada petición en una tarea separada (sin esperar a que termine)
                Dim fireAndForget = Task.Run(Async Function()
                                                 Await HandleRequestAsync(context.Request, context.Response)
                                             End Function)
            End While

        Catch ex As Exception
            Console.WriteLine($"Error: {ex.Message}")
        Finally
            ' Detener el servidor
            Console.WriteLine("Servidor detenido")
        End Try
    End Function


    Async Function HandleRequestAsync(request As HttpListenerRequest, response As HttpListenerResponse) As Task
        Dim jsonResponse As String = GenerateErrorResponse("501", "No implementado")
        Dim contentType = "application/json"
        Dim statusCode As Integer = HttpStatusCode.OK
        Dim URLpath As String() = request.Url.AbsolutePath.Split("/"c) ' Ejemplo: /song/123

        Try
            ' Mostrar información de la petición
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Petición recibida: {request.HttpMethod} {request.Url.AbsolutePath}")

            ' Obtener segmentos de la URL de manera segura
            Dim resource As String = If(URLpath.Length > 1, URLpath(1), "")
            Dim action As String = If(URLpath.Length > 2, URLpath(2), "")

            ' Verificar si el endpoint requiere autenticación
            ' Todos los endpoints GET no requieren autenticación
            ' Solo POST, PATCH, DELETE requieren autenticación
            Dim userId As Integer? = Nothing
            If request.HttpMethod <> "GET" Then
                ' Validar el token de autenticación
                userId = Await ValidateAuthTokenAsync(request)

                If Not userId.HasValue Then
                    ' No autenticado o token inválido
                    jsonResponse = GenerateErrorResponse("401", "No autenticado. Se requiere iniciar sesión")
                    statusCode = HttpStatusCode.Unauthorized

                    ' Configurar y enviar la respuesta
                    response.StatusCode = statusCode
                    response.ContentType = contentType
                    Dim buffer2 As Byte() = Encoding.UTF8.GetBytes(jsonResponse)
                    response.ContentLength64 = buffer2.Length
                    Dim output2 As Stream = response.OutputStream
                    Await output2.WriteAsync(buffer2, 0, buffer2.Length)
                    output2.Close()

                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Acceso denegado - No autenticado")
                    Console.WriteLine()
                    Return
                End If

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Usuario autenticado: {userId.Value}")
            End If

            ' Detectar la ruta personalizada
            If resource = "song" Then

                If action = "upload" AndAlso request.HttpMethod = "POST" Then
                    uploadSong(request, action, jsonResponse, statusCode, userId.Value)

                ElseIf action = "search" AndAlso request.HttpMethod = "GET" Then
                    searchSong(request, action, jsonResponse, statusCode)

                ElseIf action = "list" AndAlso request.HttpMethod = "GET" Then
                    listSongs(request, action, jsonResponse, statusCode)

                ElseIf action = "filter" AndAlso request.HttpMethod = "GET" Then
                    filterSongs(request, action, jsonResponse, statusCode)

                ElseIf IsNumeric(action) AndAlso request.HttpMethod = "GET" Then
                    getSong(request, action, jsonResponse, statusCode)

                ElseIf IsNumeric(action) AndAlso request.HttpMethod = "DELETE" Then
                    deleteSong(request, action, jsonResponse, statusCode, userId.Value)

                ElseIf IsNumeric(action) AndAlso request.HttpMethod = "PATCH" Then
                    updateSong(request, action, jsonResponse, statusCode, userId.Value)

                Else
                    ' Ruta no encontrada
                    jsonResponse = GenerateErrorResponse("404", "Recurso no encontrado")
                    statusCode = HttpStatusCode.NotFound
                End If


            ElseIf resource = "album" Then

                If action = "upload" AndAlso request.HttpMethod = "POST" Then
                    uploadAlbum(request, action, jsonResponse, statusCode, userId.Value)

                ElseIf action = "search" AndAlso request.HttpMethod = "GET" Then
                    searchAlbum(request, action, jsonResponse, statusCode)

                ElseIf action = "list" AndAlso request.HttpMethod = "GET" Then
                    listAlbums(request, action, jsonResponse, statusCode)

                ElseIf action = "filter" AndAlso request.HttpMethod = "GET" Then
                    filterAlbums(request, action, jsonResponse, statusCode)

                ElseIf IsNumeric(action) AndAlso request.HttpMethod = "GET" Then
                    getAlbum(request, action, jsonResponse, statusCode)

                ElseIf IsNumeric(action) AndAlso request.HttpMethod = "DELETE" Then
                    deleteAlbum(request, action, jsonResponse, statusCode, userId.Value)

                ElseIf IsNumeric(action) AndAlso request.HttpMethod = "PATCH" Then
                    updateAlbum(request, action, jsonResponse, statusCode, userId.Value)

                Else
                    ' Ruta no encontrada
                    jsonResponse = GenerateErrorResponse("404", "Recurso no encontrado")
                    statusCode = HttpStatusCode.NotFound
                End If


            ElseIf resource = "merch" Then

                If action = "upload" AndAlso request.HttpMethod = "POST" Then
                    uploadMerch(request, action, jsonResponse, statusCode, userId.Value)

                ElseIf action = "search" AndAlso request.HttpMethod = "GET" Then
                    searchMerch(request, action, jsonResponse, statusCode)

                ElseIf action = "list" AndAlso request.HttpMethod = "GET" Then
                    listMerch(request, action, jsonResponse, statusCode)

                ElseIf action = "filter" AndAlso request.HttpMethod = "GET" Then
                    filterMerch(request, action, jsonResponse, statusCode)

                ElseIf IsNumeric(action) AndAlso request.HttpMethod = "GET" Then
                    getMerch(request, action, jsonResponse, statusCode)

                ElseIf IsNumeric(action) AndAlso request.HttpMethod = "DELETE" Then
                    deleteMerch(request, action, jsonResponse, statusCode, userId.Value)

                ElseIf IsNumeric(action) AndAlso request.HttpMethod = "PATCH" Then
                    updateMerch(request, action, jsonResponse, statusCode, userId.Value)

                Else
                    ' Ruta no encontrada
                    jsonResponse = GenerateErrorResponse("404", "Recurso no encontrado")
                    statusCode = HttpStatusCode.NotFound
                End If


            ElseIf resource = "artist" Then

                If action = "upload" AndAlso request.HttpMethod = "POST" Then
                    uploadArtist(request, action, jsonResponse, statusCode, userId.Value)

                ElseIf action = "search" AndAlso request.HttpMethod = "GET" Then
                    searchArtist(request, action, jsonResponse, statusCode)

                ElseIf action = "list" AndAlso request.HttpMethod = "GET" Then
                    listArtists(request, action, jsonResponse, statusCode)

                ElseIf action = "filter" AndAlso request.HttpMethod = "GET" Then
                    filterArtists(request, action, jsonResponse, statusCode)

                ElseIf IsNumeric(action) AndAlso request.HttpMethod = "GET" Then
                    getArtist(request, action, jsonResponse, statusCode)

                ElseIf IsNumeric(action) AndAlso request.HttpMethod = "DELETE" Then
                    deleteArtist(request, action, jsonResponse, statusCode, userId.Value)

                ElseIf IsNumeric(action) AndAlso request.HttpMethod = "PATCH" Then
                    updateArtist(request, action, jsonResponse, statusCode, userId.Value)

                Else
                    ' Ruta no encontrada
                    jsonResponse = GenerateErrorResponse("404", "Recurso no encontrado")
                    statusCode = HttpStatusCode.NotFound
                End If


            ElseIf resource = "genres" AndAlso request.HttpMethod = "GET" Then
                ' Endpoint /genres - no requiere autenticación
                getGenres(request, action, jsonResponse, statusCode)


            ElseIf request.Url.AbsolutePath = "/" Then
                ' Ruta raíz
                jsonResponse = ConvertToJson("Microservicio TEMAS y AUTORES proyecto OverSounds - GPS 2025-2026\nCreado por: José Manuel de Torres Dominguez")
                statusCode = HttpStatusCode.OK


            Else
                ' Ruta no encontrada
                jsonResponse = GenerateErrorResponse("404", "Recurso no encontrado")
                statusCode = HttpStatusCode.NotFound
            End If


            ' Configurar la respuesta
            response.StatusCode = statusCode
            response.ContentType = contentType
            Dim buffer As Byte() = Encoding.UTF8.GetBytes(jsonResponse)
            response.ContentLength64 = buffer.Length

            ' Enviar la respuesta de manera asíncrona
            Dim output As Stream = response.OutputStream
            Await output.WriteAsync(buffer, 0, buffer.Length)
            output.Close()

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Respuesta enviada: {statusCode} - {jsonResponse.Replace(Environment.NewLine, "")}")
            Console.WriteLine()

        Catch ex As Exception
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Error procesando petición: {ex.Message}")
            Console.WriteLine(ex.ToString)

            Try
                response.StatusCode = 500
                response.Close()
            Catch
                ' Ignorar errores al cerrar la respuesta
            End Try
        End Try
    End Function

    ' Función helper para convertir lista de Strings a JSON
    Function ConvertToJson(obj As Object) As String
        Dim options As New JsonSerializerOptions With {.WriteIndented = True}
        Return JsonSerializer.Serialize(obj, options)
    End Function

    ' Funcion helper para generar una respuesta error JSON
    Function GenerateErrorResponse(code As String, message As String) As String
        Dim errorObj As New Dictionary(Of String, String) From {{"code", code}, {"message", message}}
        Return ConvertToJson(errorObj)
    End Function

    ' Función para validar el token de autenticación
    Async Function ValidateAuthTokenAsync(request As HttpListenerRequest) As Task(Of Integer?)
        Try
            ' Buscar la cookie oversound_auth
            Dim authCookie As Cookie = Nothing
            If request.Cookies IsNot Nothing Then
                authCookie = request.Cookies("oversound_auth")
            End If

            If authCookie Is Nothing OrElse String.IsNullOrEmpty(authCookie.Value) Then
                Return Nothing ' No hay token
            End If

            Dim token As String = authCookie.Value
            Dim authUrl As String = $"http://{ip_auth}/auth"
            Dim timeout As TimeSpan = TimeSpan.FromSeconds(2)

            Using httpClient As New Net.Http.HttpClient()
                httpClient.Timeout = timeout

                ' Crear el request con la cookie en el header
                Dim requestMessage As New Net.Http.HttpRequestMessage(Net.Http.HttpMethod.Get, authUrl)
                requestMessage.Headers.Add("Cookie", $"oversound_auth={token}")

                Dim authResponse = Await httpClient.SendAsync(requestMessage)

                If authResponse.StatusCode = Net.HttpStatusCode.OK Then
                    ' Leer los datos del usuario
                    Dim responseBody As String = Await authResponse.Content.ReadAsStringAsync()
                    Dim userData = JsonSerializer.Deserialize(Of Dictionary(Of String, JsonElement))(responseBody)
                    ' Extraer solo el userId
                    If userData.ContainsKey("idUsuario") Then
                        Return userData("idUsuario").GetInt32()
                    End If

                    Return Nothing
                Else
                    Console.WriteLine($"Auth service returned status: {authResponse.StatusCode}")
                    Return Nothing ' Token inválido
                End If
            End Using

        Catch ex As Exception
            Console.WriteLine($"Error al validar token: {ex.Message}")
            Return Nothing
        End Try
    End Function

    '==========================================================================
    ' LÓGICA DE NEGOCIO
    '==========================================================================


    ' Función auxiliar para obtener el ID del artista asociado a un usuario
    Function GetArtistIdByUserId(userId As Integer) As Integer?
        Try
            Using cmd = db.CreateCommand("SELECT idartista FROM artistas WHERE userid = @userid")
                cmd.Parameters.AddWithValue("@userid", userId)
                Dim result = cmd.ExecuteScalar()
                If result IsNot Nothing AndAlso Not IsDBNull(result) Then
                    Return CInt(result)
                End If
            End Using
        Catch ex As Exception
            Console.WriteLine($"Error al buscar artista por userId: {ex.Message}")
        End Try
        Return Nothing
    End Function

    '==========================================================================
    ' MÉTODOS PARA SONG
    '==========================================================================
    Sub uploadSong(request As HttpListenerRequest, action As String, ByRef jsonResponse As String, ByRef statusCode As Integer, userId As Integer)
        Try
            ' Leer el body del request
            Dim body As String
            Using reader As New StreamReader(request.InputStream, request.ContentEncoding)
                body = reader.ReadToEnd()
            End Using

            ' Parsear el JSON
            Dim songData = JsonSerializer.Deserialize(Of Dictionary(Of String, JsonElement))(body)

            ' Validar campos requeridos
            If Not songData.ContainsKey("title") OrElse Not songData.ContainsKey("genres") OrElse
               Not songData.ContainsKey("cover") OrElse Not songData.ContainsKey("price") OrElse
               Not songData.ContainsKey("trackId") OrElse Not songData.ContainsKey("duration") Then
                jsonResponse = GenerateErrorResponse("400", "Faltan campos requeridos")
                statusCode = HttpStatusCode.BadRequest
                Return
            End If

            ' Obtener valores
            Dim title As String = songData("title").GetString()
            Dim description As String = If(songData.ContainsKey("description") AndAlso songData("description").ValueKind <> JsonValueKind.Null, songData("description").GetString(), Nothing)
            Dim cover As String = songData("cover").GetString()
            Dim price As Decimal = songData("price").GetDecimal()
            Dim albumId As Integer? = If(songData.ContainsKey("albumId") AndAlso songData("albumId").ValueKind <> JsonValueKind.Null, CType(songData("albumId").GetInt32(), Integer?), Nothing)
            Dim albumOrder As Integer? = If(songData.ContainsKey("albumOrder") AndAlso songData("albumOrder").ValueKind <> JsonValueKind.Null, CType(songData("albumOrder").GetInt32(), Integer?), Nothing)
            Dim releaseDate As String = If(songData.ContainsKey("releaseDate"), songData("releaseDate").GetString(), DateTime.Now.ToString("yyyy-MM-dd"))
            Dim trackId As Integer = songData("trackId").GetInt32()
            Dim duration As Integer = songData("duration").GetInt32()

            ' Validar que price sea positivo
            If price <= 0 Then
                jsonResponse = GenerateErrorResponse("400", "El precio debe ser un valor positivo")
                statusCode = HttpStatusCode.BadRequest
                Return
            End If

            ' Validar que si albumId está definido, albumOrder también lo esté
            If albumId.HasValue AndAlso Not albumOrder.HasValue Then
                jsonResponse = GenerateErrorResponse("400", "Si se especifica albumId, también se debe especificar albumOrder")
                statusCode = HttpStatusCode.BadRequest
                Return
            End If

            If Not albumId.HasValue AndAlso albumOrder.HasValue Then
                jsonResponse = GenerateErrorResponse("400", "Si se especifica albumOrder, también se debe especificar albumId")
                statusCode = HttpStatusCode.BadRequest
                Return
            End If

            ' Validar que el álbum exista si se especifica
            If albumId.HasValue Then
                Dim albumExists As Boolean = False
                Using cmd = db.CreateCommand("SELECT COUNT(*) FROM albumes WHERE idalbum = @idalbum")
                    cmd.Parameters.AddWithValue("@idalbum", albumId.Value)
                    Dim count As Integer = CInt(cmd.ExecuteScalar())
                    albumExists = count > 0
                End Using

                If Not albumExists Then
                    jsonResponse = GenerateErrorResponse("422", "El álbum especificado no existe")
                    statusCode = 422 ' Unprocessable Entity
                    Return
                End If
            End If

            ' Insertar canción con albumog (el álbum original)
            Dim newSongId As Integer
            Using cmd = db.CreateCommand("INSERT INTO canciones (titulo, descripcion, cover, track, duracion, fechalanzamiento, precio, albumog) VALUES (@titulo, @descripcion, @cover, @track, @duracion, @fecha, @precio, @albumog) RETURNING idcancion")
                cmd.Parameters.AddWithValue("@titulo", title)
                cmd.Parameters.AddWithValue("@descripcion", If(description, DBNull.Value))
                cmd.Parameters.AddWithValue("@cover", StringToBytes(cover))
                cmd.Parameters.AddWithValue("@track", trackId)
                cmd.Parameters.AddWithValue("@duracion", duration)
                cmd.Parameters.AddWithValue("@fecha", Date.Parse(releaseDate))
                cmd.Parameters.AddWithValue("@precio", price)
                cmd.Parameters.AddWithValue("@albumog", If(albumId.HasValue, CType(albumId.Value, Object), DBNull.Value))
                newSongId = CInt(cmd.ExecuteScalar())
            End Using

            ' Obtener el ID del artista asociado al usuario autenticado
            Dim artistId As Integer? = GetArtistIdByUserId(userId)

            If Not artistId.HasValue Then
                jsonResponse = GenerateErrorResponse("403", "El usuario no tiene un artista asociado")
                statusCode = HttpStatusCode.Forbidden
                Return
            End If

            ' Insertar al artista principal (el usuario autenticado) - NO es colaborador (ft = false)
            Using cmd = db.CreateCommand("INSERT INTO autorescanciones (idartista, idcancion, ft) VALUES (@idartista, @idcancion, @ft)")
                cmd.Parameters.AddWithValue("@idartista", artistId.Value)
                cmd.Parameters.AddWithValue("@idcancion", newSongId)
                cmd.Parameters.AddWithValue("@ft", False) ' No es colaborador, es el artista principal
                cmd.ExecuteNonQuery()
            End Using

            ' Validar y insertar géneros
            If songData.ContainsKey("genres") Then
                For Each genreElement In songData("genres").EnumerateArray()
                    Dim genreId As Integer = genreElement.GetInt32()
                    
                    ' Validar que el género exista
                    Dim genreExists As Boolean = False
                    Using cmd = db.CreateCommand("SELECT COUNT(*) FROM generos WHERE idgenero = @idgenero")
                        cmd.Parameters.AddWithValue("@idgenero", genreId)
                        Dim count As Integer = CInt(cmd.ExecuteScalar())
                        genreExists = count > 0
                    End Using

                    If Not genreExists Then
                        jsonResponse = GenerateErrorResponse("422", $"El género con ID {genreId} no existe")
                        statusCode = 422 ' Unprocessable Entity
                        Return
                    End If
                    
                    Using cmd = db.CreateCommand("INSERT INTO generoscanciones (idcancion, idgenero) VALUES (@idcancion, @idgenero)")
                        cmd.Parameters.AddWithValue("@idcancion", newSongId)
                        cmd.Parameters.AddWithValue("@idgenero", genreId)
                        cmd.ExecuteNonQuery()
                    End Using
                Next
            End If

            ' Insertar colaboradores (artistas con ft = true)
            If songData.ContainsKey("collaborators") Then
                For Each collabElement In songData("collaborators").EnumerateArray()
                    Dim collabArtistId As Integer = collabElement.GetInt32()
                    Using cmd = db.CreateCommand("INSERT INTO autorescanciones (idartista, idcancion, ft) VALUES (@idartista, @idcancion, @ft)")
                        cmd.Parameters.AddWithValue("@idartista", collabArtistId)
                        cmd.Parameters.AddWithValue("@idcancion", newSongId)
                        cmd.Parameters.AddWithValue("@ft", True) ' Es colaborador
                        cmd.ExecuteNonQuery()
                    End Using
                Next
            End If

            ' Si tiene álbum original (albumog), SIEMPRE insertar en CancionesAlbumes
            ' Esto asegura que el álbum original siempre aparezca en la tabla de relaciones
            If albumId.HasValue Then
                ' albumOrder es obligatorio cuando se proporciona albumId
                If Not albumOrder.HasValue Then
                    jsonResponse = GenerateErrorResponse("400", "Si se especifica albumId, también se debe especificar albumOrder")
                    statusCode = HttpStatusCode.BadRequest
                    Return
                End If

                Using cmd = db.CreateCommand("INSERT INTO cancionesalbumes (idcancion, idalbum, tracknumber) VALUES (@idcancion, @idalbum, @tracknumber)")
                    cmd.Parameters.AddWithValue("@idcancion", newSongId)
                    cmd.Parameters.AddWithValue("@idalbum", albumId.Value)
                    cmd.Parameters.AddWithValue("@tracknumber", albumOrder.Value)
                    cmd.ExecuteNonQuery()
                End Using
            End If

            jsonResponse = ConvertToJson(New Dictionary(Of String, Object) From {{"songId", newSongId}})
            statusCode = HttpStatusCode.OK

        Catch ex As Exception
            jsonResponse = GenerateErrorResponse("500", "Error al crear la canción: " & ex.Message)
            statusCode = HttpStatusCode.InternalServerError
        End Try
    End Sub

    Sub searchSong(request As HttpListenerRequest, action As String, ByRef jsonResponse As String, ByRef statusCode As Integer)
        Try
            ' Obtener parámetro de búsqueda
            Dim query As String = request.QueryString("q")
            If String.IsNullOrEmpty(query) Then
                jsonResponse = GenerateErrorResponse("400", "Parámetro de búsqueda 'q' requerido")
                statusCode = HttpStatusCode.BadRequest
                Return
            End If

            ' Buscar canciones por título (puede extenderse a otros campos)
            Dim results As New List(Of Dictionary(Of String, Object))

            Using cmd = db.CreateCommand("SELECT idcancion FROM canciones WHERE LOWER(titulo) LIKE LOWER(@query)")
                cmd.Parameters.AddWithValue("@query", "%" & query & "%")
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        results.Add(New Dictionary(Of String, Object) From {{"songId", reader.GetInt32(0)}})
                    End While
                End Using
            End Using

            jsonResponse = ConvertToJson(results)
            statusCode = HttpStatusCode.OK

        Catch ex As Exception
            jsonResponse = GenerateErrorResponse("500", "Error al buscar canciones: " & ex.Message)
            statusCode = HttpStatusCode.InternalServerError
        End Try
    End Sub

    Sub listSongs(request As HttpListenerRequest, action As String, ByRef jsonResponse As String, ByRef statusCode As Integer)
        Try
            ' Obtener parámetro de lista de IDs
            Dim idsParam As String = request.QueryString("ids")
            If String.IsNullOrEmpty(idsParam) Then
                jsonResponse = GenerateErrorResponse("400", "Parámetro 'ids' requerido")
                statusCode = HttpStatusCode.BadRequest
                Return
            End If

            ' Dividir los IDs por comas
            Dim idStrings As String() = idsParam.Split(","c)
            Dim songIds As New List(Of Integer)

            ' Parsear y validar los IDs
            For Each idStr In idStrings
                Dim songId As Integer
                If Integer.TryParse(idStr.Trim(), songId) Then
                    songIds.Add(songId)
                Else
                    jsonResponse = GenerateErrorResponse("400", "ID inválido: " & idStr)
                    statusCode = HttpStatusCode.BadRequest
                    Return
                End If
            Next

            ' Obtener los datos de todas las canciones
            Dim results As New List(Of Dictionary(Of String, Object))

            For Each songId In songIds
                Dim songData As Dictionary(Of String, Object) = GetSongData(songId)
                If songData IsNot Nothing Then
                    results.Add(songData)
                End If
            Next

            jsonResponse = ConvertToJson(results)
            statusCode = HttpStatusCode.OK

        Catch ex As Exception
            jsonResponse = GenerateErrorResponse("500", "Error al obtener canciones: " & ex.Message)
            statusCode = HttpStatusCode.InternalServerError
        End Try
    End Sub

    Sub filterSongs(request As HttpListenerRequest, action As String, ByRef jsonResponse As String, ByRef statusCode As Integer)
        Try
            ' Obtener parámetros de filtro
            Dim genresParam As String = request.QueryString("genres")
            Dim artistsParam As String = request.QueryString("artists")
            Dim orderParam As String = request.QueryString("order")
            Dim directionParam As String = request.QueryString("direction")
            Dim pageParam As String = request.QueryString("page")

            ' Límite fijo de 9 elementos por página
            Const pageLimit As Integer = 9

            ' Parsear página (por defecto 1)
            Dim pageNumber As Integer = 1
            If Not String.IsNullOrEmpty(pageParam) Then
                Integer.TryParse(pageParam, pageNumber)
                If pageNumber < 1 Then pageNumber = 1
            End If

            ' Calcular OFFSET
            Dim offset As Integer = (pageNumber - 1) * pageLimit

            ' Parsear géneros
            Dim genreIds As New List(Of Integer)
            If Not String.IsNullOrEmpty(genresParam) Then
                For Each genreStr In genresParam.Split(","c)
                    Dim genreId As Integer
                    If Integer.TryParse(genreStr.Trim(), genreId) Then
                        genreIds.Add(genreId)
                    End If
                Next
            End If

            ' Parsear artistas
            Dim artistIds As New List(Of Integer)
            If Not String.IsNullOrEmpty(artistsParam) Then
                For Each artistStr In artistsParam.Split(","c)
                    Dim artistId As Integer
                    If Integer.TryParse(artistStr.Trim(), artistId) Then
                        artistIds.Add(artistId)
                    End If
                Next
            End If

            ' Construir query SQL
            Dim orderField As String = "c.idcancion"

            ' Determinar campo de ordenamiento
            If Not String.IsNullOrEmpty(orderParam) Then
                If orderParam.ToLower() = "date" Then
                    orderField = "c.fechalanzamiento"
                ElseIf orderParam.ToLower() = "name" Then
                    orderField = "c.titulo"
                End If
            End If

            ' SELECT con el campo de ordenamiento para evitar error con DISTINCT
            Dim sqlQuery As String = $"SELECT DISTINCT c.idcancion, {orderField} FROM canciones c "
            Dim whereClauses As New List(Of String)

            ' Filtro por géneros
            If genreIds.Count > 0 Then
                sqlQuery &= "INNER JOIN generoscanciones gc ON c.idcancion = gc.idcancion "
                whereClauses.Add("gc.idgenero IN (" & String.Join(",", genreIds) & ")")
            End If

            ' Filtro por artistas
            If artistIds.Count > 0 Then
                sqlQuery &= "INNER JOIN autorescanciones ac ON c.idcancion = ac.idcancion "
                whereClauses.Add("ac.idartista IN (" & String.Join(",", artistIds) & ")")
            End If

            ' Agregar WHERE clause
            If whereClauses.Count > 0 Then
                sqlQuery &= "WHERE " & String.Join(" AND ", whereClauses) & " "
            End If

            ' Agregar ORDER BY
            sqlQuery &= $"ORDER BY {orderField} "

            ' Dirección del ordenamiento
            If Not String.IsNullOrEmpty(directionParam) AndAlso directionParam.ToLower() = "desc" Then
                sqlQuery &= "DESC"
            Else
                sqlQuery &= "ASC"
            End If

            ' Añadir paginación
            sqlQuery &= $" LIMIT {pageLimit} OFFSET {offset}"

            ' Ejecutar query
            Dim results As New List(Of Integer)
            Using cmd = db.CreateCommand(sqlQuery)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        results.Add(reader.GetInt32(0))
                    End While
                End Using
            End Using

            jsonResponse = ConvertToJson(results)
            statusCode = HttpStatusCode.OK

        Catch ex As Exception
            jsonResponse = GenerateErrorResponse("500", "Error al filtrar canciones: " & ex.Message)
            statusCode = HttpStatusCode.InternalServerError
        End Try
    End Sub

    ' Función auxiliar para obtener datos completos de una canción
    Function GetSongData(songId As Integer) As Dictionary(Of String, Object)
        Try
            Dim schema As New Dictionary(Of String, Object) From {
                {"songId", songId},
                {"title", Nothing},
                {"artistId", Nothing},
                {"collaborators", Nothing},
                {"releaseDate", Nothing},
                {"description", Nothing},
                {"duration", Nothing},
                {"genres", Nothing},
                {"cover", Nothing},
                {"price", Nothing},
                {"albumId", Nothing},
                {"trackId", Nothing},
                {"albumOrder", Nothing},
                {"linked_albums", Nothing}
            }

            ' Recuperar datos básicos
            Using cmd = db.CreateCommand("SELECT titulo, descripcion, cover, duracion, fechalanzamiento, precio, track, albumog FROM canciones WHERE idcancion = @id")
                cmd.Parameters.AddWithValue("@id", songId)
                Using reader = cmd.ExecuteReader()
                    If reader.HasRows Then
                        While reader.Read()
                            schema("title") = reader.GetString(0)
                            schema("description") = If(reader.IsDBNull(1), Nothing, reader.GetString(1))
                            Dim coverBytes As Byte() = CType(reader("cover"), Byte())
                            schema("cover") = BytesToString(coverBytes)
                            schema("duration") = reader.GetInt32(3).ToString()
                            schema("releaseDate") = reader.GetDateTime(4).ToString("yyyy-MM-dd")
                            schema("price") = reader.GetDecimal(5).ToString()
                            schema("trackId") = reader.GetInt32(6).ToString()
                            schema("albumId") = If(reader.IsDBNull(7), Nothing, CType(reader.GetInt32(7), Object))
                        End While
                    Else
                        Return Nothing
                    End If
                End Using
            End Using

            ' Recuperar autor y colaboradores
            Dim collaborators As New List(Of Integer)
            Using cmd = db.CreateCommand("SELECT idartista, ft FROM autorescanciones WHERE idcancion = @id")
                cmd.Parameters.AddWithValue("@id", songId)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        If reader.GetBoolean(1) = False Then
                            schema("artistId") = reader.GetInt32(0).ToString()
                        Else
                            collaborators.Add(reader.GetInt32(0))
                        End If
                    End While
                End Using
            End Using
            schema("collaborators") = collaborators

            ' Recuperar géneros
            Dim genres As New List(Of String)
            Using cmd = db.CreateCommand("SELECT idgenero FROM generoscanciones WHERE idcancion = @id")
                cmd.Parameters.AddWithValue("@id", songId)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        genres.Add(reader.GetInt32(0).ToString())
                    End While
                End Using
            End Using
            schema("genres") = genres

            ' Recuperar albumOrder del álbum original (si existe)
            If schema("albumId") IsNot Nothing Then
                Using cmd = db.CreateCommand("SELECT tracknumber FROM cancionesalbumes WHERE idcancion = @id AND idalbum = @albumog")
                    cmd.Parameters.AddWithValue("@id", songId)
                    cmd.Parameters.AddWithValue("@albumog", CInt(schema("albumId")))
                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            schema("albumOrder") = reader.GetInt32(0)
                        End If
                    End Using
                End Using
            End If

            ' Recuperar álbumes enlazados (linked_albums)
            Dim linkedAlbums As New List(Of Integer)
            Dim albumOgId As Object = schema("albumId")
            Using cmd = db.CreateCommand("SELECT idalbum FROM cancionesalbumes WHERE idcancion = @id")
                cmd.Parameters.AddWithValue("@id", songId)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        Dim linkedAlbumId As Integer = reader.GetInt32(0)
                        If albumOgId Is Nothing OrElse linkedAlbumId <> CInt(albumOgId) Then
                            linkedAlbums.Add(linkedAlbumId)
                        End If
                    End While
                End Using
            End Using
            schema("linked_albums") = linkedAlbums

            Return schema

        Catch ex As Exception
            Console.WriteLine($"Error al obtener datos de canción {songId}: {ex.Message}")
            Return Nothing
        End Try
    End Function

    Sub getSong(request As HttpListenerRequest, action As String, ByRef jsonResponse As String, ByRef statusCode As Integer)
        If Not IsNumeric(action) Then
            jsonResponse = GenerateErrorResponse("400", "ID de canción inválido")
            statusCode = HttpStatusCode.BadRequest
            Return
        End If

        ' Schema:
        Dim schema As New Dictionary(Of String, Object) From {
            {"songId", Nothing},
            {"title", Nothing},
            {"artistId", Nothing},
            {"collaborators", Nothing},
            {"releaseDate", Nothing},
            {"description", Nothing},
            {"duration", Nothing},
            {"genres", Nothing},
            {"cover", Nothing},
            {"price", Nothing},
            {"albumId", Nothing},
            {"trackId", Nothing},
            {"albumOrder", Nothing},
            {"linked_albums", Nothing}
        }

        ' Recuperar todas las filas
        Using cmd = db.CreateCommand("SELECT titulo, descripcion, cover, duracion, fechalanzamiento, precio, track, albumog FROM canciones WHERE idcancion = @id")
            cmd.Parameters.AddWithValue("@id", Integer.Parse(action))
            Using reader = cmd.ExecuteReader()
                If reader.HasRows Then
                    While reader.Read()
                        schema("songId") = action
                        schema("title") = reader.GetString(0)
                        schema("description") = If(reader.IsDBNull(1), Nothing, reader.GetString(1))
                        Dim coverBytes As Byte() = CType(reader("cover"), Byte())
                        schema("cover") = BytesToString(coverBytes)
                        schema("duration") = reader.GetInt32(3).ToString()
                        schema("releaseDate") = reader.GetDateTime(4).ToString("yyyy-MM-dd")
                        schema("price") = reader.GetDecimal(5).ToString()
                        schema("trackId") = reader.GetInt32(6).ToString()
                        ' albumId proviene de albumog (NULL = single, sin álbum)
                        schema("albumId") = If(reader.IsDBNull(7), Nothing, CType(reader.GetInt32(7), Object))
                    End While
                Else
                    jsonResponse = ""
                    statusCode = HttpStatusCode.NotFound
                    Return
                End If
            End Using
        End Using

        ' Recuperar autor y colaboradores
        Dim collaborators As New List(Of Integer)
        Using cmd = db.CreateCommand("SELECT idartista, ft FROM autorescanciones WHERE idcancion = @id")
            cmd.Parameters.AddWithValue("@id", Integer.Parse(action))
            Using reader = cmd.ExecuteReader()
                While reader.Read()
                    If reader.GetBoolean(1) = False Then
                        schema("artistId") = reader.GetInt32(0).ToString()
                    Else
                        collaborators.Add(reader.GetInt32(0))
                    End If
                End While
            End Using
        End Using
        schema("collaborators") = collaborators

        ' Recuperar géneros
        Dim genres As New List(Of String)
        Using cmd = db.CreateCommand("SELECT idgenero FROM generoscanciones WHERE idcancion = @id")
            cmd.Parameters.AddWithValue("@id", Integer.Parse(action))
            Using reader = cmd.ExecuteReader()
                While reader.Read()
                    genres.Add(reader.GetInt32(0).ToString())
                End While
            End Using
        End Using
        schema("genres") = genres

        ' Recuperar albumOrder del álbum original (si existe)
        If schema("albumId") IsNot Nothing Then
            Using cmd = db.CreateCommand("SELECT tracknumber FROM cancionesalbumes WHERE idcancion = @id AND idalbum = @albumog")
                cmd.Parameters.AddWithValue("@id", Integer.Parse(action))
                cmd.Parameters.AddWithValue("@albumog", CInt(schema("albumId")))
                Using reader = cmd.ExecuteReader()
                    If reader.Read() Then
                        schema("albumOrder") = reader.GetInt32(0)
                    End If
                End Using
            End Using
        End If

        ' Recuperar álbumes enlazados (linked_albums) - excluir el albumog
        Dim linkedAlbums As New List(Of Integer)
        Dim albumOgId As Object = schema("albumId")
        Using cmd = db.CreateCommand("SELECT idalbum FROM cancionesalbumes WHERE idcancion = @id")
            cmd.Parameters.AddWithValue("@id", Integer.Parse(action))
            Using reader = cmd.ExecuteReader()
                While reader.Read()
                    Dim linkedAlbumId As Integer = reader.GetInt32(0)
                    ' Excluir el albumog de la lista de linked_albums
                    If albumOgId Is Nothing OrElse linkedAlbumId <> CInt(albumOgId) Then
                        linkedAlbums.Add(linkedAlbumId)
                    End If
                End While
            End Using
        End Using
        schema("linked_albums") = linkedAlbums

        jsonResponse = ConvertToJson(schema)
        statusCode = HttpStatusCode.OK
    End Sub

    Sub deleteSong(request As HttpListenerRequest, action As String, ByRef jsonResponse As String, ByRef statusCode As Integer, userId As Integer)
        Try
            If Not IsNumeric(action) Then
                jsonResponse = GenerateErrorResponse("400", "ID de canción inválido")
                statusCode = HttpStatusCode.BadRequest
                Return
            End If

            Dim songId As Integer = Integer.Parse(action)

            ' Eliminar canción (las relaciones se eliminan en cascada)
            Using cmd = db.CreateCommand("DELETE FROM canciones WHERE idcancion = @id")
                cmd.Parameters.AddWithValue("@id", songId)
                Dim rowsAffected As Integer = cmd.ExecuteNonQuery()

                If rowsAffected = 0 Then
                    jsonResponse = GenerateErrorResponse("404", "Canción no encontrada")
                    statusCode = HttpStatusCode.NotFound
                Else
                    jsonResponse = ""
                    statusCode = HttpStatusCode.OK
                End If
            End Using

        Catch ex As Exception
            jsonResponse = GenerateErrorResponse("500", "Error al eliminar la canción: " & ex.Message)
            statusCode = HttpStatusCode.InternalServerError
        End Try
    End Sub

    Sub updateSong(request As HttpListenerRequest, action As String, ByRef jsonResponse As String, ByRef statusCode As Integer, userId As Integer)
        Try
            If Not IsNumeric(action) Then
                jsonResponse = GenerateErrorResponse("400", "ID de canción inválido")
                statusCode = HttpStatusCode.BadRequest
                Return
            End If

            Dim songId As Integer = Integer.Parse(action)

            ' Leer el body del request
            Dim body As String
            Using reader As New StreamReader(request.InputStream, request.ContentEncoding)
                body = reader.ReadToEnd()
            End Using

            Dim songData = JsonSerializer.Deserialize(Of Dictionary(Of String, JsonElement))(body)

            ' Validar price si está presente
            If songData.ContainsKey("price") Then
                Dim price As Decimal = songData("price").GetDecimal()
                If price <= 0 Then
                    jsonResponse = GenerateErrorResponse("400", "El precio debe ser un valor positivo")
                    statusCode = HttpStatusCode.BadRequest
                    Return
                End If
            End If

            ' Construir UPDATE dinámico solo con los campos presentes
            ' NOTA: albumId (albumog) NO se puede modificar una vez creada la canción
            Dim updates As New List(Of String)
            Dim cmdText As String = "UPDATE canciones SET "

            Using cmd = db.CreateCommand("")
                If songData.ContainsKey("title") Then
                    updates.Add("titulo = @titulo")
                    cmd.Parameters.AddWithValue("@titulo", songData("title").GetString())
                End If
                If songData.ContainsKey("description") Then
                    updates.Add("descripcion = @descripcion")
                    cmd.Parameters.AddWithValue("@descripcion", If(songData("description").ValueKind = JsonValueKind.Null, DBNull.Value, CType(songData("description").GetString(), Object)))
                End If
                If songData.ContainsKey("cover") Then
                    updates.Add("cover = @cover")
                    cmd.Parameters.AddWithValue("@cover", StringToBytes(songData("cover").GetString()))
                End If
                If songData.ContainsKey("price") Then
                    updates.Add("precio = @precio")
                    cmd.Parameters.AddWithValue("@precio", songData("price").GetDecimal())
                End If
                If songData.ContainsKey("releaseDate") Then
                    updates.Add("fechalanzamiento = @fecha")
                    cmd.Parameters.AddWithValue("@fecha", Date.Parse(songData("releaseDate").GetString()))
                End If
                If songData.ContainsKey("trackId") Then
                    updates.Add("track = @track")
                    cmd.Parameters.AddWithValue("@track", songData("trackId").GetInt32())
                End If
                If songData.ContainsKey("duration") Then
                    updates.Add("duracion = @duracion")
                    cmd.Parameters.AddWithValue("@duracion", songData("duration").GetInt32())
                End If

                If updates.Count > 0 Then
                    cmd.CommandText = cmdText & String.Join(", ", updates) & " WHERE idcancion = @id"
                    cmd.Parameters.AddWithValue("@id", songId)
                    cmd.ExecuteNonQuery()
                End If
            End Using

            ' Actualizar géneros si están presentes
            If songData.ContainsKey("genres") Then
                ' Validar que todos los géneros existan
                For Each genreElement In songData("genres").EnumerateArray()
                    Dim genreId As Integer = genreElement.GetInt32()
                    Dim genreExists As Boolean = False
                    Using cmd = db.CreateCommand("SELECT COUNT(*) FROM generos WHERE idgenero = @idgenero")
                        cmd.Parameters.AddWithValue("@idgenero", genreId)
                        Dim count As Integer = CInt(cmd.ExecuteScalar())
                        genreExists = count > 0
                    End Using

                    If Not genreExists Then
                        jsonResponse = GenerateErrorResponse("422", $"El género con ID {genreId} no existe")
                        statusCode = 422 ' Unprocessable Entity
                        Return
                    End If
                Next

                Using cmd = db.CreateCommand("DELETE FROM generoscanciones WHERE idcancion = @id")
                    cmd.Parameters.AddWithValue("@id", songId)
                    cmd.ExecuteNonQuery()
                End Using

                For Each genreElement In songData("genres").EnumerateArray()
                    Using cmd = db.CreateCommand("INSERT INTO generoscanciones (idcancion, idgenero) VALUES (@idcancion, @idgenero)")
                        cmd.Parameters.AddWithValue("@idcancion", songId)
                        cmd.Parameters.AddWithValue("@idgenero", genreElement.GetInt32())
                        cmd.ExecuteNonQuery()
                    End Using
                Next
            End If

            ' Actualizar colaboradores si están presentes
            If songData.ContainsKey("collaborators") Then
                Using cmd = db.CreateCommand("DELETE FROM autorescanciones WHERE idcancion = @id AND ft = true")
                    cmd.Parameters.AddWithValue("@id", songId)
                    cmd.ExecuteNonQuery()
                End Using

                For Each collabElement In songData("collaborators").EnumerateArray()
                    Using cmd = db.CreateCommand("INSERT INTO autorescanciones (idartista, idcancion, ft) VALUES (@idartista, @idcancion, @ft)")
                        cmd.Parameters.AddWithValue("@idartista", collabElement.GetInt32())
                        cmd.Parameters.AddWithValue("@idcancion", songId)
                        cmd.Parameters.AddWithValue("@ft", True)
                        cmd.ExecuteNonQuery()
                    End Using
                Next
            End If

            ' Actualizar orden en álbum si está presente
            If songData.ContainsKey("albumOrder") AndAlso songData("albumOrder").ValueKind <> JsonValueKind.Null Then
                Using cmd = db.CreateCommand("UPDATE cancionesalbumes SET tracknumber = @tracknumber WHERE idcancion = @id")
                    cmd.Parameters.AddWithValue("@tracknumber", songData("albumOrder").GetInt32())
                    cmd.Parameters.AddWithValue("@id", songId)
                    cmd.ExecuteNonQuery()
                End Using
            End If

            jsonResponse = ""
            statusCode = HttpStatusCode.OK

        Catch ex As Exception
            jsonResponse = GenerateErrorResponse("500", "Error al actualizar la canción: " & ex.Message)
            statusCode = HttpStatusCode.InternalServerError
        End Try
    End Sub

    '==========================================================================
    ' MÉTODOS PARA ALBUM
    '==========================================================================
    Sub uploadAlbum(request As HttpListenerRequest, action As String, ByRef jsonResponse As String, ByRef statusCode As Integer, userId As Integer)
        Try
            Dim body As String
            Using reader As New StreamReader(request.InputStream, request.ContentEncoding)
                body = reader.ReadToEnd()
            End Using

            Dim albumData = JsonSerializer.Deserialize(Of Dictionary(Of String, JsonElement))(body)

            ' Validar campos requeridos
            If Not albumData.ContainsKey("title") OrElse
               Not albumData.ContainsKey("songs") OrElse Not albumData.ContainsKey("cover") OrElse
               Not albumData.ContainsKey("price") Then
                jsonResponse = GenerateErrorResponse("400", "Faltan campos requeridos")
                statusCode = HttpStatusCode.BadRequest
                Return
            End If

            Dim title As String = albumData("title").GetString()
            Dim cover As String = albumData("cover").GetString()
            Dim price As Decimal = albumData("price").GetDecimal()
            Dim releaseDate As String = If(albumData.ContainsKey("releaseDate"), albumData("releaseDate").GetString(), DateTime.Now.ToString("yyyy-MM-dd"))
            Dim description As String = If(albumData.ContainsKey("description"), albumData("description").GetString(), "")

            ' Validar que price sea positivo
            If price <= 0 Then
                jsonResponse = GenerateErrorResponse("400", "El precio debe ser un valor positivo")
                statusCode = HttpStatusCode.BadRequest
                Return
            End If

            ' Validar que todas las canciones existan
            If albumData.ContainsKey("songs") Then
                For Each songElement In albumData("songs").EnumerateArray()
                    Dim songId As Integer = songElement.GetInt32()
                    Dim songExists As Boolean = False
                    Using cmd = db.CreateCommand("SELECT COUNT(*) FROM canciones WHERE idcancion = @idcancion")
                        cmd.Parameters.AddWithValue("@idcancion", songId)
                        Dim count As Integer = CInt(cmd.ExecuteScalar())
                        songExists = count > 0
                    End Using

                    If Not songExists Then
                        jsonResponse = GenerateErrorResponse("422", $"La canción con ID {songId} no existe")
                        statusCode = 422 ' Unprocessable Entity
                        Return
                    End If
                Next
            End If

            ' Insertar álbum
            Dim newAlbumId As Integer
            Using cmd = db.CreateCommand("INSERT INTO albumes (titulo, descripcion, cover, fechalanzamiento, precio, precioauto) VALUES (@titulo, @descripcion, @cover, @fecha, @precio, @precioauto) RETURNING idalbum")
                cmd.Parameters.AddWithValue("@titulo", title)
                cmd.Parameters.AddWithValue("@descripcion", If(description, DBNull.Value))
                cmd.Parameters.AddWithValue("@cover", StringToBytes(cover))
                cmd.Parameters.AddWithValue("@fecha", Date.Parse(releaseDate))
                cmd.Parameters.AddWithValue("@precio", price)
                cmd.Parameters.AddWithValue("@precioauto", False)
                newAlbumId = CInt(cmd.ExecuteScalar())
            End Using

            ' Obtener el ID del artista asociado al usuario autenticado
            Dim artistId As Integer? = GetArtistIdByUserId(userId)

            If Not artistId.HasValue Then
                jsonResponse = GenerateErrorResponse("403", "El usuario no tiene un artista asociado")
                statusCode = HttpStatusCode.Forbidden
                Return
            End If

            ' Insertar al artista principal (el usuario autenticado) - NO es colaborador (ft = false)
            Using cmd = db.CreateCommand("INSERT INTO autoresalbumes (idartista, idalbum, ft) VALUES (@idartista, @idalbum, @ft)")
                cmd.Parameters.AddWithValue("@idartista", artistId.Value)
                cmd.Parameters.AddWithValue("@idalbum", newAlbumId)
                cmd.Parameters.AddWithValue("@ft", False) ' No es colaborador, es el artista principal
                cmd.ExecuteNonQuery()
            End Using

            ' Insertar colaboradores (artistas con ft = true)
            If albumData.ContainsKey("collaborators") Then
                For Each collabElement In albumData("collaborators").EnumerateArray()
                    Dim collabArtistId As Integer = collabElement.GetInt32()
                    Using cmd = db.CreateCommand("INSERT INTO autoresalbumes (idartista, idalbum, ft) VALUES (@idartista, @idalbum, @ft)")
                        cmd.Parameters.AddWithValue("@idartista", collabArtistId)
                        cmd.Parameters.AddWithValue("@idalbum", newAlbumId)
                        cmd.Parameters.AddWithValue("@ft", True) ' Es colaborador
                        cmd.ExecuteNonQuery()
                    End Using
                Next
            End If

            ' Vincular canciones al álbum
            If albumData.ContainsKey("songs") Then
                Dim trackNumber As Integer = 1
                For Each songElement In albumData("songs").EnumerateArray()
                    Dim songId As Integer = songElement.GetInt32()
                    Using cmd = db.CreateCommand("INSERT INTO cancionesalbumes (idcancion, idalbum, tracknumber) VALUES (@idcancion, @idalbum, @tracknumber) ON CONFLICT (idcancion, idalbum) DO UPDATE SET tracknumber = @tracknumber")
                        cmd.Parameters.AddWithValue("@idcancion", songId)
                        cmd.Parameters.AddWithValue("@idalbum", newAlbumId)
                        cmd.Parameters.AddWithValue("@tracknumber", trackNumber)
                        cmd.ExecuteNonQuery()
                    End Using
                    trackNumber += 1
                Next
            End If

            jsonResponse = ConvertToJson(New Dictionary(Of String, Object) From {{"albumId", newAlbumId}})
            statusCode = HttpStatusCode.OK

        Catch ex As Exception
            jsonResponse = GenerateErrorResponse("500", "Error al crear el álbum: " & ex.Message)
            statusCode = HttpStatusCode.InternalServerError
        End Try
    End Sub

    Sub searchAlbum(request As HttpListenerRequest, action As String, ByRef jsonResponse As String, ByRef statusCode As Integer)
        Try
            Dim query As String = request.QueryString("q")
            If String.IsNullOrEmpty(query) Then
                jsonResponse = GenerateErrorResponse("400", "Parámetro de búsqueda 'q' requerido")
                statusCode = HttpStatusCode.BadRequest
                Return
            End If

            Dim results As New List(Of Dictionary(Of String, Object))

            Using cmd = db.CreateCommand("SELECT idalbum FROM albumes WHERE LOWER(titulo) LIKE LOWER(@query)")
                cmd.Parameters.AddWithValue("@query", "%" & query & "%")
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        results.Add(New Dictionary(Of String, Object) From {{"albumId", reader.GetInt32(0)}})
                    End While
                End Using
            End Using

            jsonResponse = ConvertToJson(results)
            statusCode = HttpStatusCode.OK

        Catch ex As Exception
            jsonResponse = GenerateErrorResponse("500", "Error al buscar álbumes: " & ex.Message)
            statusCode = HttpStatusCode.InternalServerError
        End Try
    End Sub

    Sub listAlbums(request As HttpListenerRequest, action As String, ByRef jsonResponse As String, ByRef statusCode As Integer)
        Try
            ' Obtener parámetro de lista de IDs
            Dim idsParam As String = request.QueryString("ids")
            If String.IsNullOrEmpty(idsParam) Then
                jsonResponse = GenerateErrorResponse("400", "Parámetro 'ids' requerido")
                statusCode = HttpStatusCode.BadRequest
                Return
            End If

            ' Dividir los IDs por comas
            Dim idStrings As String() = idsParam.Split(","c)
            Dim albumIds As New List(Of Integer)

            ' Parsear y validar los IDs
            For Each idStr In idStrings
                Dim albumId As Integer
                If Integer.TryParse(idStr.Trim(), albumId) Then
                    albumIds.Add(albumId)
                Else
                    jsonResponse = GenerateErrorResponse("400", "ID inválido: " & idStr)
                    statusCode = HttpStatusCode.BadRequest
                    Return
                End If
            Next

            ' Obtener los datos de todos los álbumes
            Dim results As New List(Of Dictionary(Of String, Object))

            For Each albumId In albumIds
                Dim albumData As Dictionary(Of String, Object) = GetAlbumData(albumId)
                If albumData IsNot Nothing Then
                    results.Add(albumData)
                End If
            Next

            jsonResponse = ConvertToJson(results)
            statusCode = HttpStatusCode.OK

        Catch ex As Exception
            jsonResponse = GenerateErrorResponse("500", "Error al obtener álbumes: " & ex.Message)
            statusCode = HttpStatusCode.InternalServerError
        End Try
    End Sub

    Sub filterAlbums(request As HttpListenerRequest, action As String, ByRef jsonResponse As String, ByRef statusCode As Integer)
        Try
            ' Obtener parámetros de filtro
            Dim genresParam As String = request.QueryString("genres")
            Dim artistsParam As String = request.QueryString("artists")
            Dim orderParam As String = request.QueryString("order")
            Dim directionParam As String = request.QueryString("direction")
            Dim pageParam As String = request.QueryString("page")

            ' Límite fijo de 9 elementos por página
            Const pageLimit As Integer = 9

            ' Parsear página (por defecto 1)
            Dim pageNumber As Integer = 1
            If Not String.IsNullOrEmpty(pageParam) Then
                Integer.TryParse(pageParam, pageNumber)
                If pageNumber < 1 Then pageNumber = 1
            End If

            ' Calcular OFFSET
            Dim offset As Integer = (pageNumber - 1) * pageLimit

            ' Parsear géneros
            Dim genreIds As New List(Of Integer)
            If Not String.IsNullOrEmpty(genresParam) Then
                For Each genreStr In genresParam.Split(","c)
                    Dim genreId As Integer
                    If Integer.TryParse(genreStr.Trim(), genreId) Then
                        genreIds.Add(genreId)
                    End If
                Next
            End If

            ' Parsear artistas
            Dim artistIds As New List(Of Integer)
            If Not String.IsNullOrEmpty(artistsParam) Then
                For Each artistStr In artistsParam.Split(","c)
                    Dim artistId As Integer
                    If Integer.TryParse(artistStr.Trim(), artistId) Then
                        artistIds.Add(artistId)
                    End If
                Next
            End If

            ' Construir query SQL
            Dim orderField As String = "a.idalbum"

            ' Determinar campo de ordenamiento
            If Not String.IsNullOrEmpty(orderParam) Then
                If orderParam.ToLower() = "date" Then
                    orderField = "a.fechalanzamiento"
                ElseIf orderParam.ToLower() = "name" Then
                    orderField = "a.titulo"
                End If
            End If

            ' SELECT con el campo de ordenamiento para evitar error con DISTINCT
            Dim sqlQuery As String = $"SELECT DISTINCT a.idalbum, {orderField} FROM albumes a "
            Dim whereClauses As New List(Of String)

            ' Filtro por géneros (a través de las canciones del álbum)
            If genreIds.Count > 0 Then
                sqlQuery &= "INNER JOIN cancionesalbumes ca ON a.idalbum = ca.idalbum " &
                           "INNER JOIN generoscanciones gc ON ca.idcancion = gc.idcancion "
                whereClauses.Add("gc.idgenero IN (" & String.Join(",", genreIds) & ")")
            End If

            ' Filtro por artistas
            If artistIds.Count > 0 Then
                sqlQuery &= "INNER JOIN autoresalbumes aa ON a.idalbum = aa.idalbum "
                whereClauses.Add("aa.idartista IN (" & String.Join(",", artistIds) & ")")
            End If

            ' Agregar WHERE clause
            If whereClauses.Count > 0 Then
                sqlQuery &= "WHERE " & String.Join(" AND ", whereClauses) & " "
            End If

            ' Agregar ORDER BY
            sqlQuery &= $"ORDER BY {orderField} "

            ' Dirección del ordenamiento
            If Not String.IsNullOrEmpty(directionParam) AndAlso directionParam.ToLower() = "desc" Then
                sqlQuery &= "DESC"
            Else
                sqlQuery &= "ASC"
            End If

            ' Añadir paginación solo si se especifica el parámetro page
            If Not String.IsNullOrEmpty(pageParam) Then
                sqlQuery &= $" LIMIT {pageLimit} OFFSET {offset}"
            End If

            ' Ejecutar query
            Dim results As New List(Of Integer)
            Using cmd = db.CreateCommand(sqlQuery)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        results.Add(reader.GetInt32(0))
                    End While
                End Using
            End Using

            jsonResponse = ConvertToJson(results)
            statusCode = HttpStatusCode.OK

        Catch ex As Exception
            jsonResponse = GenerateErrorResponse("500", "Error al filtrar álbumes: " & ex.Message)
            statusCode = HttpStatusCode.InternalServerError
        End Try
    End Sub

    ' Función auxiliar para obtener datos completos de un álbum
    Function GetAlbumData(albumId As Integer) As Dictionary(Of String, Object)
        Try
            Dim schema As New Dictionary(Of String, Object) From {
                {"albumId", albumId},
                {"title", Nothing},
                {"artistId", Nothing},
                {"collaborators", Nothing},
                {"description", Nothing},
                {"releaseDate", Nothing},
                {"genres", Nothing},
                {"songs", Nothing},
                {"cover", Nothing},
                {"price", Nothing}
            }

            ' Recuperar datos del álbum
            Using cmd = db.CreateCommand("SELECT titulo, descripcion, cover, fechalanzamiento, precio FROM albumes WHERE idalbum = @id")
                cmd.Parameters.AddWithValue("@id", albumId)
                Using reader = cmd.ExecuteReader()
                    If reader.HasRows Then
                        While reader.Read()
                            schema("title") = reader.GetString(0)
                            schema("description") = If(reader.IsDBNull(1), "", reader.GetString(1))
                            Dim coverBytes As Byte() = CType(reader("cover"), Byte())
                            schema("cover") = Convert.ToBase64String(coverBytes)
                            schema("releaseDate") = reader.GetDateTime(3).ToString("yyyy-MM-dd")
                            schema("price") = reader.GetDecimal(4).ToString()
                        End While
                    Else
                        Return Nothing
                    End If
                End Using
            End Using

            ' Recuperar autor y colaboradores
            Dim collaborators As New List(Of Integer)
            Using cmd = db.CreateCommand("SELECT idartista, ft FROM autoresalbumes WHERE idalbum = @id")
                cmd.Parameters.AddWithValue("@id", albumId)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        If reader.GetBoolean(1) = False Then
                            schema("artistId") = reader.GetInt32(0).ToString()
                        Else
                            collaborators.Add(reader.GetInt32(0))
                        End If
                    End While
                End Using
            End Using
            schema("collaborators") = collaborators

            ' Recuperar canciones del álbum
            Dim songs As New List(Of Integer)
            Using cmd = db.CreateCommand("SELECT idcancion FROM cancionesalbumes WHERE idalbum = @id ORDER BY tracknumber")
                cmd.Parameters.AddWithValue("@id", albumId)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        songs.Add(reader.GetInt32(0))
                    End While
                End Using
            End Using
            schema("songs") = songs

            ' Recuperar géneros únicos de todas las canciones del álbum
            Dim genres As New List(Of Integer)
            Using cmd = db.CreateCommand("SELECT DISTINCT gc.idgenero FROM generoscanciones gc INNER JOIN cancionesalbumes ca ON gc.idcancion = ca.idcancion WHERE ca.idalbum = @id ORDER BY gc.idgenero")
                cmd.Parameters.AddWithValue("@id", albumId)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        genres.Add(reader.GetInt32(0))
                    End While
                End Using
            End Using
            schema("genres") = genres

            Return schema

        Catch ex As Exception
            Console.WriteLine($"Error al obtener datos de álbum {albumId}: {ex.Message}")
            Return Nothing
        End Try
    End Function

    Sub getAlbum(request As HttpListenerRequest, action As String, ByRef jsonResponse As String, ByRef statusCode As Integer)
        Try
            If Not IsNumeric(action) Then
                jsonResponse = GenerateErrorResponse("400", "ID de álbum inválido")
                statusCode = HttpStatusCode.BadRequest
                Return
            End If

            Dim schema As New Dictionary(Of String, Object) From {
                {"albumId", Nothing},
                {"title", Nothing},
                {"artistId", Nothing},
                {"collaborators", Nothing},
                {"description", Nothing},
                {"releaseDate", Nothing},
                {"genres", Nothing},
                {"songs", Nothing},
                {"cover", Nothing},
                {"price", Nothing}
            }

            ' Recuperar datos del álbum
            Using cmd = db.CreateCommand("SELECT titulo, descripcion, cover, fechalanzamiento, precio FROM albumes WHERE idalbum = @id")
                cmd.Parameters.AddWithValue("@id", Integer.Parse(action))
                Using reader = cmd.ExecuteReader()
                    If reader.HasRows Then
                        While reader.Read()
                            schema("albumId") = action
                            schema("title") = reader.GetString(0)
                            schema("description") = If(reader.IsDBNull(1), "", reader.GetString(1))
                            Dim coverBytes As Byte() = CType(reader("cover"), Byte())
                            schema("cover") = Convert.ToBase64String(coverBytes)
                            schema("releaseDate") = reader.GetDateTime(3).ToString("yyyy-MM-dd")
                            schema("price") = reader.GetDecimal(4).ToString()
                        End While
                    Else
                        jsonResponse = ""
                        statusCode = HttpStatusCode.NotFound
                        Return
                    End If
                End Using
            End Using

            ' Recuperar autor y colaboradores
            Dim collaborators As New List(Of Integer)
            Using cmd = db.CreateCommand("SELECT idartista, ft FROM autoresalbumes WHERE idalbum = @id")
                cmd.Parameters.AddWithValue("@id", Integer.Parse(action))
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        If reader.GetBoolean(1) = False Then
                            schema("artistId") = reader.GetInt32(0).ToString()
                        Else
                            collaborators.Add(reader.GetInt32(0))
                        End If
                    End While
                End Using
            End Using
            schema("collaborators") = collaborators

            ' Recuperar canciones del álbum
            Dim songs As New List(Of Integer)
            Using cmd = db.CreateCommand("SELECT idcancion FROM cancionesalbumes WHERE idalbum = @id ORDER BY tracknumber")
                cmd.Parameters.AddWithValue("@id", Integer.Parse(action))
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        songs.Add(reader.GetInt32(0))
                    End While
                End Using
            End Using
            schema("songs") = songs

            ' Recuperar géneros únicos de todas las canciones del álbum
            Dim genres As New List(Of Integer)
            Using cmd = db.CreateCommand("SELECT DISTINCT gc.idgenero FROM generoscanciones gc INNER JOIN cancionesalbumes ca ON gc.idcancion = ca.idcancion WHERE ca.idalbum = @id ORDER BY gc.idgenero")
                cmd.Parameters.AddWithValue("@id", Integer.Parse(action))
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        genres.Add(reader.GetInt32(0))
                    End While
                End Using
            End Using
            schema("genres") = genres

            jsonResponse = ConvertToJson(schema)
            statusCode = HttpStatusCode.OK

        Catch ex As Exception
            jsonResponse = GenerateErrorResponse("500", "Error al obtener el álbum: " & ex.Message)
            statusCode = HttpStatusCode.InternalServerError
        End Try
    End Sub

    Sub deleteAlbum(request As HttpListenerRequest, action As String, ByRef jsonResponse As String, ByRef statusCode As Integer, userId As Integer)
        Try
            If Not IsNumeric(action) Then
                jsonResponse = GenerateErrorResponse("400", "ID de álbum inválido")
                statusCode = HttpStatusCode.BadRequest
                Return
            End If

            Dim albumId As Integer = Integer.Parse(action)

            ' Antes de eliminar el álbum, poner albumog a NULL en todas las canciones
            ' que tengan este álbum como álbum original (se convierten en singles)
            Using cmd = db.CreateCommand("UPDATE canciones SET albumog = NULL WHERE albumog = @id")
                cmd.Parameters.AddWithValue("@id", albumId)
                cmd.ExecuteNonQuery()
            End Using

            ' Eliminar álbum (las relaciones en cancionesalbumes se eliminan en cascada)
            Using cmd = db.CreateCommand("DELETE FROM albumes WHERE idalbum = @id")
                cmd.Parameters.AddWithValue("@id", albumId)
                Dim rowsAffected As Integer = cmd.ExecuteNonQuery()

                If rowsAffected = 0 Then
                    jsonResponse = GenerateErrorResponse("404", "Álbum no encontrado")
                    statusCode = HttpStatusCode.NotFound
                Else
                    jsonResponse = ""
                    statusCode = HttpStatusCode.OK
                End If
            End Using

        Catch ex As Exception
            jsonResponse = GenerateErrorResponse("500", "Error al eliminar el álbum: " & ex.Message)
            statusCode = HttpStatusCode.InternalServerError
        End Try
    End Sub

    Sub updateAlbum(request As HttpListenerRequest, action As String, ByRef jsonResponse As String, ByRef statusCode As Integer, userId As Integer)
        Try
            If Not IsNumeric(action) Then
                jsonResponse = GenerateErrorResponse("400", "ID de álbum inválido")
                statusCode = HttpStatusCode.BadRequest
                Return
            End If

            Dim albumId As Integer = Integer.Parse(action)

            Dim body As String
            Using reader As New StreamReader(request.InputStream, request.ContentEncoding)
                body = reader.ReadToEnd()
            End Using

            Dim albumData = JsonSerializer.Deserialize(Of Dictionary(Of String, JsonElement))(body)

            ' Validar price si está presente
            If albumData.ContainsKey("price") Then
                Dim price As Decimal = albumData("price").GetDecimal()
                If price <= 0 Then
                    jsonResponse = GenerateErrorResponse("400", "El precio debe ser un valor positivo")
                    statusCode = HttpStatusCode.BadRequest
                    Return
                End If
            End If

            ' Validar que todas las canciones existan si se van a actualizar
            If albumData.ContainsKey("songs") Then
                For Each songElement In albumData("songs").EnumerateArray()
                    Dim songId As Integer = songElement.GetInt32()
                    Dim songExists As Boolean = False
                    Using cmd = db.CreateCommand("SELECT COUNT(*) FROM canciones WHERE idcancion = @idcancion")
                        cmd.Parameters.AddWithValue("@idcancion", songId)
                        Dim count As Integer = CInt(cmd.ExecuteScalar())
                        songExists = count > 0
                    End Using

                    If Not songExists Then
                        jsonResponse = GenerateErrorResponse("422", $"La canción con ID {songId} no existe")
                        statusCode = 422 ' Unprocessable Entity
                        Return
                    End If
                Next
            End If

            ' Construir UPDATE dinámico
            Dim updates As New List(Of String)
            Dim cmdText As String = "UPDATE albumes SET "

            Using cmd = db.CreateCommand("")
                If albumData.ContainsKey("title") Then
                    updates.Add("titulo = @titulo")
                    cmd.Parameters.AddWithValue("@titulo", albumData("title").GetString())
                End If
                If albumData.ContainsKey("description") Then
                    updates.Add("descripcion = @descripcion")
                    cmd.Parameters.AddWithValue("@descripcion", If(albumData("description").ValueKind = JsonValueKind.Null, DBNull.Value, CType(albumData("description").GetString(), Object)))
                End If
                If albumData.ContainsKey("cover") Then
                    updates.Add("cover = @cover")
                    cmd.Parameters.AddWithValue("@cover", StringToBytes(albumData("cover").GetString()))
                End If
                If albumData.ContainsKey("price") Then
                    updates.Add("precio = @precio")
                    cmd.Parameters.AddWithValue("@precio", albumData("price").GetDecimal())
                End If
                If albumData.ContainsKey("releaseDate") Then
                    updates.Add("fechalanzamiento = @fecha")
                    cmd.Parameters.AddWithValue("@fecha", Date.Parse(albumData("releaseDate").GetString()))
                End If

                If updates.Count > 0 Then
                    cmd.CommandText = cmdText & String.Join(", ", updates) & " WHERE idalbum = @id"
                    cmd.Parameters.AddWithValue("@id", albumId)
                    cmd.ExecuteNonQuery()
                End If
            End Using

            ' Actualizar colaboradores si están presentes
            If albumData.ContainsKey("collaborators") Then
                Using cmd = db.CreateCommand("DELETE FROM autoresalbumes WHERE idalbum = @id AND ft = true")
                    cmd.Parameters.AddWithValue("@id", albumId)
                    cmd.ExecuteNonQuery()
                End Using

                For Each collabElement In albumData("collaborators").EnumerateArray()
                    Using cmd = db.CreateCommand("INSERT INTO autoresalbumes (idartista, idalbum, ft) VALUES (@idartista, @idalbum, @ft)")
                        cmd.Parameters.AddWithValue("@idartista", collabElement.GetInt32())
                        cmd.Parameters.AddWithValue("@idalbum", albumId)
                        cmd.Parameters.AddWithValue("@ft", True)
                        cmd.ExecuteNonQuery()
                    End Using
                Next
            End If

            ' Actualizar canciones del álbum si están presentes
            If albumData.ContainsKey("songs") Then
                Using cmd = db.CreateCommand("DELETE FROM cancionesalbumes WHERE idalbum = @id")
                    cmd.Parameters.AddWithValue("@id", albumId)
                    cmd.ExecuteNonQuery()
                End Using

                Dim trackNumber As Integer = 1
                For Each songElement In albumData("songs").EnumerateArray()
                    Using cmd = db.CreateCommand("INSERT INTO cancionesalbumes (idcancion, idalbum, tracknumber) VALUES (@idcancion, @idalbum, @tracknumber)")
                        cmd.Parameters.AddWithValue("@idcancion", songElement.GetInt32())
                        cmd.Parameters.AddWithValue("@idalbum", albumId)
                        cmd.Parameters.AddWithValue("@tracknumber", trackNumber)
                        cmd.ExecuteNonQuery()
                    End Using
                    trackNumber += 1
                Next
            End If

            jsonResponse = ""
            statusCode = HttpStatusCode.OK

        Catch ex As Exception
            jsonResponse = GenerateErrorResponse("500", "Error al actualizar el álbum: " & ex.Message)
            statusCode = HttpStatusCode.InternalServerError
        End Try
    End Sub

    '==========================================================================
    ' MÉTODOS PARA MERCH
    '==========================================================================
    Sub uploadMerch(request As HttpListenerRequest, action As String, ByRef jsonResponse As String, ByRef statusCode As Integer, userId As Integer)
        Try
            Dim body As String
            Using reader As New StreamReader(request.InputStream, request.ContentEncoding)
                body = reader.ReadToEnd()
            End Using

            Dim merchData = JsonSerializer.Deserialize(Of Dictionary(Of String, JsonElement))(body)

            ' Validar campos requeridos
            If Not merchData.ContainsKey("title") OrElse Not merchData.ContainsKey("price") OrElse
               Not merchData.ContainsKey("cover") Then
                jsonResponse = GenerateErrorResponse("400", "Faltan campos requeridos")
                statusCode = HttpStatusCode.BadRequest
                Return
            End If

            Dim title As String = merchData("title").GetString()
            Dim description As String = If(merchData.ContainsKey("description"), merchData("description").GetString(), "")
            Dim price As Decimal = merchData("price").GetDecimal()
            Dim cover As String = merchData("cover").GetString()
            Dim releaseDate As String = If(merchData.ContainsKey("releaseDate"), merchData("releaseDate").GetString(), DateTime.Now.ToString("yyyy-MM-dd"))

            ' Validar que price sea positivo
            If price <= 0 Then
                jsonResponse = GenerateErrorResponse("400", "El precio debe ser un valor positivo")
                statusCode = HttpStatusCode.BadRequest
                Return
            End If

            ' Insertar merchandising
            Dim newMerchId As Integer
            Using cmd = db.CreateCommand("INSERT INTO merch (titulo, descripcion, cover, fechalanzamiento, precio) VALUES (@titulo, @descripcion, @cover, @fecha, @precio) RETURNING idmerch")
                cmd.Parameters.AddWithValue("@titulo", title)
                cmd.Parameters.AddWithValue("@descripcion", description)
                cmd.Parameters.AddWithValue("@cover", StringToBytes(cover))
                cmd.Parameters.AddWithValue("@fecha", Date.Parse(releaseDate))
                cmd.Parameters.AddWithValue("@precio", price)
                newMerchId = CInt(cmd.ExecuteScalar())
            End Using

            ' Obtener el ID del artista asociado al usuario autenticado
            Dim artistId As Integer? = GetArtistIdByUserId(userId)

            If Not artistId.HasValue Then
                jsonResponse = GenerateErrorResponse("403", "El usuario no tiene un artista asociado")
                statusCode = HttpStatusCode.Forbidden
                Return
            End If

            ' Insertar al artista principal (el usuario autenticado) - NO es colaborador (ft = false)
            Using cmd = db.CreateCommand("INSERT INTO AutoresMerch (idartista, idmerch, ft) VALUES (@idartista, @idmerch, @ft)")
                cmd.Parameters.AddWithValue("@idartista", artistId.Value)
                cmd.Parameters.AddWithValue("@idmerch", newMerchId)
                cmd.Parameters.AddWithValue("@ft", False) ' No es colaborador, es el artista principal
                cmd.ExecuteNonQuery()
            End Using

            ' Insertar colaboradores (artistas con ft = true)
            If merchData.ContainsKey("collaborators") Then
                For Each collabElement In merchData("collaborators").EnumerateArray()
                    Dim collabArtistId As Integer = collabElement.GetInt32()
                    Using cmd = db.CreateCommand("INSERT INTO AutoresMerch (idartista, idmerch, ft) VALUES (@idartista, @idmerch, @ft)")
                        cmd.Parameters.AddWithValue("@idartista", collabArtistId)
                        cmd.Parameters.AddWithValue("@idmerch", newMerchId)
                        cmd.Parameters.AddWithValue("@ft", True) ' Es colaborador
                        cmd.ExecuteNonQuery()
                    End Using
                Next
            End If

            jsonResponse = ConvertToJson(New Dictionary(Of String, Object) From {{"merchId", newMerchId}})
            statusCode = HttpStatusCode.OK

        Catch ex As Exception
            jsonResponse = GenerateErrorResponse("500", "Error al crear el merchandising: " & ex.Message)
            statusCode = HttpStatusCode.InternalServerError
        End Try
    End Sub

    Sub searchMerch(request As HttpListenerRequest, action As String, ByRef jsonResponse As String, ByRef statusCode As Integer)
        Try
            Dim query As String = request.QueryString("q")
            If String.IsNullOrEmpty(query) Then
                jsonResponse = GenerateErrorResponse("400", "Parámetro de búsqueda 'q' requerido")
                statusCode = HttpStatusCode.BadRequest
                Return
            End If

            Dim results As New List(Of Dictionary(Of String, Object))

            Using cmd = db.CreateCommand("SELECT idmerch FROM merch WHERE LOWER(titulo) LIKE LOWER(@query)")
                cmd.Parameters.AddWithValue("@query", "%" & query & "%")
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        results.Add(New Dictionary(Of String, Object) From {{"merchId", reader.GetInt32(0)}})
                    End While
                End Using
            End Using

            jsonResponse = ConvertToJson(results)
            statusCode = HttpStatusCode.OK

        Catch ex As Exception
            jsonResponse = GenerateErrorResponse("500", "Error al buscar merchandising: " & ex.Message)
            statusCode = HttpStatusCode.InternalServerError
        End Try
    End Sub

    Sub listMerch(request As HttpListenerRequest, action As String, ByRef jsonResponse As String, ByRef statusCode As Integer)
        Try
            ' Obtener parámetro de lista de IDs
            Dim idsParam As String = request.QueryString("ids")
            If String.IsNullOrEmpty(idsParam) Then
                jsonResponse = GenerateErrorResponse("400", "Parámetro 'ids' requerido")
                statusCode = HttpStatusCode.BadRequest
                Return
            End If

            ' Dividir los IDs por comas
            Dim idStrings As String() = idsParam.Split(","c)
            Dim merchIds As New List(Of Integer)

            ' Parsear y validar los IDs
            For Each idStr In idStrings
                Dim merchId As Integer
                If Integer.TryParse(idStr.Trim(), merchId) Then
                    merchIds.Add(merchId)
                Else
                    jsonResponse = GenerateErrorResponse("400", "ID inválido: " & idStr)
                    statusCode = HttpStatusCode.BadRequest
                    Return
                End If
            Next

            ' Obtener los datos de todos los merchandising
            Dim results As New List(Of Dictionary(Of String, Object))

            For Each merchId In merchIds
                Dim merchData As Dictionary(Of String, Object) = GetMerchData(merchId)
                If merchData IsNot Nothing Then
                    results.Add(merchData)
                End If
            Next

            jsonResponse = ConvertToJson(results)
            statusCode = HttpStatusCode.OK

        Catch ex As Exception
            jsonResponse = GenerateErrorResponse("500", "Error al obtener merchandising: " & ex.Message)
            statusCode = HttpStatusCode.InternalServerError
        End Try
    End Sub

    Sub filterMerch(request As HttpListenerRequest, action As String, ByRef jsonResponse As String, ByRef statusCode As Integer)
        Try
            ' Obtener parámetros de filtro
            Dim artistsParam As String = request.QueryString("artists")
            Dim orderParam As String = request.QueryString("order")
            Dim directionParam As String = request.QueryString("direction")
            Dim pageParam As String = request.QueryString("page")

            ' Límite fijo de 9 elementos por página
            Const pageLimit As Integer = 9

            ' Parsear página (por defecto 1)
            Dim pageNumber As Integer = 1
            If Not String.IsNullOrEmpty(pageParam) Then
                Integer.TryParse(pageParam, pageNumber)
                If pageNumber < 1 Then pageNumber = 1
            End If

            ' Calcular OFFSET
            Dim offset As Integer = (pageNumber - 1) * pageLimit

            ' Parsear artistas
            Dim artistIds As New List(Of Integer)
            If Not String.IsNullOrEmpty(artistsParam) Then
                For Each artistStr In artistsParam.Split(","c)
                    Dim artistId As Integer
                    If Integer.TryParse(artistStr.Trim(), artistId) Then
                        artistIds.Add(artistId)
                    End If
                Next
            End If

            ' Construir query SQL
            Dim orderField As String = "m.idmerch"

            ' Determinar campo de ordenamiento
            If Not String.IsNullOrEmpty(orderParam) Then
                If orderParam.ToLower() = "date" Then
                    orderField = "m.fechalanzamiento"
                ElseIf orderParam.ToLower() = "name" Then
                    orderField = "m.titulo"
                End If
            End If

            ' SELECT con el campo de ordenamiento para evitar error con DISTINCT
            Dim sqlQuery As String = ""
            If artistIds.Count > 0 Then
                sqlQuery = $"SELECT DISTINCT m.idmerch, {orderField} FROM merch m " &
                                        "INNER JOIN AutoresMerch am ON m.idmerch = am.idmerch " &
                                        "WHERE am.idartista IN (" & String.Join(",", artistIds) & ") "
            Else
                ' Sin filtro de artistas, devolver todos
                sqlQuery = $"SELECT m.idmerch, {orderField} FROM merch m "
            End If

            ' Agregar ORDER BY
            sqlQuery &= $"ORDER BY {orderField} "

            ' Dirección del ordenamiento
            If Not String.IsNullOrEmpty(directionParam) AndAlso directionParam.ToLower() = "desc" Then
                sqlQuery &= "DESC"
            Else
                sqlQuery &= "ASC"
            End If

            ' Añadir paginación solo si se especifica el parámetro page
            If Not String.IsNullOrEmpty(pageParam) Then
                sqlQuery &= $" LIMIT {pageLimit} OFFSET {offset}"
            End If

            ' Ejecutar query
            Dim results As New List(Of Integer)
            Using cmd = db.CreateCommand(sqlQuery)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        results.Add(reader.GetInt32(0))
                    End While
                End Using
            End Using

            jsonResponse = ConvertToJson(results)
            statusCode = HttpStatusCode.OK

        Catch ex As Exception
            jsonResponse = GenerateErrorResponse("500", "Error al filtrar merchandising: " & ex.Message)
            statusCode = HttpStatusCode.InternalServerError
        End Try
    End Sub

    ' Función auxiliar para obtener datos completos de merchandising
    Function GetMerchData(merchId As Integer) As Dictionary(Of String, Object)
        Try
            Dim schema As New Dictionary(Of String, Object) From {
                {"merchId", merchId},
                {"title", Nothing},
                {"artistId", Nothing},
                {"collaborators", Nothing},
                {"releaseDate", Nothing},
                {"description", Nothing},
                {"price", Nothing},
                {"cover", Nothing}
            }

            ' Recuperar datos del merchandising
            Using cmd = db.CreateCommand("SELECT titulo, descripcion, precio, cover, fechaLanzamiento FROM merch WHERE idmerch = @id")
                cmd.Parameters.AddWithValue("@id", merchId)
                Using reader = cmd.ExecuteReader()
                    If reader.HasRows Then
                        While reader.Read()
                            schema("title") = reader.GetString(0)
                            schema("description") = reader.GetString(1)
                            schema("price") = reader.GetDecimal(2).ToString()
                            Dim coverBytes As Byte() = CType(reader("cover"), Byte())
                            schema("cover") = Convert.ToBase64String(coverBytes)
                            schema("releaseDate") = reader.GetDateTime(4).ToString("yyyy-MM-dd")
                        End While
                    Else
                        Return Nothing
                    End If
                End Using
            End Using

            ' Recuperar artista creador y colaboradores
            Dim collaborators As New List(Of Integer)
            Using cmd = db.CreateCommand("SELECT idartista, ft FROM AutoresMerch WHERE idmerch = @id")
                cmd.Parameters.AddWithValue("@id", merchId)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        If reader.GetBoolean(1) = False Then
                            schema("artistId") = reader.GetInt32(0).ToString()
                        Else
                            collaborators.Add(reader.GetInt32(0))
                        End If
                    End While
                End Using
            End Using
            schema("collaborators") = collaborators

            Return schema

        Catch ex As Exception
            Console.WriteLine($"Error al obtener datos de merchandising {merchId}: {ex.Message}")
            Return Nothing
        End Try
    End Function

    Sub getMerch(request As HttpListenerRequest, action As String, ByRef jsonResponse As String, ByRef statusCode As Integer)
        Try
            If Not IsNumeric(action) Then
                jsonResponse = GenerateErrorResponse("400", "ID de merchandising inválido")
                statusCode = HttpStatusCode.BadRequest
                Return
            End If

            Dim schema As New Dictionary(Of String, Object) From {
                {"merchId", Nothing},
                {"title", Nothing},
                {"artistId", Nothing},
                {"collaborators", Nothing},
                {"releaseDate", Nothing},
                {"description", Nothing},
                {"price", Nothing},
                {"cover", Nothing}
            }

            ' Recuperar datos del merchandising
            Using cmd = db.CreateCommand("SELECT titulo, descripcion, precio, cover, fechaLanzamiento FROM merch WHERE idmerch = @id")
                cmd.Parameters.AddWithValue("@id", Integer.Parse(action))
                Using reader = cmd.ExecuteReader()
                    If reader.HasRows Then
                        While reader.Read()
                            schema("merchId") = action
                            schema("title") = reader.GetString(0)
                            schema("description") = reader.GetString(1)
                            schema("price") = reader.GetDecimal(2).ToString()
                            Dim coverBytes As Byte() = CType(reader("cover"), Byte())
                            schema("cover") = Convert.ToBase64String(coverBytes)
                            schema("releaseDate") = reader.GetDateTime(4).ToString("yyyy-MM-dd")
                        End While
                    Else
                        jsonResponse = ""
                        statusCode = HttpStatusCode.NotFound
                        Return
                    End If
                End Using
            End Using

            ' Recuperar artista creador y colaboradores
            Dim collaborators As New List(Of Integer)
            Using cmd = db.CreateCommand("SELECT idartista, ft FROM AutoresMerch WHERE idmerch = @id")
                cmd.Parameters.AddWithValue("@id", Integer.Parse(action))
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        If reader.GetBoolean(1) = False Then
                            ' ft = false: artista creador
                            schema("artistId") = reader.GetInt32(0).ToString()
                        Else
                            ' ft = true: colaborador
                            collaborators.Add(reader.GetInt32(0))
                        End If
                    End While
                End Using
            End Using
            schema("collaborators") = collaborators

            jsonResponse = ConvertToJson(schema)
            statusCode = HttpStatusCode.OK

        Catch ex As Exception
            jsonResponse = GenerateErrorResponse("500", "Error al obtener el merchandising: " & ex.Message)
            statusCode = HttpStatusCode.InternalServerError
        End Try
    End Sub

    Sub deleteMerch(request As HttpListenerRequest, action As String, ByRef jsonResponse As String, ByRef statusCode As Integer, userId As Integer)
        Try
            If Not IsNumeric(action) Then
                jsonResponse = GenerateErrorResponse("400", "ID de merchandising inválido")
                statusCode = HttpStatusCode.BadRequest
                Return
            End If

            Dim merchId As Integer = Integer.Parse(action)

            ' Eliminar merchandising (las relaciones se eliminan en cascada)
            Using cmd = db.CreateCommand("DELETE FROM merch WHERE idmerch = @id")
                cmd.Parameters.AddWithValue("@id", merchId)
                Dim rowsAffected As Integer = cmd.ExecuteNonQuery()

                If rowsAffected = 0 Then
                    jsonResponse = GenerateErrorResponse("404", "Merchandising no encontrado")
                    statusCode = HttpStatusCode.NotFound
                Else
                    jsonResponse = ""
                    statusCode = HttpStatusCode.OK
                End If
            End Using

        Catch ex As Exception
            jsonResponse = GenerateErrorResponse("500", "Error al eliminar el merchandising: " & ex.Message)
            statusCode = HttpStatusCode.InternalServerError
        End Try
    End Sub

    Sub updateMerch(request As HttpListenerRequest, action As String, ByRef jsonResponse As String, ByRef statusCode As Integer, userId As Integer)
        Try
            If Not IsNumeric(action) Then
                jsonResponse = GenerateErrorResponse("400", "ID de merchandising inválido")
                statusCode = HttpStatusCode.BadRequest
                Return
            End If

            Dim merchId As Integer = Integer.Parse(action)

            Dim body As String
            Using reader As New StreamReader(request.InputStream, request.ContentEncoding)
                body = reader.ReadToEnd()
            End Using

            Dim merchData = JsonSerializer.Deserialize(Of Dictionary(Of String, JsonElement))(body)

            ' Validar price si está presente
            If merchData.ContainsKey("price") Then
                Dim price As Decimal = merchData("price").GetDecimal()
                If price <= 0 Then
                    jsonResponse = GenerateErrorResponse("400", "El precio debe ser un valor positivo")
                    statusCode = HttpStatusCode.BadRequest
                    Return
                End If
            End If

            ' Construir UPDATE dinámico
            Dim updates As New List(Of String)
            Dim cmdText As String = "UPDATE merch SET "

            Using cmd = db.CreateCommand("")
                If merchData.ContainsKey("title") Then
                    updates.Add("titulo = @titulo")
                    cmd.Parameters.AddWithValue("@titulo", merchData("title").GetString())
                End If
                If merchData.ContainsKey("description") Then
                    updates.Add("descripcion = @descripcion")
                    cmd.Parameters.AddWithValue("@descripcion", merchData("description").GetString())
                End If
                If merchData.ContainsKey("price") Then
                    updates.Add("precio = @precio")
                    cmd.Parameters.AddWithValue("@precio", merchData("price").GetDecimal())
                End If
                If merchData.ContainsKey("cover") Then
                    updates.Add("cover = @cover")
                    cmd.Parameters.AddWithValue("@cover", StringToBytes(merchData("cover").GetString()))
                End If
                If merchData.ContainsKey("releaseDate") Then
                    updates.Add("fechalanzamiento = @fecha")
                    cmd.Parameters.AddWithValue("@fecha", Date.Parse(merchData("releaseDate").GetString()))
                End If

                If updates.Count > 0 Then
                    cmd.CommandText = cmdText & String.Join(", ", updates) & " WHERE idmerch = @id"
                    cmd.Parameters.AddWithValue("@id", merchId)
                    cmd.ExecuteNonQuery()
                End If
            End Using

            ' Actualizar colaboradores si están presentes
            If merchData.ContainsKey("collaborators") Then
                ' Eliminar solo los colaboradores (ft = true), mantener el artista principal (ft = false)
                Using cmd = db.CreateCommand("DELETE FROM AutoresMerch WHERE idmerch = @id AND ft = true")
                    cmd.Parameters.AddWithValue("@id", merchId)
                    cmd.ExecuteNonQuery()
                End Using

                ' Insertar nuevos colaboradores
                For Each collabElement In merchData("collaborators").EnumerateArray()
                    Using cmd = db.CreateCommand("INSERT INTO AutoresMerch (idartista, idmerch, ft) VALUES (@idartista, @idmerch, @ft)")
                        cmd.Parameters.AddWithValue("@idartista", collabElement.GetInt32())
                        cmd.Parameters.AddWithValue("@idmerch", merchId)
                        cmd.Parameters.AddWithValue("@ft", True) ' Es colaborador
                        cmd.ExecuteNonQuery()
                    End Using
                Next
            End If

            jsonResponse = ""
            statusCode = HttpStatusCode.OK

        Catch ex As Exception
            jsonResponse = GenerateErrorResponse("500", "Error al actualizar el merchandising: " & ex.Message)
            statusCode = HttpStatusCode.InternalServerError
        End Try
    End Sub

    '==========================================================================
    ' MÉTODOS PARA ARTIST
    '==========================================================================
    Sub uploadArtist(request As HttpListenerRequest, action As String, ByRef jsonResponse As String, ByRef statusCode As Integer, userId As Integer)
        Try
            Dim body As String
            Using reader As New StreamReader(request.InputStream, request.ContentEncoding)
                body = reader.ReadToEnd()
            End Using

            Dim artistData = JsonSerializer.Deserialize(Of Dictionary(Of String, JsonElement))(body)

            ' Validar campos requeridos (userId ya no es necesario en el body)
            If Not artistData.ContainsKey("artisticName") OrElse Not artistData.ContainsKey("artisticEmail") Then
                jsonResponse = GenerateErrorResponse("400", "Faltan campos requeridos")
                statusCode = HttpStatusCode.BadRequest
                Return
            End If

            Dim artisticName As String = artistData("artisticName").GetString()
            Dim artisticEmail As String = artistData("artisticEmail").GetString()
            ' userId ya viene como parámetro de la función (del token de autenticación)
            Dim biography As String = If(artistData.ContainsKey("artisticBiography"), artistData("artisticBiography").GetString(), "")
            Dim image As String = If(artistData.ContainsKey("artisticImage"), artistData("artisticImage").GetString(), Nothing)
            Dim socialMediaUrl As String = If(artistData.ContainsKey("socialMediaUrl"), artistData("socialMediaUrl").GetString(), Nothing)
            Dim registrationDate As String = DateTime.Now.ToString("yyyy-MM-dd")

            ' Insertar artista
            Dim newArtistId As Integer
            Using cmd = db.CreateCommand("INSERT INTO artistas (nombre, imagen, bio, fechainicio, email, socialmediaurl, userid) VALUES (@nombre, @imagen, @bio, @fecha, @email, @socialmediaurl, @userid) RETURNING idartista")
                cmd.Parameters.AddWithValue("@nombre", artisticName)
                cmd.Parameters.AddWithValue("@imagen", If(image IsNot Nothing, StringToBytes(image), DBNull.Value))
                cmd.Parameters.AddWithValue("@bio", biography)
                cmd.Parameters.AddWithValue("@fecha", Date.Parse(registrationDate))
                cmd.Parameters.AddWithValue("@email", artisticEmail)
                cmd.Parameters.AddWithValue("@socialmediaurl", If(socialMediaUrl, DBNull.Value))
                cmd.Parameters.AddWithValue("@userid", userId)
                newArtistId = CInt(cmd.ExecuteScalar())
            End Using

            jsonResponse = ConvertToJson(New Dictionary(Of String, Object) From {{"artistId", newArtistId}})
            statusCode = HttpStatusCode.OK

        Catch ex As Exception
            jsonResponse = GenerateErrorResponse("500", "Error al crear el artista: " & ex.Message)
            statusCode = HttpStatusCode.InternalServerError
        End Try
    End Sub

    Sub searchArtist(request As HttpListenerRequest, action As String, ByRef jsonResponse As String, ByRef statusCode As Integer)
        Try
            Dim query As String = request.QueryString("q")
            If String.IsNullOrEmpty(query) Then
                jsonResponse = GenerateErrorResponse("400", "Parámetro de búsqueda 'q' requerido")
                statusCode = HttpStatusCode.BadRequest
                Return
            End If

            Dim results As New List(Of Dictionary(Of String, Object))

            Using cmd = db.CreateCommand("SELECT idartista FROM artistas WHERE LOWER(nombre) LIKE LOWER(@query)")
                cmd.Parameters.AddWithValue("@query", "%" & query & "%")
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        results.Add(New Dictionary(Of String, Object) From {{"artistId", reader.GetInt32(0)}})
                    End While
                End Using
            End Using

            jsonResponse = ConvertToJson(results)
            statusCode = HttpStatusCode.OK

        Catch ex As Exception
            jsonResponse = GenerateErrorResponse("500", "Error al buscar artistas: " & ex.Message)
            statusCode = HttpStatusCode.InternalServerError
        End Try
    End Sub

    Sub listArtists(request As HttpListenerRequest, action As String, ByRef jsonResponse As String, ByRef statusCode As Integer)
        Try
            ' Obtener parámetro de lista de IDs
            Dim idsParam As String = request.QueryString("ids")
            If String.IsNullOrEmpty(idsParam) Then
                jsonResponse = GenerateErrorResponse("400", "Parámetro 'ids' requerido")
                statusCode = HttpStatusCode.BadRequest
                Return
            End If

            ' Dividir los IDs por comas
            Dim idStrings As String() = idsParam.Split(","c)
            Dim artistIds As New List(Of Integer)

            ' Parsear y validar los IDs
            For Each idStr In idStrings
                Dim artistId As Integer
                If Integer.TryParse(idStr.Trim(), artistId) Then
                    artistIds.Add(artistId)
                Else
                    jsonResponse = GenerateErrorResponse("400", "ID inválido: " & idStr)
                    statusCode = HttpStatusCode.BadRequest
                    Return
                End If
            Next

            ' Obtener los datos de todos los artistas
            Dim results As New List(Of Dictionary(Of String, Object))

            For Each artistId In artistIds
                Dim artistData As Dictionary(Of String, Object) = GetArtistData(artistId)
                If artistData IsNot Nothing Then
                    results.Add(artistData)
                End If
            Next

            jsonResponse = ConvertToJson(results)
            statusCode = HttpStatusCode.OK

        Catch ex As Exception
            jsonResponse = GenerateErrorResponse("500", "Error al obtener artistas: " & ex.Message)
            statusCode = HttpStatusCode.InternalServerError
        End Try
    End Sub

    Sub filterArtists(request As HttpListenerRequest, action As String, ByRef jsonResponse As String, ByRef statusCode As Integer)
        Try
            ' Obtener parámetros de filtro
            Dim genresParam As String = request.QueryString("genres")
            Dim orderParam As String = request.QueryString("order")
            Dim directionParam As String = request.QueryString("direction")
            Dim pageParam As String = request.QueryString("page")

            ' Límite fijo de 9 elementos por página
            Const pageLimit As Integer = 9

            ' Parsear página (por defecto 1)
            Dim pageNumber As Integer = 1
            If Not String.IsNullOrEmpty(pageParam) Then
                Integer.TryParse(pageParam, pageNumber)
                If pageNumber < 1 Then pageNumber = 1
            End If

            ' Calcular OFFSET
            Dim offset As Integer = (pageNumber - 1) * pageLimit

            ' Parsear géneros
            Dim genreIds As New List(Of Integer)
            If Not String.IsNullOrEmpty(genresParam) Then
                For Each genreStr In genresParam.Split(","c)
                    Dim genreId As Integer
                    If Integer.TryParse(genreStr.Trim(), genreId) Then
                        genreIds.Add(genreId)
                    End If
                Next
            End If

            ' Construir query SQL - buscar artistas que tengan canciones con esos géneros
            Dim orderField As String = "a.idartista"

            ' Determinar campo de ordenamiento
            If Not String.IsNullOrEmpty(orderParam) Then
                If orderParam.ToLower() = "date" Then
                    orderField = "a.fechainicio"
                ElseIf orderParam.ToLower() = "name" Then
                    orderField = "a.nombre"
                End If
            End If

            ' SELECT con el campo de ordenamiento para evitar error con DISTINCT
            Dim sqlQuery As String = ""
            If genreIds.Count > 0 Then
                sqlQuery = $"SELECT DISTINCT a.idartista, {orderField} FROM artistas a " &
                                        "INNER JOIN autorescanciones ac ON a.idartista = ac.idartista " &
                                        "INNER JOIN generoscanciones gc ON ac.idcancion = gc.idcancion " &
                                        "WHERE gc.idgenero IN (" & String.Join(",", genreIds) & ") "
            Else
                ' Sin filtro de géneros, devolver todos los artistas
                sqlQuery = $"SELECT a.idartista, {orderField} FROM artistas a "
            End If

            ' Agregar ORDER BY
            sqlQuery &= $"ORDER BY {orderField} "

            ' Dirección del ordenamiento
            If Not String.IsNullOrEmpty(directionParam) AndAlso directionParam.ToLower() = "desc" Then
                sqlQuery &= "DESC"
            Else
                sqlQuery &= "ASC"
            End If

            ' Añadir paginación solo si se especifica el parámetro page
            If Not String.IsNullOrEmpty(pageParam) Then
                sqlQuery &= $" LIMIT {pageLimit} OFFSET {offset}"
            End If

            ' Ejecutar query
            Dim results As New List(Of Integer)
            Using cmd = db.CreateCommand(sqlQuery)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        results.Add(reader.GetInt32(0))
                    End While
                End Using
            End Using

            jsonResponse = ConvertToJson(results)
            statusCode = HttpStatusCode.OK

        Catch ex As Exception
            jsonResponse = GenerateErrorResponse("500", "Error al filtrar artistas: " & ex.Message)
            statusCode = HttpStatusCode.InternalServerError
        End Try
    End Sub

    ' Función auxiliar para obtener datos completos de un artista
    Function GetArtistData(artistId As Integer) As Dictionary(Of String, Object)
        Try
            Dim schema As New Dictionary(Of String, Object) From {
                {"artistId", artistId},
                {"artisticName", Nothing},
                {"artisticBiography", Nothing},
                {"artisticEmail", Nothing},
                {"artisticImage", Nothing},
                {"socialMediaUrl", Nothing},
                {"registrationDate", Nothing},
                {"userId", Nothing},
                {"owner_songs", New List(Of Integer)},
                {"owner_albums", New List(Of Integer)},
                {"owner_merch", New List(Of Integer)}
            }

            ' Recuperar datos del artista
            Using cmd = db.CreateCommand("SELECT nombre, imagen, bio, fechainicio, email, socialmediaurl, userid FROM artistas WHERE idartista = @id")
                cmd.Parameters.AddWithValue("@id", artistId)
                Using reader = cmd.ExecuteReader()
                    If reader.HasRows Then
                        While reader.Read()
                            schema("artisticName") = reader.GetString(0)
                            Dim imagenBytes As Byte() = CType(reader("imagen"), Byte())
                            schema("artisticImage") = Convert.ToBase64String(imagenBytes)
                            schema("artisticBiography") = reader.GetString(2)
                            schema("registrationDate") = reader.GetDateTime(3).ToString("yyyy-MM-dd")
                            schema("artisticEmail") = If(reader.IsDBNull(4), Nothing, reader.GetString(4))
                            schema("socialMediaUrl") = If(reader.IsDBNull(5), Nothing, reader.GetString(5))
                            schema("userId") = If(reader.IsDBNull(6), Nothing, CType(reader.GetInt32(6), Object))
                        End While
                    Else
                        Return Nothing
                    End If
                End Using
            End Using

            ' Obtener canciones donde el artista es creador principal (ft=false)
            Dim ownerSongs As New List(Of Integer)
            Using cmd = db.CreateCommand("SELECT idcancion FROM autorescanciones WHERE idartista = @id AND ft = false")
                cmd.Parameters.AddWithValue("@id", artistId)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        ownerSongs.Add(reader.GetInt32(0))
                    End While
                End Using
            End Using
            schema("owner_songs") = ownerSongs

            ' Obtener álbumes donde el artista es creador principal (ft=false)
            Dim ownerAlbums As New List(Of Integer)
            Using cmd = db.CreateCommand("SELECT idalbum FROM autoresalbumes WHERE idartista = @id AND ft = false")
                cmd.Parameters.AddWithValue("@id", artistId)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        ownerAlbums.Add(reader.GetInt32(0))
                    End While
                End Using
            End Using
            schema("owner_albums") = ownerAlbums

            ' Obtener merchandising asociado al artista
            Dim ownerMerch As New List(Of Integer)
            Using cmd = db.CreateCommand("SELECT idmerch FROM AutoresMerch WHERE idartista = @id")
                cmd.Parameters.AddWithValue("@id", artistId)
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        ownerMerch.Add(reader.GetInt32(0))
                    End While
                End Using
            End Using
            schema("owner_merch") = ownerMerch

            Return schema

        Catch ex As Exception
            Console.WriteLine($"Error al obtener datos de artista {artistId}: {ex.Message}")
            Return Nothing
        End Try
    End Function

    Sub getArtist(request As HttpListenerRequest, action As String, ByRef jsonResponse As String, ByRef statusCode As Integer)
        Try
            If Not IsNumeric(action) Then
                jsonResponse = GenerateErrorResponse("400", "ID de artista inválido")
                statusCode = HttpStatusCode.BadRequest
                Return
            End If

            Dim schema As New Dictionary(Of String, Object) From {
                {"artistId", Nothing},
                {"artisticName", Nothing},
                {"artisticBiography", Nothing},
                {"artisticEmail", Nothing},
                {"artisticImage", Nothing},
                {"socialMediaUrl", Nothing},
                {"registrationDate", Nothing},
                {"userId", Nothing},
                {"owner_songs", New List(Of Integer)},
                {"owner_albums", New List(Of Integer)},
                {"owner_merch", New List(Of Integer)}
            }

            ' Recuperar datos del artista
            Using cmd = db.CreateCommand("SELECT nombre, imagen, bio, fechainicio, email, socialmediaurl, userid FROM artistas WHERE idartista = @id")
                cmd.Parameters.AddWithValue("@id", Integer.Parse(action))
                Using reader = cmd.ExecuteReader()
                    If reader.HasRows Then
                        While reader.Read()
                            schema("artistId") = action
                            schema("artisticName") = reader.GetString(0)
                            Dim imagenBytes As Byte() = CType(reader("imagen"), Byte())
                            schema("artisticImage") = Convert.ToBase64String(imagenBytes)
                            schema("artisticBiography") = reader.GetString(2)
                            schema("registrationDate") = reader.GetDateTime(3).ToString("yyyy-MM-dd")
                            schema("artisticEmail") = If(reader.IsDBNull(4), Nothing, reader.GetString(4))
                            schema("socialMediaUrl") = If(reader.IsDBNull(5), Nothing, reader.GetString(5))
                            schema("userId") = If(reader.IsDBNull(6), Nothing, CType(reader.GetInt32(6), Object))
                        End While
                    Else
                        jsonResponse = ""
                        statusCode = HttpStatusCode.NotFound
                        Return
                    End If
                End Using
            End Using

            ' Obtener canciones donde el artista es creador principal (ft=false)
            Dim ownerSongs As New List(Of Integer)
            Using cmd = db.CreateCommand("SELECT idcancion FROM autorescanciones WHERE idartista = @id AND ft = false")
                cmd.Parameters.AddWithValue("@id", Integer.Parse(action))
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        ownerSongs.Add(reader.GetInt32(0))
                    End While
                End Using
            End Using
            schema("owner_songs") = ownerSongs

            ' Obtener álbumes donde el artista es creador principal (ft=false)
            Dim ownerAlbums As New List(Of Integer)
            Using cmd = db.CreateCommand("SELECT idalbum FROM autoresalbumes WHERE idartista = @id AND ft = false")
                cmd.Parameters.AddWithValue("@id", Integer.Parse(action))
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        ownerAlbums.Add(reader.GetInt32(0))
                    End While
                End Using
            End Using
            schema("owner_albums") = ownerAlbums

            ' Obtener merchandising asociado al artista
            Dim ownerMerch As New List(Of Integer)
            Using cmd = db.CreateCommand("SELECT idmerch FROM AutoresMerch WHERE idartista = @id")
                cmd.Parameters.AddWithValue("@id", Integer.Parse(action))
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        ownerMerch.Add(reader.GetInt32(0))
                    End While
                End Using
            End Using
            schema("owner_merch") = ownerMerch

            jsonResponse = ConvertToJson(schema)
            statusCode = HttpStatusCode.OK

        Catch ex As Exception
            jsonResponse = GenerateErrorResponse("500", "Error al obtener el artista: " & ex.Message)
            statusCode = HttpStatusCode.InternalServerError
        End Try
    End Sub

    Sub deleteArtist(request As HttpListenerRequest, action As String, ByRef jsonResponse As String, ByRef statusCode As Integer, userId As Integer)
        Try
            If Not IsNumeric(action) Then
                jsonResponse = GenerateErrorResponse("400", "ID de artista inválido")
                statusCode = HttpStatusCode.BadRequest
                Return
            End If

            Dim artistId As Integer = Integer.Parse(action)

            ' Eliminar artista (las relaciones se eliminan en cascada)
            Using cmd = db.CreateCommand("DELETE FROM artistas WHERE idartista = @id")
                cmd.Parameters.AddWithValue("@id", artistId)
                Dim rowsAffected As Integer = cmd.ExecuteNonQuery()

                If rowsAffected = 0 Then
                    jsonResponse = GenerateErrorResponse("404", "Artista no encontrado")
                    statusCode = HttpStatusCode.NotFound
                Else
                    jsonResponse = ""
                    statusCode = HttpStatusCode.OK
                End If
            End Using

        Catch ex As Exception
            jsonResponse = GenerateErrorResponse("500", "Error al eliminar el artista: " & ex.Message)
            statusCode = HttpStatusCode.InternalServerError
        End Try
    End Sub

    Sub updateArtist(request As HttpListenerRequest, action As String, ByRef jsonResponse As String, ByRef statusCode As Integer, userId As Integer)
        Try
            If Not IsNumeric(action) Then
                jsonResponse = GenerateErrorResponse("400", "ID de artista inválido")
                statusCode = HttpStatusCode.BadRequest
                Return
            End If

            Dim artistId As Integer = Integer.Parse(action)

            Dim body As String
            Using reader As New StreamReader(request.InputStream, request.ContentEncoding)
                body = reader.ReadToEnd()
            End Using

            Dim artistData = JsonSerializer.Deserialize(Of Dictionary(Of String, JsonElement))(body)

            ' Construir UPDATE dinámico
            Dim updates As New List(Of String)
            Dim cmdText As String = "UPDATE artistas SET "

            Using cmd = db.CreateCommand("")
                If artistData.ContainsKey("artisticName") Then
                    updates.Add("nombre = @nombre")
                    cmd.Parameters.AddWithValue("@nombre", artistData("artisticName").GetString())
                End If
                If artistData.ContainsKey("artisticImage") Then
                    updates.Add("imagen = @imagen")
                    cmd.Parameters.AddWithValue("@imagen", StringToBytes(artistData("artisticImage").GetString()))
                End If
                If artistData.ContainsKey("artisticBiography") Then
                    updates.Add("bio = @bio")
                    cmd.Parameters.AddWithValue("@bio", artistData("artisticBiography").GetString())
                End If
                If artistData.ContainsKey("artisticEmail") Then
                    updates.Add("email = @email")
                    cmd.Parameters.AddWithValue("@email", artistData("artisticEmail").GetString())
                End If
                If artistData.ContainsKey("socialMediaUrl") Then
                    updates.Add("socialmediaurl = @socialmediaurl")
                    cmd.Parameters.AddWithValue("@socialmediaurl", If(artistData("socialMediaUrl").ValueKind = JsonValueKind.Null, DBNull.Value, CType(artistData("socialMediaUrl").GetString(), Object)))
                End If
                If artistData.ContainsKey("userId") Then
                    updates.Add("userid = @userid")
                    cmd.Parameters.AddWithValue("@userid", If(artistData("userId").ValueKind = JsonValueKind.Null, DBNull.Value, CType(artistData("userId").GetInt32(), Object)))
                End If

                If updates.Count > 0 Then
                    cmd.CommandText = cmdText & String.Join(", ", updates) & " WHERE idartista = @id"
                    cmd.Parameters.AddWithValue("@id", artistId)
                    cmd.ExecuteNonQuery()
                End If
            End Using

            jsonResponse = ""
            statusCode = HttpStatusCode.OK

        Catch ex As Exception
            jsonResponse = GenerateErrorResponse("500", "Error al actualizar el artista: " & ex.Message)
            statusCode = HttpStatusCode.InternalServerError
        End Try
    End Sub

    ' Obtener todos los géneros
    Sub getGenres(request As HttpListenerRequest, action As String, ByRef jsonResponse As String, ByRef statusCode As HttpStatusCode)
        Try
            Dim genresList As New List(Of Dictionary(Of String, Object))

            Using conn As New NpgsqlConnection(connectionString)
                conn.Open()
                Using cmd As New NpgsqlCommand("SELECT idgenero, nombre FROM generos ORDER BY nombre", conn)
                    Using reader As NpgsqlDataReader = cmd.ExecuteReader()
                        While reader.Read()
                            Dim genre As New Dictionary(Of String, Object) From {
                                {"id", reader.GetInt32(0)},
                                {"name", reader.GetString(1)}
                            }
                            genresList.Add(genre)
                        End While
                    End Using
                End Using
            End Using

            jsonResponse = ConvertToJson(genresList)
            statusCode = HttpStatusCode.OK

        Catch ex As Exception
            jsonResponse = GenerateErrorResponse("500", "Error al obtener los géneros: " & ex.Message)
            statusCode = HttpStatusCode.InternalServerError
        End Try
    End Sub

    ' ==========================================================================
    ' FUNCIONES HELPER PARA CONVERSIÓN DE IMÁGENES
    ' ==========================================================================
    
    ''' <summary>
    ''' Convierte una cadena a bytes. Soporta base64 puro o data URI completo.
    ''' Si la conversión base64 falla, convierte como texto UTF8.
    ''' </summary>
    ''' <param name="input">Cadena a convertir (puede incluir prefijo data:image/...;base64,)</param>
    ''' <returns>Array de bytes</returns>
    Function StringToBytes(input As String) As Byte()
        If String.IsNullOrEmpty(input) Then
            Return New Byte() {}
        End If

        Try
            ' Si tiene el prefijo data:image, extraer solo la parte base64
            Dim base64String As String = input
            If input.Contains(",") Then
                ' Formato: data:image/png;base64,iVBORw0KGgo...
                base64String = input.Substring(input.IndexOf(",") + 1)
            End If

            ' Intentar convertir desde base64
            Return Convert.FromBase64String(base64String)
        Catch ex As FormatException
            ' Si falla, convertir como texto UTF8
            Return Encoding.UTF8.GetBytes(input)
        End Try
    End Function

    ''' <summary>
    ''' Convierte bytes a cadena base64 con prefijo data URI.
    ''' </summary>
    ''' <param name="bytes">Array de bytes a convertir</param>
    ''' <returns>Cadena en formato data:image/png;base64,...</returns>
    Function BytesToString(bytes As Byte()) As String
        If bytes Is Nothing OrElse bytes.Length = 0 Then
            Return ""
        End If
        Return "data:image/png;base64," & Convert.ToBase64String(bytes)
    End Function

End Module
