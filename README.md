# BarberMenagment

ASP.NET Core MVC aplikacija za upravljanje frizerskim salonom. Aplikacija omogućava klijentima da pregledaju frizere, usluge i slobodne termine, dok frizeri mogu da upravljaju uslugama, smenama i internim zakazivanjima.

## Tehnologije

- ASP.NET Core MVC
- .NET 10
- Entity Framework Core
- PostgreSQL 16
- Bootstrap 5, jQuery i Razor Views
- Cookie authentication i session state
- MailKit za transakcione e-mail poruke
- Docker Compose za lokalni PostgreSQL

## Arhitektura i struktura projekta

Projekat prati standardnu ASP.NET Core MVC organizaciju:

```text
BarberMenagment/
├── Configuration/       Učitavanje .env vrednosti i SMTP podešavanja
├── Constants/           Ključevi i konstante aplikacije
├── Controllers/         MVC endpointi i orkestracija zahteva
├── Data/
│   ├── Configurations/  EF Core konfiguracije entiteta
│   ├── Migrations/      Code-first migracije baze
│   ├── ApplicationDbContext.cs
│   └── DatabaseSeeder.cs
├── Models/              Entiteti i view modeli
├── Services/            Poslovne usluge, dostupnost termina i e-mail
├── Views/               Razor prikazi po kontrolerima
├── wwwroot/             CSS, JavaScript i statički fajlovi
├── docker-compose.yaml  Lokalni PostgreSQL servis
├── Program.cs           Composition root i HTTP pipeline
└── appsettings*.json    Konfiguracija aplikacije
```

### Tok zahteva

- `Program.cs` registruje MVC, EF Core, PostgreSQL, sesiju, cookie autentikaciju, autorizaciju i aplikacione servise.
- `Controllers/` primaju HTTP zahteve, učitavaju podatke i pripremaju view modele. Kontroleri su podeljeni na `Account`, `Booking`, `Barber` i `Home`.
- `Models/` sadrži EF Core entitete (`User`, `Service`, `Appointment`, `BarberShift`) i modele namenjene pojedinačnim prikazima i formama.
- `BookingAvailabilityService` iz smene frizera, trajanja usluge i aktivnih termina izračunava slobodne slotove. Slotovi se ne čuvaju kao posebna tabela.
- `Data/Configurations/` definiše mapiranje entiteta, relacije, indekse i PostgreSQL tipove.
- `DatabaseSeeder` pri pokretanju izvršava migracije i kreira početne korisnike, usluge, smene i test termine ako već ne postoje.
- `Views/` koristi Razor i ASP.NET Core Tag Helpers za forme, linkove, validaciju i prikaz podataka.
- Autentikacija koristi cookie scheme. Javni korisnici mogu da se registruju kao klijenti, dok se barber nalozi inicijalno kreiraju seederom.

### Glavni korisnički tokovi

- Klijent bira frizera na `/Booking/SelectBarber`, zatim uslugu na `/Booking/SelectService` i datum/termin na `/Booking/Calendar`.
- Nakon potvrde termina kreira se aktivan appointment i šalje se potvrda e-mailom kada je SMTP konfigurisan.
- Frizer na `/Barber/Shifts` definiše prvu smenu, drugu smenu ili slobodan dan.
- Frizer na `/Barber/Services` upravlja uslugama koje pruža.
- Frizer na `/Barber/InternalBooking` može da izabere uslugu, datum i slobodan termin za interno zakazivanje.
- `/Account/Login`, `/Account/Register` i `/Account/Appointments` pokrivaju prijavu, registraciju i pregled klijentovih termina.

## Preduslovi

Instalirati:

- Git
- .NET SDK 10
- Docker Desktop ili Docker Engine sa Docker Compose podrškom
- opciono: EF Core CLI alat za ručno upravljanje migracijama

Ako `dotnet ef` nije dostupan:

```bash
dotnet tool install --global dotnet-ef
```

## Setup projekta

### 1. Kloniranje repozitorijuma

```bash
git clone https://github.com/nole78/barber-menagment.git
cd barber-menagment
```

### 2. Konfiguracija lokalnog okruženja

Kopirati primer konfiguracije u lokalni `.env` fajl:

```bash
cp .env.example .env
```

