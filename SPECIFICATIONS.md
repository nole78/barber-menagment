# OPERATIVNI PLAN RAZVOJA SOFTVERSKOG REŠENJA: BARBERSHOP MANAGEMENT SYSTEM ("RADISAV FASHION") 

## 1. Vizija Projekta, Poslovni Ciljevi i Tehnološki Okvir 

Digitalizacija poslovanja savremenih uslužnih delatnosti, poput frizerskih salona, predstavlja ključni korak ka optimizaciji resursa, smanjenju operativnih troškova i značajnom unapređenju korisničkog iskustva. Automatizacija procesa zakazivanja eliminiše potrebu za ručnim vođenjem evidencije, potpuno sprečava ljudske greške pri preklapanju rasporeda i omogućava klijentima jednostavan, 24/7 pristup slobodnim terminima. Uvođenjem integrisanog digitalnog sistema, salon "Radiav Fashion" transformiše svoj operativni model, pružajući visoku transparentnost u pogledu ponude i cena, dok osoblju omogućava potpunu kontrolu nad radnim kapacitetima i smenama. 

### 1.1. Osnovne informacije i sažetak rešenja 

Softversko rešenje **Barbershop "Radisav Fashion"** projektovano je kao moderna web aplikacija za upravljanje radom frizerskog salona i automatizovano online zakazivanje tretmana. Primarna svrha aplikacije jeste da eksternim korisnicima (klijentima) obezbedi intuitivan uvid u ponudu usluga, profile frizera, cenovnik i dostupne vremenske slotove unutar kalendara tekućeg meseca. Sa druge strane, rešenje internim korisnicima (frizerima sa administrativnim privilegijama) pruža potpun administrativni nadzor i upravljački panel za kontrolu asortimana usluga, rasporeda radnih smena, unosa internih rezervacija i otkazivanja termina uz automatsku e-mail komunikaciju. 

### 1.2. Analiza tehnološkog steka (Technology Stack) 

Izbor tehnologija usmeren je na kreiranje pouzdane, sigurne i stabilne arhitekture koja obezbeđuje visok stepen odvojenosti slojeva aplikacije (Separation of Concerns). 

- **Backend Framework:** C# ASP.NET Core MVC (.NET 10) 

- Obrada poslovne logike, rutiranje i kontrolerski sloj. Pruža visoke performanse, stabilnost i robusnu arhitekturu za obradu aplikativnih zahteva. 

- **Baza podataka:** PostgreSQL 

   - Relaciono skladištenje podataka sistema. Napredni relacioni sistem koji garantuje integritet transakcija i pouzdanost kompleksnih upita. 

- **ORM:** Entity Framework Core (Code-First) Mapiranje objekata u bazu i upravljanje šemom. Omogućava razdvajanje modela podataka od baze, brze migracije i efikasno pisanje upita kroz C#. 

- **Frontend:** Bootstrap 5, Razor Views, HTML5, CSS3, JavaScript (Vanilla JS) Renderovanje korisničkog interfejsa i klijentska interaktivnost. Obezbeđuje moderan, responzivan dizajn prilagođen svim uređajima uz lagane klijentske skripte. 

- **Autentifikacija & Autorizacija:** ASP.NET Core Cookie Authentication Upravljanje korisničkim sesijama na serveru i kontrola pristupa na osnovu uloga. Sigurno čuvanje stanja prijave unutar server-side sesije uz enkriptovani autentifikacioni kolačić za automatsku identifikaciju korisnika na ulogama Klijent i Frizer. 

- **E-mail servis:** MailKit / SMTP 

   - Slanje automatskih obaveštenja i potvrda. Automatizuje eksternu komunikaciju slanjem transakcionih poruka klijentima. 

## 2. Detaljna Šema i Arhitektura Baze Podataka (PostgreSQL) 

Robusno modeliranje podataka zasnovano na Code-First pristupu predstavlja osnovu za izradu sistema za zakazivanje u realnom vremenu. U okruženjima gde više korisnika istovremeno pristupa kalendaru, baza podataka mora osigurati transakcionu konzistentnost, sprečiti preklapanje termina i precizno sačuvati istorijske podatke o cenama i uslugama. 

### 2.1. Specifikacija entiteta i relacija 

Baza podataka se sastoji od pet ključnih entiteta povezanih primarnim i stranim ključevima radi očuvanja relacione celovitosti: 

