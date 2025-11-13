
DROP TABLE IF EXISTS Canciones CASCADE;
DROP TABLE IF EXISTS Albumes CASCADE;
DROP TABLE IF EXISTS Merch CASCADE;
DROP TABLE IF EXISTS Artistas CASCADE;
DROP TABLE IF EXISTS AutoresAlbumes CASCADE;
DROP TABLE IF EXISTS AutoresCanciones CASCADE;
DROP TABLE IF EXISTS AutoresMerch CASCADE;
DROP TABLE IF EXISTS Generos CASCADE;
DROP TABLE IF EXISTS Idiomas CASCADE;
DROP TABLE IF EXISTS GenerosCanciones CASCADE;
DROP TABLE IF EXISTS IdiomasCanciones CASCADE;
DROP TABLE IF EXISTS CancionesAlbumes CASCADE;

CREATE TABLE Canciones (
    idCancion INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    titulo VARCHAR(255) NOT NULL,
	descripcion VARCHAR(512),
	cover VARCHAR(255) NOT NULL,
	track INT NOT NULL,
	duracion INT NOT NULL,
	fechaLanzamiento DATE NOT NULL,
	precio NUMERIC(10,2) NOT NULL,
	albumog INT
);

CREATE TABLE Albumes (
	idAlbum INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
	titulo VARCHAR(255) NOT NULL,
	descripcion VARCHAR(512),
	cover VARCHAR(255) NOT NULL,
	fechaLanzamiento DATE NOT NULL,
	precio NUMERIC(10,2) NOT NULL,
	precioAuto BOOL NOT NULL DEFAULT true
);

CREATE TABLE Merch (
	idMerch INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
	titulo VARCHAR(255) NOT NULL,
	descripcion VARCHAR(512) NOT NULL,
	cover VARCHAR(255) NOT NULL,
	fechaLanzamiento DATE NOT NULL,
	precio NUMERIC(10,2) NOT NULL
);

CREATE TABLE Artistas (
	idArtista INT  GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
	nombre VARCHAR(255) NOT NULL,
	imagen VARCHAR(255) NOT NULL,
	bio VARCHAR(1024) NOT NULL DEFAULT '',
	fechaInicio DATE NOT NULL,
	email VARCHAR(255),
	socialMediaUrl VARCHAR(512),
	userId INT
);

CREATE TABLE AutoresAlbumes (
	idArtista INT,
	idAlbum INT,
	ft BOOL NOT NULL,
	PRIMARY KEY (idArtista, idAlbum),
	FOREIGN KEY (idArtista) REFERENCES Artistas(idArtista) ON UPDATE CASCADE ON DELETE CASCADE,
	FOREIGN KEY (idAlbum) REFERENCES Albumes(idAlbum) ON UPDATE CASCADE ON DELETE CASCADE
);

CREATE TABLE AutoresCanciones (
	idArtista INT,
	idCancion INT,
	ft BOOL NOT NULL,
	PRIMARY KEY (idArtista, idCancion),
	FOREIGN KEY (idArtista) REFERENCES Artistas(idArtista) ON UPDATE CASCADE ON DELETE CASCADE,
	FOREIGN KEY (idCancion) REFERENCES Canciones(idCancion) ON UPDATE CASCADE ON DELETE CASCADE
);

CREATE TABLE AutoresMerch (
	idArtista INT,
	idMerch INT,
	ft BOOL NOT NULL,
	PRIMARY KEY (idArtista, idMerch),
	FOREIGN KEY (idArtista) REFERENCES Artistas(idArtista) ON UPDATE CASCADE ON DELETE CASCADE,
	FOREIGN KEY (idMerch) REFERENCES Merch(idMerch) ON UPDATE CASCADE ON DELETE CASCADE
);

CREATE TABLE CancionesAlbumes (
	idCancion INT,
	idAlbum INT,
	trackNumber INT NOT NULL,
	PRIMARY KEY (idCancion, idAlbum),
	FOREIGN KEY (idCancion) REFERENCES Canciones(idCancion) ON UPDATE CASCADE ON DELETE CASCADE,
	FOREIGN KEY (idAlbum) REFERENCES Albumes(idAlbum) ON UPDATE CASCADE ON DELETE CASCADE
);