Za lokalni razvoj, `ConnectionStrings__DefaultConnection` u `.env` treba da koristi iste PostgreSQL parametre kao `docker-compose.yaml`, uključujući razvojnu lozinku `postgrespassword`. `.env` je lokalni fajl i ne treba ga commitovati. SMTP vrednosti mogu ostati prazne ako slanje e-maila nije potrebno za lokalno testiranje.

### 3. Preuzimanje dependency-ja

```bash
dotnet restore
```

### 4. Pokretanje PostgreSQL Docker servisa

```bash
docker compose up -d --build
```

Compose fajl trenutno pokreće PostgreSQL 16 kontejner `barbershop_db` na portu `5432`. Projekat nema poseban Docker image za ASP.NET aplikaciju, pa `--build` nema aplikacioni image koji bi trebalo da izgradi; koristi se zbog kompatibilnosti sa budućim servisima.

Provera statusa servisa:

```bash
docker compose ps
```

### 5. Kreiranje baze i migracija

Za primenu postojeće početne migracije:

```bash
dotnet ef database update
```

Nova migracija se kreira ovako:

```bash
dotnet ef migrations add <NazivMigracije> --output-dir Data/Migrations
dotnet ef database update
```

Napomena: aplikacija pri pokretanju poziva `DatabaseSeeder.SeedAsync`, koji takođe izvršava `Database.MigrateAsync`. Nakon uspešnog pokretanja aplikacije automatski se dodaju inicijalni korisnici, usluge, smene i test termini. Seeder je idempotentan i ne kreira duplikate postojećih zapisa.

### 6. Pokretanje aplikacije

```bash
dotnet run --launch-profile http
```

Aplikacija je dostupna na:

- http://localhost:5099

HTTPS profil se pokreće komandom:

```bash
dotnet run --launch-profile https
```

HTTPS adresa:

- https://localhost:7073

Za zaustavljanje aplikacije pritisnuti `Ctrl+C`, a za zaustavljanje baze:

```bash
docker compose down
```

Za brisanje i Docker volume-a sa lokalnim podacima:

```bash
docker compose down -v
```

Ova komanda briše lokalnu PostgreSQL bazu i treba je koristiti samo kada je reset baze nameran.

## Početni korisnici za testiranje

Seeder kreira sledeće korisnike. Lozinke su namenjene isključivo lokalnom razvojnom i test okruženju.

### Frizeri

| E-mail | Lozinka | Uloga |
|---|---|---|
| `marko.barber@radisav.local` | `admin123` | Barber |
| `nikola.barber@radisav.local` | `barber123` | Barber |

### Klijenti

| E-mail | Lozinka | Uloga |
|---|---|---|
| `petar.client@radisav.local` | `client123` | Client |
| `jovan.client@radisav.local` | `client123` | Client |

Početne usluge i dodela usluga:

- `Muško šišanje` — 30 minuta; dostupno kod oba frizera
- `Skin fade` — 45 minuta; dostupno kod Marka
- `Sređivanje brade` — 30 minuta; dostupno kod Nikole

Seeder takođe kreira smene za narednih sedam dana i nekoliko online/internih termina kako bi se odmah mogli testirati slobodni, zauzeti i različito trajajući slotovi.

## Konfiguracija

Najvažnija podešavanja mogu se zadati kroz environment varijable ili `.env` fajl. ASP.NET Core koristi `__` kao separator sekcija:

```text
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=barbershop_db;Username=postgres;Password=postgrespassword
Smtp__Host=smtp.example.com
Smtp__Port=587
Smtp__UseSsl=true
Smtp__Username=your-smtp-username
Smtp__Password=change-me
Smtp__FromEmail=no-reply@example.com
Smtp__FromName=Radisav Fashion
```

Ne postavljati stvarne produkcione lozinke, SMTP kredencijale ili produkcione connection stringove u source control. Produkciona konfiguracija treba da se prosledi kroz environment/deployment secrets.

## Testiranje i korisne komande

U repozitorijumu trenutno ne postoji poseban test projekat. Dostupne osnovne komande su:

```bash
dotnet build
dotnet test
```

`dotnet test` trenutno nema aplikacione testove za izvršavanje. Frontend JavaScript i CSS se ne build-uju posebnim Node pipeline-om; Bootstrap, jQuery i aplikacioni fajlovi se učitavaju iz `wwwroot/`.