### _1. Users (Korisnici i Frizeri)_ 

Čuva podatke o svim registrovanim korisnicima aplikacije (klijentima i frizerima). 

- Id (int, Primary Key, Auto-increment) – Jedinstveni identifikator korisnika. 

- Ime (string, Required) – Ime korisnika. 

- Prezime (string, Required) – Prezime korisnika. 

- Email (string, Required) – E-mail adresa (koristi se za prijavu i obaveštenja). 

- BrojTelefona (string, Required) – Kontakt telefon. 

- PasswordHash (string, Required) – Kriptovani hash korisničke lozinke. 

- Role (string, Required) – Uloga u sistemu ("Klijent" ili "Frizer"). 

### _2. Services (Usluge salona)_ 

Definiše ponudu uslužnih delatnosti u salonu. 

- Id (int, Primary Key, Auto-increment) – Jedinstveni identifikator usluge. 

- Naziv (string, Required) – Naziv usluge (npr. _Muško šišanje_ , _Fejd_ , _Sređivanje brade_ ). 

- Opis (string) – Detaljan opis tretmana. 

- Cena (decimal, Required) – Trenutna cena usluge. 

- TrajanjeMinuti (int, Required) – Trajanje usluge u minutima (npr. 30, 45). 

### _3. BarberServices (Spojna tabela M:N)_ 

Predstavlja spojnu tabelu za relaciju više-prema-više između frizera i usluga koje pružaju. 

- BarberId (int, Foreign Key -> Users.Id, Required) – Identifikator frizera. 

- ServiceId (int, Foreign Key -> Services.Id, Required) – Identifikator usluge. 

- _Primarni ključ:_ Kompozitni primarni ključ (BarberId, ServiceId) sprečava dupliranje parova frizer-usluga na nivou baze. 

### _4. BarberShifts (Radne smene frizera)_ 

Definiše raspored radnog vremena frizera po danima. 

- Id (int, Primary Key, Auto-increment) – Jedinstveni identifikator smene. 

- BarberId (int, Foreign Key -> Users.Id, Required) – Identifikator frizera. 

- Datum (Date, Required) – Datum na koji se smena odnosi. 

- TipSmene (Enum / string, Required) – Vrsta smene sa tačno definisanim intervalima: PrvaSmena 08:00 - 14:00, DrugaSmena 14:00 - 20:00, ili SlobodanDan. 

### _5. Appointments (Zakazani termini)_ 

Centralni entitet koji bilježi sve realizovane i predstojeće rezervacije. 

- Id (int, Primary Key, Auto-increment) – Jedinstveni identifikator termina. 

- ClientId (int, Foreign Key -> Users.Id, **Nullable** ) – Identifikator klijenta. Polje je nullable radi omogućavanja internih rezervacija za neregistrovane klijente. 

- BarberId (int, Foreign Key -> Users.Id, Required) – Identifikator izabranog frizera. 

- ServiceId (int, Foreign Key -> Services.Id, Required) – Identifikator zakazane usluge. 

- DatumVreme (DateTime, Required) – Datum i tačno vreme početka termina. 

- Cena (decimal, Required) – Zabeležena cena u trenutku pravljenja rezervacije. 

- TipRezervacije (Enum / string, Required) – Vrsta rezervacije: OnlineKlijent ili InternoFrizer. 

- Status (Enum / string, Required) – Status termina: Aktivno ili Otkazano. 

- RazlogOtkazivanja (string, **Nullable** ) – Tekstualno obrazloženje koje se popunjava isključivo prilikom otkazivanja. 

### 2.2. Evaluacija strukture i poslovnih pravila 

- Arhitektura baze sadrži dve specifičnosti od ključnog značaja za poslovne procese salona: 

   - **Nullable ClientId polje:** Polje ClientId u entitetu Appointments eksplicitno dopušta NULL vrednosti. Ovo pravilo omogućava frizerima unosa internih rezervacija uživo ili putem telefona, kao i blokiranje termina za pauzu ili privatne obaveze, gde klijent nema kreiran nalog u sistemu, a termin dobija oznaku InternoFrizer. 

   - **Zamrzavanje cene u entitetu Appointments:** Polje Cena u entitetu Appointments kopira i trajno čuva iznos iz entiteta Services u trenutku pravljenja rezervacije. Ovim mehanizmom sprečava se da naknadne promene cenovnika u salonu izmene već ugovorene cene postojećih rezervacija, čime se garantuje finansijska konzistentnost i zaštita klijentskih prava.Precizno definisana struktura baze podataka predstavlja neophodnu osnovu za pouzdano izvršavanje svih korisničkih tokova u aplikaciji. 