CREATE TABLE Generos (
	idGenero INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
	nombre VARCHAR(32) NOT NULL
);

CREATE TABLE Idiomas (
	idIdioma INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
	nombre VARCHAR(16) NOT NULL
);

CREATE TABLE GenerosCanciones (
	idCancion INT,
	idGenero INT,
	PRIMARY KEY (idCancion, idGenero),
	FOREIGN KEY (idCancion) REFERENCES Canciones(idCancion) ON UPDATE CASCADE ON DELETE CASCADE,
	FOREIGN KEY (idGenero) REFERENCES Generos(idGenero) ON UPDATE CASCADE ON DELETE CASCADE
);

CREATE TABLE IdiomasCanciones (
	idCancion INT,
	idIdioma INT,
	PRIMARY KEY (idCancion, idIdioma),
	FOREIGN KEY (idCancion) REFERENCES Canciones(idCancion) ON UPDATE CASCADE ON DELETE CASCADE,
	FOREIGN KEY (idIdioma) REFERENCES Idiomas(idIdioma) ON UPDATE CASCADE ON DELETE CASCADE
);

-- Datos de prueba: Géneros
INSERT INTO Generos (nombre) VALUES ('Pop');
INSERT INTO Generos (nombre) VALUES ('Rock');

-- INSERT INTO Idiomas VALUES (0, 'ESPAÑOL');
-- INSERT INTO Generos VALUES (null, 'ROCK');
-- INSERT INTO Generos VALUES (null, 'POP');
-- INSERT INTO Albumes VALUES (0, 'Yo, minoría absoluta', 'https://cover.com', CURRENT_DATE, 10.00, true);
-- INSERT INTO Canciones VALUES (0, 'La vereda de la puerta de atrás', 0, 0, 0, CURRENT_DATE, 2.00);
-- INSERT INTO Artistas VALUES (0, 'Extremoduro', 'https://imagen.com', 'Mi biografía', CURRENT_DATE);
-- INSERT INTO GenerosCanciones VALUES (0, 0);
-- INSERT INTO IdiomasCanciones VALUES (0, 0);
-- INSERT INTO AutoresCanciones VALUES (0, 0, false);
-- INSERT INTO AutoresAlbumes VALUES (0, 0, false);
-- INSERT INTO CancionesAlbumes VALUES (0, 0, 1);

-- SELECT * FROM Idiomas;
-- SELECT * FROM Generos;
-- SELECT * FROM Albumes;
-- SELECT * FROM Canciones;
-- SELECT * FROM Artistas;
-- SELECT * FROM GenerosCanciones;
-- SELECT * FROM IdiomasCanciones;
-- SELECT * FROM AutoresCanciones;
-- SELECT * FROM AutoresAlbumes;
-- SELECT * FROM CancionesAlbumes;

-- DELETE FROM Idiomas;
-- DELETE FROM Generos;
-- DELETE FROM Albumes;
-- DELETE FROM Canciones;
-- DELETE FROM Artistas;
-- DELETE FROM GenerosCanciones;
-- DELETE FROM IdiomasCanciones;
-- DELETE FROM AutoresCanciones;
-- DELETE FROM AutoresAlbumes;
-- DELETE FROM CancionesAlbumes;

-- DROP TABLE IF EXISTS Canciones CASCADE;
-- DROP TABLE IF EXISTS Albumes CASCADE;
-- DROP TABLE IF EXISTS Artistas CASCADE;
-- DROP TABLE IF EXISTS AutoresAlbumes CASCADE;
-- DROP TABLE IF EXISTS AutoresCanciones CASCADE;
-- DROP TABLE IF EXISTS Generos CASCADE;
-- DROP TABLE IF EXISTS Idiomas CASCADE;
-- DROP TABLE IF EXISTS GenerosCanciones CASCADE;
-- DROP TABLE IF EXISTS IdiomasCanciones CASCADE;

