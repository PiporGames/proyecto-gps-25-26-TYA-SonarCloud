#!/usr/bin/env python3
"""
Script para insertar datos de prueba en el microservicio TYA
Genera: 1 artista, varios álbumes, canciones (singles y en álbumes), merchandising
"""

import requests
import sys

BASE_URL = "http://localhost:8081"
AUTH_TOKEN = "07186e0e6e5a5691ba38fed320687429c98d965359762980a43472b51cf1cbd7e290f2bc16ec2bac8515a952881095ad20b654527a6e9d924f9bb123f2c164d0"
cookies = {"oversound_auth": AUTH_TOKEN}

def post(endpoint, data):
    r = requests.post(f"{BASE_URL}{endpoint}", json=data, cookies=cookies)
    if r.status_code != 200:
        print(f"ERROR {endpoint}: {r.status_code} - {r.text}")
        sys.exit(1)
    return r.json()

# Obtener géneros disponibles
print("Obteniendo géneros disponibles...")
r = requests.get(f"{BASE_URL}/genres")
genres = r.json()
genre_ids = [g["id"] for g in genres[:3]] if genres else [1]  # Usar los primeros 3 géneros o [1] por defecto
print(f"✓ Géneros disponibles: {[g['name'] for g in genres[:3]]}")

# Crear artista
print("Creando artista...")
artist = post("/artist/upload", {
    "artisticName": "Luna Eclipse",
    "artisticBiography": "Cantautora indie con influencias de rock alternativo y folk. Ganadora de múltiples premios, conocida por sus letras introspectivas y melodías envolventes.",
    "artisticImage": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==",
    "artisticEmail": "contact@lunaeclipse.music",
    "socialMediaUrl": "https://instagram.com/lunaeclipseofficial",
    "userId": 1
})
artist_id = artist["artistId"]
print(f"✓ Artista creado (ID: {artist_id})")

# Crear singles (canciones sin álbum)
print("\nCreando singles...")
singles = []
single_data = [
    {"title": "Midnight Dreams", "duration": 245, "price": 1.29, "trackId": 10001, "cover": "data:image/png;base64,iVBORw0KGg1"},
    {"title": "Electric Heart", "duration": 198, "price": 0.99, "trackId": 10002, "cover": "data:image/png;base64,iVBORw0KGg2"},
    {"title": "Neon Lights", "duration": 213, "price": 1.49, "trackId": 10003, "cover": "data:image/png;base64,iVBORw0KGg3"}
]
for sd in single_data:
    s = post("/song/upload", {**sd, "genres": genre_ids[:2], "description": f"Single lanzado en 2024"})
    singles.append(s["songId"])
    print(f"✓ Single '{sd['title']}' (ID: {s['songId']})")

# Crear álbum vacío
print("\nCreando álbum vacío...")
empty_album = post("/album/upload", {
    "title": "Untitled Project (Coming Soon)",
    "songs": [],
    "cover": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg==",
    "price": 0.01,
    "releaseDate": "2025-12-31"
})
print(f"✓ Álbum vacío creado (ID: {empty_album['albumId']})")

# Crear álbum 1: "Shadows & Echoes"
print("\nCreando álbum 'Shadows & Echoes'...")
album1_songs = []
album1_data = [
    {"title": "Intro: The Awakening", "duration": 82, "price": 0.69, "trackId": 20001, "order": 1},
    {"title": "Lost in Translation", "duration": 224, "price": 1.29, "trackId": 20002, "order": 2},
    {"title": "Echoes of You", "duration": 267, "price": 1.29, "trackId": 20003, "order": 3},
    {"title": "Dancing with Shadows", "duration": 198, "price": 1.29, "trackId": 20004, "order": 4},
    {"title": "Interlude: Silence", "duration": 45, "price": 0.49, "trackId": 20005, "order": 5},
    {"title": "Broken Wings", "duration": 301, "price": 1.49, "trackId": 20006, "order": 6},
    {"title": "Outro: Fade Away", "duration": 156, "price": 0.99, "trackId": 20007, "order": 7}
]

# Primero crear álbum vacío
album1 = post("/album/upload", {
    "title": "Shadows & Echoes",
    "songs": [],
    "cover": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJALBUM1",
    "price": 9.99,
    "releaseDate": "2023-03-15"
})
album1_id = album1["albumId"]

# Crear canciones asociadas al álbum
for sd in album1_data:
    s = post("/song/upload", {
        "title": sd["title"],
        "duration": sd["duration"],
        "price": sd["price"],
        "trackId": sd["trackId"],
        "genres": genre_ids[:2],
        "cover": f"data:image/png;base64,song{sd['trackId']}",
        "description": f"Track {sd['order']} from Shadows & Echoes",
        "albumId": album1_id,
        "albumOrder": sd["order"]
    })
    album1_songs.append(s["songId"])
    print(f"✓ '{sd['title']}' (ID: {s['songId']})")

print(f"✓ Álbum 'Shadows & Echoes' (ID: {album1_id}) con {len(album1_songs)} canciones")