## 3. Korisnički Tokovi i Funkcionalne Specifikacije (User Flows) 

Sistem "Radisav Fashion" razdvaja korisničke putanje na eksterne (za klijente) i interne (za osoblje salona). Ovakav pristup optimizuje konverziju klijenta kroz jednostavan i intuitivan proces zakazivanja, dok radnicima obezbeđuje visoku operativnu efikasnost. 

### 3.1. Eksterni korisnički tok za Klijenta (Gost / Registrovani korisnik) 

Proces zakazivanja za klijenta odvija se kroz jasne, hronološke korake sa ugrađenom proverom autentifikacije: 

- **Landing Page ( /Home ):** Klijent pristupa početnoj stranici koja sadrži generalne informacije o salonu "Radisav Fashion", opšti opis, lokaciju, radno vreme i kontakt podatke. Centralno akciono dugme _"Rezervišite termin"_ preusmerava klijenta u proces zakazivanja. 

- **Izbor frizera ( /Booking/SelectBarber ):** Prikazuju se profilne kartice sa informacijama o svim raspoloživim frizerima u salonu. Klijent bira željeno lice klikom na odgovarajuće dugme. 

- **Izbor usluge ( /Booking/SelectService ):** Aplikacija prikazuje isključivo usluge koje izabrani frizer pruža (na osnovu BarberServices veze), sa jasno istaknutim cenama i trajanjem tretmana u minutima. 

- **Provera autentifikacije (Security Guard):** 

   - Ako korisnik **nije prijavljen** (u server-side sesiji ne postoji validan autentifikacioni identifikator), sistem ga preusmerava na stranicu /Account/Login uz poruku obaveštenja: _"Morate biti prijavljeni da biste izabrali termin"_ . Nakon uspešne prijave ili registracije, korisnik se automatski vraća nazad na korak sa kalendarom. 

- Ako je korisnik _već prijavljen_ , sistem ga odmah sprovodi na sledeći korak. 

- **Kalendar i izbor termina ( /Booking/Calendar ):** Prikazuje se interaktivni kalendar za tekući mesec. U zaglavlju ili podnožju kalendara prikazani su sumarni podaci: izabrani frizer, usluga i ukupna cena. Klikom na željeni dan, algoritam vrši dinamički proračun slobodnih slotova: 

   - **Algoritam za kalkulaciju slotova:** Sistem prvo iz entiteta BarberShifts čita tip smene za izabrani datum. Ukoliko je TipSmene == SlobodanDan, prikazuje se obaveštenje _"Nema slobodnih termina za izabrani datum"_ . Ukoliko je smena aktivna (PrvaSmena 08:00 - 14:00, tj. 360 minuta ili DrugaSmena 14:00 - 20:00, tj. 360 minuta), radni vremenski prozor se deli na diskretne vremenske slotove čija je dužina jednaka vrednosti Services.TrajanjeMinuti. Zatim se iz baze povlače svi postojeći termini iz Appointments za tog frizera i taj datum gde je Status == 'Aktivno'. Svaki generisani slot koji se vremenski preklapa sa postojećim aktivnim terminom uklanja se iz liste. Preostali slotovi prikazuju se klijentu za izbor. 

- **Potvrda rezervacije i automatizacija:** Klikom na slobodan vremenski slot otvara se modalni prozor sa pregledom svih detalja. Klikom na dugme _"Potvrdi"_ , kreira se novi zapis u tabeli Appointments (sa tipom OnlineKlijent i statusom Aktivno), a klijentu se automatski šalje e-mail sa potvrdom i detaljima termina. 

### 3.2. Interni korisnički tok za Frizera (Admin Panel) 

Nakon prijave naloga sa ulogom Frizer, u navigacionom meniju se pojavljuje opcija 

**"Frizerski Panel"** . Ovaj panel obuhvata četiri ključne modulske funkcionalnosti: 

- **Upravljanje uslugama (CRUD):** Tabela sa pregledom svih usluga u salonu, uz mogućnost dodavanja novih tretmana, izmene postojećih opisa i cena, ili brisanja usluga iz ponude. 

- **Upravljanje smenama:** Kalendarski i tabelarni prikaz u kom frizer za određene dane postavlja svoj raspored rada, birajući između opcija: PrvaSmena (08:00 - 14:00), DrugaSmena (14:00 - 20:00) ili SlobodanDan. 

- **Unutrašnje (Interno) zakazivanje:** Funkcionalnost koja omogućava frizeru da direktno u sistemu rezerviše termin za klijente koji zakazuju telefonom ili uživo, ili da blokira određeni vremenski period. Zakazani termini dobijaju oznaku InternoFrizer. 

- **Pregled i otkazivanje termina sa razlogom:** Frizer ima uvid u hronološku listu svih predstojećih termina. Pored svakog termina nalazi se dugme _"Otkaži"_ . Klikom na dugme otvara se modalni prozor sa obaveznim tekstualnim poljem za unos razloga otkazivanja (npr. _"Bolovanje"_ , _"Iznenadne obaveze"_ ). 

- **Tranzicija stanja i događaji:** Klikom na dugme _"Potvrdi"_ , pokreće se tranzicija stanja u bazi gde se atribut Status menja iz Aktivno u Otkazano, a polje RazlogOtkazivanja popunjava unetim tekstom. Istovremeno se generiše i asinhrono isporučuje MailKit/SMTP e-mail obaveštenje klijentu sa navedenim razlogom otkazivanja.Operacije u navedenim korisničkim tokovima direktno oslanjaju svoj rad na bezbednosne mehanizme i servise za automatizaciju obaveštenja. 

## 4. Strategija Autentifikacije, Autorizacije i Automatizacije Obaveštenja 

Pouzdana identifikacija korisnika i pravovremena komunikacija čine stub stabilnosti i profesionalnosti aplikacije. Kontrola pristupa štiti tajnost podataka i sprečava neovlašćene izmene, dok e-mail obaveštenja smanjuju procenat nedolazaka na zakazane termine. 

### 4.1. Bezbednosni mehanizmi i Cookie Autentifikacija 

Aplikacija koristi ASP.NET Core **Cookie Authentication i Session State** mehanizme za bezbedno i jednostavno upravljanje korisničkim stanjem. 

Prilikom prijave, sistem verifikuje korisničke podatke poređenjem kriptovanog hasha unete lozinke sa zapisom PasswordHash u bazi podataka. Nakon uspešne validacije, kreira se autentifikacioni kolačić i inicijalizuje korisnička sesija na serveru. 

Sistem štiti privatne i administrativne rute primenom autorizacionih atributa: 

- Administrativne rute namenjene osoblju, poput /Barber/... i kontrolera Frizerskog Panela, eksplicitno su zaštićene proverom uloge putem atributa [Authorize(Roles = "Frizer")]. 

- Ukoliko anonimni posetilac ili korisnik sa ulogom Klijent pokuša da pristupi administrativnim rutama, middleware automatski presreće zahtev, odbija pristup i preusmerava korisnika na stranicu za prijavu uz odgovarajući statusni kôd. 

### 4.2. Automatizovani e-mail servis (MailKit / SMTP) 

Integracijom MailKit biblioteke i SMTP protokola, sistem automatizuje komunikaciju sa klijentima, eliminisajući potrebu za ručnim slanjem poruka. Slanje e-mail poruka okidaju dva ključna događaja u sistemu: 

- **Okidač 1: Potvrda rezervacije:** Nakon što klijent uspešno kreira i potvrdi termin kroz kalendar, servis generiše i šalje poruku koja sadrži naziv usluge, ime izabranog frizera, datum, vreme i usaglašenu cenu. 

- **Okidač 2: Otkazivanje termina od strane frizera:** Kada frizer otkaže termin kroz upravljački panel, servis prikuplja uneseno tekstualno obrazloženje i generiše e-mail koji sadrži detalje otkazanog termina i navedeni razlog otkazivanja.Slanje jasnog razloga otkazivanja osigurava transparentnost u vanrednim situacijama (poput bolesti ili iznenadnih obaveza osoblja), čime se čuva poverenje klijenata.Navedena bezbednosna infrastruktura, uz odabrani .NET 10 stek, efikasnu bazu podataka i definisane operativne tokove, obezbeđuje kompletnu automatizaciju i visoku pouzdanost u radu frizerskog salona "Radisav Fashion". 