# Crear álbum 2: "Cosmic Journey"
print("\nCreando álbum 'Cosmic Journey'...")
album2_songs = []
album2_data = [
    {"title": "Stellar", "duration": 189, "price": 1.29, "trackId": 30001, "order": 1},
    {"title": "Gravity", "duration": 234, "price": 1.29, "trackId": 30002, "order": 2},
    {"title": "Nebula", "duration": 276, "price": 1.49, "trackId": 30003, "order": 3},
    {"title": "Orbit", "duration": 201, "price": 1.29, "trackId": 30004, "order": 4},
    {"title": "Supernova", "duration": 312, "price": 1.49, "trackId": 30005, "order": 5}
]

album2 = post("/album/upload", {
    "title": "Cosmic Journey",
    "songs": [],
    "cover": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJCOSMIC1",
    "price": 7.99,
    "releaseDate": "2024-06-20"
})
album2_id = album2["albumId"]

for sd in album2_data:
    s = post("/song/upload", {
        "title": sd["title"],
        "duration": sd["duration"],
        "price": sd["price"],
        "trackId": sd["trackId"],
        "genres": genre_ids[-2:] if len(genre_ids) >= 2 else genre_ids,
        "cover": f"data:image/png;base64,cosmic{sd['trackId']}",
        "description": f"Track {sd['order']} from Cosmic Journey - An electronic odyssey",
        "albumId": album2_id,
        "albumOrder": sd["order"]
    })
    album2_songs.append(s["songId"])
    print(f"✓ '{sd['title']}' (ID: {s['songId']})")

print(f"✓ Álbum 'Cosmic Journey' (ID: {album2_id}) con {len(album2_songs)} canciones")

# Crear álbum "Greatest Hits" con canciones ya existentes (linked_albums)
print("\nCreando álbum 'Greatest Hits'...")
# Seleccionar canciones aleatorias de singles y álbumes existentes
hits_songs = [
    singles[0],  # Midnight Dreams
    album1_songs[2],  # Echoes of You
    singles[1],  # Electric Heart
    album2_songs[0],  # Stellar
    album1_songs[5],  # Broken Wings
    album2_songs[4]   # Supernova
]

album_hits = post("/album/upload", {
    "title": "Greatest Hits - Best of Luna Eclipse",
    "songs": hits_songs,
    "cover": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJHITS1",
    "price": 12.99,
    "releaseDate": "2024-11-01"
})
album_hits_id = album_hits["albumId"]
print(f"✓ Álbum 'Greatest Hits' (ID: {album_hits_id}) con {len(hits_songs)} canciones linkeadas")

# Crear merchandising
print("\nCreando merchandising...")
merch_data = [
    {"title": "Luna Eclipse - Official T-Shirt", "description": "Camiseta oficial negra con logo holográfico. 100% algodón orgánico. Tallas disponibles: S, M, L, XL", "price": 29.99, "cover": "data:image/png;base64,merchTshirt"},
    {"title": "Shadows & Echoes Vinyl LP", "description": "Edición limitada en vinilo de 180g. Incluye poster exclusivo y código de descarga digital.", "price": 34.99, "cover": "data:image/png;base64,merchVinyl"},
    {"title": "Luna Eclipse Tote Bag", "description": "Bolsa de tela resistente con diseño exclusivo. Perfecta para conciertos y uso diario.", "price": 19.99, "cover": "data:image/png;base64,merchBag"},
    {"title": "Cosmic Journey Hoodie", "description": "Sudadera con capucha premium. Diseño bordado del álbum Cosmic Journey. 80% algodón, 20% poliéster.", "price": 49.99, "cover": "data:image/png;base64,merchHoodie"},
    {"title": "Luna Eclipse Poster Set", "description": "Set de 3 posters de alta calidad (30x40cm). Diseños exclusivos de los álbumes y arte conceptual.", "price": 24.99, "cover": "data:image/png;base64,merchPosters"}
]

for md in merch_data:
    m = post("/merch/upload", md)
    print(f"✓ {md['title']} (ID: {m['merchId']}) - ${md['price']}")

print("\n" + "="*60)
print("✅ DATOS DE PRUEBA INSERTADOS EXITOSAMENTE")
print("="*60)
print(f"\nResumen:")
print(f"  • 1 Artista: Luna Eclipse")
print(f"  • 3 Singles")
print(f"  • 3 Álbumes con canciones:")
print(f"    - Shadows & Echoes (7 tracks)")
print(f"    - Cosmic Journey (5 tracks)")
print(f"    - Greatest Hits (6 tracks linkeadas)")
print(f"  • 1 Álbum vacío")
print(f"  • 5 Productos de merchandising")
print(f"\nTotal: {len(singles) + len(album1_songs) + len(album2_songs)} canciones únicas")
print(f"Nota: Las canciones en Greatest Hits NO tienen este álbum como albumog,")
print(f"      pero aparecerán en su campo linked_albums")
