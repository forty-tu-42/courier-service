/* ============================================================================
   КУРСОВОЙ ПРОЕКТ. ИНФОРМАЦИОННО-СПРАВОЧНАЯ СИСТЕМА «КУРЬЕРСКАЯ СЛУЖБА»
   Полный скрипт создания базы данных, ограничений, представлений,
   хранимых процедур и тестовых данных.
   СУБД: Microsoft SQL Server 2019/2022
   Префикс объектов: KURS_KS_ (Курсовая, Курьерская Служба)
   ----------------------------------------------------------------------------
   КАК ЗАПУСКАТЬ:
   1. Откройте SQL Server Management Studio.
   2. Подключитесь к своему серверу (например, stud-mssql.sttec.yar.ru,38325).
   3. Выберите базу данных user93_db (или свою учебную базу).
   4. Откройте этот файл и нажмите «Выполнить» (F5).
   Скрипт можно запускать повторно: он удаляет и создаёт только объекты
   с префиксом KURS_KS_, не затрагивая работы других студентов.
   ============================================================================ */

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

PRINT N'=== 1. Удаление предыдущей версии объектов KURS_KS_ ===';
GO

IF OBJECT_ID(N'dbo.VW_KS_Zakazy', N'V') IS NOT NULL DROP VIEW dbo.VW_KS_Zakazy;
IF OBJECT_ID(N'dbo.VW_KS_Dostavki', N'V') IS NOT NULL DROP VIEW dbo.VW_KS_Dostavki;
IF OBJECT_ID(N'dbo.VW_KS_Zagruzka_Kurierov', N'V') IS NOT NULL DROP VIEW dbo.VW_KS_Zagruzka_Kurierov;
IF OBJECT_ID(N'dbo.VW_KS_Vyruchka_Po_Zonam', N'V') IS NOT NULL DROP VIEW dbo.VW_KS_Vyruchka_Po_Zonam;
GO

IF OBJECT_ID(N'dbo.sp_KS_Sozdat_Zakaz', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_KS_Sozdat_Zakaz;
IF OBJECT_ID(N'dbo.sp_KS_Naznachit_Kuriera', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_KS_Naznachit_Kuriera;
IF OBJECT_ID(N'dbo.sp_KS_Izmenit_Status', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_KS_Izmenit_Status;
IF OBJECT_ID(N'dbo.sp_KS_Otchet_Dostavki', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_KS_Otchet_Dostavki;
GO

IF OBJECT_ID(N'dbo.KURS_KS_Audit', N'U') IS NOT NULL DROP TABLE dbo.KURS_KS_Audit;
IF OBJECT_ID(N'dbo.KURS_KS_Istoriya_Statusov', N'U') IS NOT NULL DROP TABLE dbo.KURS_KS_Istoriya_Statusov;
IF OBJECT_ID(N'dbo.KURS_KS_Zakaz', N'U') IS NOT NULL DROP TABLE dbo.KURS_KS_Zakaz;
IF OBJECT_ID(N'dbo.KURS_KS_Tarif', N'U') IS NOT NULL DROP TABLE dbo.KURS_KS_Tarif;
IF OBJECT_ID(N'dbo.KURS_KS_Kurier', N'U') IS NOT NULL DROP TABLE dbo.KURS_KS_Kurier;
IF OBJECT_ID(N'dbo.KURS_KS_Klient', N'U') IS NOT NULL DROP TABLE dbo.KURS_KS_Klient;
IF OBJECT_ID(N'dbo.KURS_KS_Zona', N'U') IS NOT NULL DROP TABLE dbo.KURS_KS_Zona;
IF OBJECT_ID(N'dbo.KURS_KS_Status_Zakaza', N'U') IS NOT NULL DROP TABLE dbo.KURS_KS_Status_Zakaza;
IF OBJECT_ID(N'dbo.KURS_KS_Polzovatel', N'U') IS NOT NULL DROP TABLE dbo.KURS_KS_Polzovatel;
IF OBJECT_ID(N'dbo.KURS_KS_Rol', N'U') IS NOT NULL DROP TABLE dbo.KURS_KS_Rol;
IF OBJECT_ID(N'dbo.KURS_KS_Seq_Zakaz', N'SO') IS NOT NULL DROP SEQUENCE dbo.KURS_KS_Seq_Zakaz;
GO

PRINT N'=== 2. Создание таблиц ===';
GO

/* ---------------------------------------------------------------------------
   2.1. Роли пользователей
   --------------------------------------------------------------------------- */
CREATE TABLE dbo.KURS_KS_Rol
(
    id_roli     INT IDENTITY(1,1) NOT NULL,
    nazvanie    NVARCHAR(50)      NOT NULL,
    CONSTRAINT PK_KS_Rol PRIMARY KEY (id_roli),
    CONSTRAINT UQ_KS_Rol_Nazvanie UNIQUE (nazvanie)
);
GO

/* ---------------------------------------------------------------------------
   2.2. Пользователи системы
   Пароль хранится в виде SHA-256 от соли и пароля (учебный вариант).
   --------------------------------------------------------------------------- */
CREATE TABLE dbo.KURS_KS_Polzovatel
(
    id_polzovatelya INT IDENTITY(1,1) NOT NULL,
    login           NVARCHAR(50)      NOT NULL,
    parol_hash      CHAR(64)          NOT NULL,
    sol             CHAR(16)          NOT NULL,
    id_roli         INT               NOT NULL,
    fio             NVARCHAR(150)     NOT NULL,
    telefon         NVARCHAR(30)      NULL,
    is_active       BIT               NOT NULL CONSTRAINT DF_KS_Polzovatel_Active DEFAULT (1),
    data_sozdaniya  DATETIME2(0)      NOT NULL CONSTRAINT DF_KS_Polzovatel_Data DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_KS_Polzovatel PRIMARY KEY (id_polzovatelya),
    CONSTRAINT UQ_KS_Polzovatel_Login UNIQUE (login),
    CONSTRAINT FK_KS_Polzovatel_Rol FOREIGN KEY (id_roli)
        REFERENCES dbo.KURS_KS_Rol (id_roli)
);
GO

/* ---------------------------------------------------------------------------
   2.3. Зоны обслуживания
   --------------------------------------------------------------------------- */
CREATE TABLE dbo.KURS_KS_Zona
(
    id_zony     INT IDENTITY(1,1) NOT NULL,
    nazvanie    NVARCHAR(100)     NOT NULL,
    opisanie    NVARCHAR(300)     NULL,
    CONSTRAINT PK_KS_Zona PRIMARY KEY (id_zony),
    CONSTRAINT UQ_KS_Zona_Nazvanie UNIQUE (nazvanie)
);
GO

/* ---------------------------------------------------------------------------
   2.4. Клиенты
   --------------------------------------------------------------------------- */
CREATE TABLE dbo.KURS_KS_Klient
(
    id_klienta      INT IDENTITY(1,1) NOT NULL,
    fio             NVARCHAR(150)     NOT NULL,
    telefon         NVARCHAR(30)      NOT NULL,
    email           NVARCHAR(100)     NULL,
    adres           NVARCHAR(250)     NULL,
    data_registracii DATETIME2(0)     NOT NULL CONSTRAINT DF_KS_Klient_Data DEFAULT (SYSDATETIME()),
    is_active       BIT               NOT NULL CONSTRAINT DF_KS_Klient_Active DEFAULT (1),
    CONSTRAINT PK_KS_Klient PRIMARY KEY (id_klienta),
    CONSTRAINT UQ_KS_Klient_Telefon UNIQUE (telefon),
    CONSTRAINT CK_KS_Klient_Telefon CHECK (LEN(telefon) >= 10)
);
GO

/* ---------------------------------------------------------------------------
   2.5. Курьеры
   --------------------------------------------------------------------------- */
CREATE TABLE dbo.KURS_KS_Kurier
(
    id_kuriera      INT IDENTITY(1,1) NOT NULL,
    id_polzovatelya INT               NULL,
    fio             NVARCHAR(150)     NOT NULL,
    telefon         NVARCHAR(30)      NOT NULL,
    transport       NVARCHAR(50)      NOT NULL,
    id_zony         INT               NOT NULL,
    status          NVARCHAR(30)      NOT NULL CONSTRAINT DF_KS_Kurier_Status DEFAULT (N'Свободен'),
    is_active       BIT               NOT NULL CONSTRAINT DF_KS_Kurier_Active DEFAULT (1),
    data_priema     DATE              NOT NULL CONSTRAINT DF_KS_Kurier_Data DEFAULT (CAST(SYSDATETIME() AS DATE)),
    CONSTRAINT PK_KS_Kurier PRIMARY KEY (id_kuriera),
    CONSTRAINT UQ_KS_Kurier_Telefon UNIQUE (telefon),
    CONSTRAINT FK_KS_Kurier_Polzovatel FOREIGN KEY (id_polzovatelya)
        REFERENCES dbo.KURS_KS_Polzovatel (id_polzovatelya),
    CONSTRAINT FK_KS_Kurier_Zona FOREIGN KEY (id_zony)
        REFERENCES dbo.KURS_KS_Zona (id_zony),
    CONSTRAINT CK_KS_Kurier_Transport CHECK (transport IN (N'Пешком', N'Велосипед', N'Мотоцикл', N'Автомобиль')),
    CONSTRAINT CK_KS_Kurier_Status CHECK (status IN (N'Свободен', N'Занят', N'Отдыхает'))
);
GO

/* один пользователь может быть привязан только к одному курьеру */
CREATE UNIQUE INDEX UX_KS_Kurier_Polzovatel
    ON dbo.KURS_KS_Kurier (id_polzovatelya)
    WHERE id_polzovatelya IS NOT NULL;
GO

/* ---------------------------------------------------------------------------
   2.6. Тарифы: зона + диапазон веса -> стоимость
   --------------------------------------------------------------------------- */
CREATE TABLE dbo.KURS_KS_Tarif
(
    id_tarifa   INT IDENTITY(1,1) NOT NULL,
    id_zony     INT               NOT NULL,
    ves_ot      DECIMAL(6,2)      NOT NULL,
    ves_do      DECIMAL(6,2)      NOT NULL,
    cena        DECIMAL(10,2)     NOT NULL,
    CONSTRAINT PK_KS_Tarif PRIMARY KEY (id_tarifa),
    CONSTRAINT UQ_KS_Tarif UNIQUE (id_zony, ves_ot, ves_do),
    CONSTRAINT FK_KS_Tarif_Zona FOREIGN KEY (id_zony)
        REFERENCES dbo.KURS_KS_Zona (id_zony),
    CONSTRAINT CK_KS_Tarif_Ves CHECK (ves_ot >= 0 AND ves_do > ves_ot),
    CONSTRAINT CK_KS_Tarif_Cena CHECK (cena >= 0)
);
GO

/* ---------------------------------------------------------------------------
   2.7. Статусы заказа
   --------------------------------------------------------------------------- */
CREATE TABLE dbo.KURS_KS_Status_Zakaza
(
    id_statusa  INT IDENTITY(1,1) NOT NULL,
    nazvanie    NVARCHAR(50)      NOT NULL,
    CONSTRAINT PK_KS_Status_Zakaza PRIMARY KEY (id_statusa),
    CONSTRAINT UQ_KS_Status_Zakaza_Nazvanie UNIQUE (nazvanie)
);
GO

/* ---------------------------------------------------------------------------
   2.8. Заказы
   --------------------------------------------------------------------------- */
CREATE TABLE dbo.KURS_KS_Zakaz
(
    id_zakaza              INT IDENTITY(1,1) NOT NULL,
    nomer                  NVARCHAR(30)      NOT NULL,
    id_klienta             INT               NOT NULL,
    adres_otpravitelya     NVARCHAR(250)     NOT NULL,
    fio_poluchatelya       NVARCHAR(150)     NOT NULL,
    telefon_poluchatelya   NVARCHAR(30)      NOT NULL,
    adres_poluchatelya     NVARCHAR(250)     NOT NULL,
    ves                    DECIMAL(6,2)      NOT NULL,
    id_zony                INT               NOT NULL,
    id_tarifa              INT               NULL,
    cena                   DECIMAL(10,2)     NOT NULL,
    id_statusa             INT               NOT NULL,
    id_kuriera             INT               NULL,
    data_sozdaniya         DATETIME2(0)      NOT NULL CONSTRAINT DF_KS_Zakaz_Data DEFAULT (SYSDATETIME()),
    data_dostavki          DATETIME2(0)      NULL,
    kommentariy            NVARCHAR(500)     NULL,
    sposob_oplaty          NVARCHAR(30)      NULL CONSTRAINT DF_KS_Zakaz_Oplata DEFAULT (N'Наличные'),
    data_izmeneniya        DATETIME2(0)      NOT NULL CONSTRAINT DF_KS_Zakaz_Izmenenie DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_KS_Zakaz PRIMARY KEY (id_zakaza),
    CONSTRAINT UQ_KS_Zakaz_Nomer UNIQUE (nomer),
    CONSTRAINT FK_KS_Zakaz_Klient FOREIGN KEY (id_klienta)
        REFERENCES dbo.KURS_KS_Klient (id_klienta),
    CONSTRAINT FK_KS_Zakaz_Zona FOREIGN KEY (id_zony)
        REFERENCES dbo.KURS_KS_Zona (id_zony),
    CONSTRAINT FK_KS_Zakaz_Tarif FOREIGN KEY (id_tarifa)
        REFERENCES dbo.KURS_KS_Tarif (id_tarifa),
    CONSTRAINT FK_KS_Zakaz_Status FOREIGN KEY (id_statusa)
        REFERENCES dbo.KURS_KS_Status_Zakaza (id_statusa),
    CONSTRAINT FK_KS_Zakaz_Kurier FOREIGN KEY (id_kuriera)
        REFERENCES dbo.KURS_KS_Kurier (id_kuriera),
    CONSTRAINT CK_KS_Zakaz_Ves CHECK (ves > 0 AND ves <= 1000),
    CONSTRAINT CK_KS_Zakaz_Cena CHECK (cena >= 0),
    CONSTRAINT CK_KS_Zakaz_Oplata CHECK (sposob_oplaty IS NULL OR sposob_oplaty IN (N'Наличные', N'Карта', N'Онлайн')),
    CONSTRAINT CK_KS_Zakaz_DataDostavki CHECK (data_dostavki IS NULL OR data_dostavki >= data_sozdaniya)
);
GO

CREATE INDEX IX_KS_Zakaz_Status ON dbo.KURS_KS_Zakaz (id_statusa);
CREATE INDEX IX_KS_Zakaz_Kurier ON dbo.KURS_KS_Zakaz (id_kuriera);
CREATE INDEX IX_KS_Zakaz_Data ON dbo.KURS_KS_Zakaz (data_sozdaniya);
CREATE INDEX IX_KS_Zakaz_Klient ON dbo.KURS_KS_Zakaz (id_klienta);
CREATE INDEX IX_KS_Klient_Fio ON dbo.KURS_KS_Klient (fio);
CREATE INDEX IX_KS_Kurier_Fio ON dbo.KURS_KS_Kurier (fio);
GO

/* ---------------------------------------------------------------------------
   2.9. История изменения статусов заказа
   --------------------------------------------------------------------------- */
CREATE TABLE dbo.KURS_KS_Istoriya_Statusov
(
    id_zapisi       INT IDENTITY(1,1) NOT NULL,
    id_zakaza       INT               NOT NULL,
    id_statusa      INT               NOT NULL,
    id_polzovatelya INT               NULL,
    data_izmeneniya DATETIME2(0)      NOT NULL CONSTRAINT DF_KS_Istoriya_Data DEFAULT (SYSDATETIME()),
    kommentariy     NVARCHAR(300)     NULL,
    CONSTRAINT PK_KS_Istoriya PRIMARY KEY (id_zapisi),
    CONSTRAINT FK_KS_Istoriya_Zakaz FOREIGN KEY (id_zakaza)
        REFERENCES dbo.KURS_KS_Zakaz (id_zakaza) ON DELETE CASCADE,
    CONSTRAINT FK_KS_Istoriya_Status FOREIGN KEY (id_statusa)
        REFERENCES dbo.KURS_KS_Status_Zakaza (id_statusa),
    CONSTRAINT FK_KS_Istoriya_Polzovatel FOREIGN KEY (id_polzovatelya)
        REFERENCES dbo.KURS_KS_Polzovatel (id_polzovatelya)
);
GO

CREATE INDEX IX_KS_Istoriya_Zakaz ON dbo.KURS_KS_Istoriya_Statusov (id_zakaza, data_izmeneniya);
GO

/* ---------------------------------------------------------------------------
   2.10. Журнал аудита
   --------------------------------------------------------------------------- */
CREATE TABLE dbo.KURS_KS_Audit
(
    id_zapisi     INT IDENTITY(1,1) NOT NULL,
    login         NVARCHAR(50)      NULL,
    deystvie      NVARCHAR(200)     NOT NULL,
    tablica       NVARCHAR(100)     NULL,
    id_obekta     INT               NULL,
    data_deystviya DATETIME2(0)     NOT NULL CONSTRAINT DF_KS_Audit_Data DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_KS_Audit PRIMARY KEY (id_zapisi)
);
GO

CREATE INDEX IX_KS_Audit_Data ON dbo.KURS_KS_Audit (data_deystviya);
GO

/* ---------------------------------------------------------------------------
   2.11. Последовательность для номеров заказов
   --------------------------------------------------------------------------- */
CREATE SEQUENCE dbo.KURS_KS_Seq_Zakaz
    AS INT
    START WITH 1
    INCREMENT BY 1
    CACHE 50;
GO

PRINT N'=== 3. Представления ===';
GO

/* Журнал заказов с расшифровкой справочников */
CREATE VIEW dbo.VW_KS_Zakazy
AS
SELECT
    z.id_zakaza,
    z.nomer,
    z.id_klienta,
    k.fio                     AS klient,
    k.telefon                 AS telefon_klienta,
    z.adres_otpravitelya,
    z.fio_poluchatelya,
    z.telefon_poluchatelya,
    z.adres_poluchatelya,
    z.ves,
    zn.nazvanie               AS zona,
    z.id_zony,
    z.id_tarifa,
    t.cena                    AS cena_tarifa,
    z.cena,
    s.nazvanie                AS status,
    z.id_statusa,
    ku.fio                    AS kurier,
    z.id_kuriera,
    z.data_sozdaniya,
    z.data_dostavki,
    z.kommentariy,
    z.sposob_oplaty,
    z.data_izmeneniya
FROM dbo.KURS_KS_Zakaz z
JOIN dbo.KURS_KS_Klient k          ON k.id_klienta = z.id_klienta
JOIN dbo.KURS_KS_Zona zn           ON zn.id_zony = z.id_zony
JOIN dbo.KURS_KS_Status_Zakaza s   ON s.id_statusa = z.id_statusa
LEFT JOIN dbo.KURS_KS_Tarif t      ON t.id_tarifa = z.id_tarifa
LEFT JOIN dbo.KURS_KS_Kurier ku    ON ku.id_kuriera = z.id_kuriera;
GO

/* Только доставленные заказы */
CREATE VIEW dbo.VW_KS_Dostavki
AS
SELECT *
FROM dbo.VW_KS_Zakazy
WHERE status = N'Доставлен';
GO

/* Загрузка курьеров */
CREATE VIEW dbo.VW_KS_Zagruzka_Kurierov
AS
SELECT
    ku.id_kuriera,
    ku.fio,
    ku.telefon,
    ku.transport,
    zn.nazvanie AS zona,
    ku.status,
    ku.is_active,
    COUNT(z.id_zakaza) AS vsego_zakazov,
    ISNULL(SUM(CASE WHEN s.nazvanie = N'Доставлен' THEN 1 ELSE 0 END), 0) AS dostavleno,
    ISNULL(SUM(CASE WHEN s.nazvanie IN (N'Назначен', N'В пути') THEN 1 ELSE 0 END), 0) AS v_rabote,
    ISNULL(SUM(CASE WHEN s.nazvanie = N'Доставлен' THEN z.cena ELSE 0 END), 0) AS summa_dostavki
FROM dbo.KURS_KS_Kurier ku
JOIN dbo.KURS_KS_Zona zn ON zn.id_zony = ku.id_zony
LEFT JOIN dbo.KURS_KS_Zakaz z ON z.id_kuriera = ku.id_kuriera
LEFT JOIN dbo.KURS_KS_Status_Zakaza s ON s.id_statusa = z.id_statusa
GROUP BY ku.id_kuriera, ku.fio, ku.telefon, ku.transport, zn.nazvanie, ku.status, ku.is_active;
GO

/* Выручка по зонам и месяцам */
CREATE VIEW dbo.VW_KS_Vyruchka_Po_Zonam
AS
SELECT
    zn.nazvanie AS zona,
    FORMAT(z.data_sozdaniya, 'yyyy-MM') AS period,
    COUNT(*) AS zakazov,
    SUM(z.cena) AS vyruchka
FROM dbo.KURS_KS_Zakaz z
JOIN dbo.KURS_KS_Zona zn ON zn.id_zony = z.id_zony
JOIN dbo.KURS_KS_Status_Zakaza s ON s.id_statusa = z.id_statusa
WHERE s.nazvanie <> N'Отменён'
GROUP BY zn.nazvanie, FORMAT(z.data_sozdaniya, 'yyyy-MM');
GO

PRINT N'=== 4. Хранимые процедуры ===';
GO

/* ---------------------------------------------------------------------------
   4.1. Создание заказа с автоматическим подбором тарифа и номера
   --------------------------------------------------------------------------- */
CREATE PROCEDURE dbo.sp_KS_Sozdat_Zakaz
    @id_klienta           INT,
    @adres_otpravitelya   NVARCHAR(250),
    @fio_poluchatelya     NVARCHAR(150),
    @telefon_poluchatelya NVARCHAR(30),
    @adres_poluchatelya   NVARCHAR(250),
    @ves                  DECIMAL(6,2),
    @id_zony              INT,
    @kommentariy          NVARCHAR(500) = NULL,
    @sposob_oplaty        NVARCHAR(30) = N'Наличные',
    @id_polzovatelya      INT = NULL,
    @id_zakaza            INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ves IS NULL OR @ves <= 0
        THROW 50001, N'Вес заказа должен быть больше нуля.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.KURS_KS_Klient WHERE id_klienta = @id_klienta AND is_active = 1)
        THROW 50002, N'Клиент не найден или заблокирован.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.KURS_KS_Zona WHERE id_zony = @id_zony)
        THROW 50003, N'Зона обслуживания не найдена.', 1;

    DECLARE @id_tarifa INT;
    DECLARE @cena DECIMAL(10,2);

    SELECT TOP (1)
        @id_tarifa = id_tarifa,
        @cena = cena
    FROM dbo.KURS_KS_Tarif
    WHERE id_zony = @id_zony
      AND @ves BETWEEN ves_ot AND ves_do
    ORDER BY ves_ot;

    IF @id_tarifa IS NULL
        THROW 50004, N'Для указанной зоны и веса не найден тариф.', 1;

    DECLARE @id_statusa INT =
        (SELECT id_statusa FROM dbo.KURS_KS_Status_Zakaza WHERE nazvanie = N'Новый');

    DECLARE @nomer NVARCHAR(30) =
        CONCAT(N'K-', FORMAT(SYSDATETIME(), N'yyyyMMdd'), N'-',
               RIGHT(N'0000' + CAST(NEXT VALUE FOR dbo.KURS_KS_Seq_Zakaz AS NVARCHAR(10)), 4));

    BEGIN TRANSACTION;

    INSERT INTO dbo.KURS_KS_Zakaz
    (
        nomer, id_klienta, adres_otpravitelya, fio_poluchatelya,
        telefon_poluchatelya, adres_poluchatelya, ves, id_zony,
        id_tarifa, cena, id_statusa, id_kuriera, kommentariy, sposob_oplaty
    )
    VALUES
    (
        @nomer, @id_klienta, @adres_otpravitelya, @fio_poluchatelya,
        @telefon_poluchatelya, @adres_poluchatelya, @ves, @id_zony,
        @id_tarifa, @cena, @id_statusa, NULL, @kommentariy, @sposob_oplaty
    );

    SET @id_zakaza = SCOPE_IDENTITY();

    INSERT INTO dbo.KURS_KS_Istoriya_Statusov
        (id_zakaza, id_statusa, id_polzovatelya, kommentariy)
    VALUES
        (@id_zakaza, @id_statusa, @id_polzovatelya, N'Заказ создан');

    INSERT INTO dbo.KURS_KS_Audit (login, deystvie, tablica, id_obekta)
    SELECT p.login, N'Создан заказ ' + @nomer, N'KURS_KS_Zakaz', @id_zakaza
    FROM dbo.KURS_KS_Polzovatel p
    WHERE p.id_polzovatelya = @id_polzovatelya;

    COMMIT TRANSACTION;

    SELECT @id_zakaza AS id_zakaza, @nomer AS nomer, @id_tarifa AS id_tarifa, @cena AS cena;
END;
GO

/* ---------------------------------------------------------------------------
   4.2. Назначение курьера на заказ
   --------------------------------------------------------------------------- */
CREATE PROCEDURE dbo.sp_KS_Naznachit_Kuriera
    @id_zakaza       INT,
    @id_kuriera      INT,
    @id_polzovatelya INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @status_tekushiy NVARCHAR(50);
    DECLARE @status_naznachen INT =
        (SELECT id_statusa FROM dbo.KURS_KS_Status_Zakaza WHERE nazvanie = N'Назначен');

    SELECT @status_tekushiy = s.nazvanie
    FROM dbo.KURS_KS_Zakaz z
    JOIN dbo.KURS_KS_Status_Zakaza s ON s.id_statusa = z.id_statusa
    WHERE z.id_zakaza = @id_zakaza;

    IF @status_tekushiy IS NULL
        THROW 50010, N'Заказ не найден.', 1;

    IF @status_tekushiy IN (N'Доставлен', N'Отменён')
        THROW 50011, N'Нельзя назначить курьера на завершённый или отменённый заказ.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.KURS_KS_Kurier WHERE id_kuriera = @id_kuriera AND is_active = 1)
        THROW 50012, N'Курьер не найден или неактивен.', 1;

    IF EXISTS (SELECT 1 FROM dbo.KURS_KS_Kurier WHERE id_kuriera = @id_kuriera AND status = N'Отдыхает')
        THROW 50013, N'Курьер находится на отдыхе.', 1;

    /* Нельзя назначить курьера, который уже занят другим заказом */
    IF EXISTS (SELECT 1 FROM dbo.KURS_KS_Kurier WHERE id_kuriera = @id_kuriera AND status = N'Занят')
       AND NOT EXISTS (SELECT 1 FROM dbo.KURS_KS_Zakaz WHERE id_zakaza = @id_zakaza AND id_kuriera = @id_kuriera)
        THROW 50014, N'Курьер уже занят другим заказом.', 1;

    BEGIN TRANSACTION;

    UPDATE dbo.KURS_KS_Zakaz
    SET id_kuriera = @id_kuriera,
        id_statusa = @status_naznachen,
        data_izmeneniya = SYSDATETIME()
    WHERE id_zakaza = @id_zakaza;

    UPDATE dbo.KURS_KS_Kurier
    SET status = N'Занят'
    WHERE id_kuriera = @id_kuriera;

    INSERT INTO dbo.KURS_KS_Istoriya_Statusov
        (id_zakaza, id_statusa, id_polzovatelya, kommentariy)
    VALUES
        (@id_zakaza, @status_naznachen, @id_polzovatelya, N'Назначен курьер');

    INSERT INTO dbo.KURS_KS_Audit (login, deystvie, tablica, id_obekta)
    SELECT p.login, N'Назначен курьер на заказ №' + CAST(@id_zakaza AS NVARCHAR(10)),
           N'KURS_KS_Zakaz', @id_zakaza
    FROM dbo.KURS_KS_Polzovatel p
    WHERE p.id_polzovatelya = @id_polzovatelya;

    COMMIT TRANSACTION;
END;
GO

/* ---------------------------------------------------------------------------
   4.3. Изменение статуса заказа
   --------------------------------------------------------------------------- */
CREATE PROCEDURE dbo.sp_KS_Izmenit_Status
    @id_zakaza       INT,
    @id_statusa      INT,
    @id_polzovatelya INT = NULL,
    @kommentariy     NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.KURS_KS_Zakaz WHERE id_zakaza = @id_zakaza)
        THROW 50020, N'Заказ не найден.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.KURS_KS_Status_Zakaza WHERE id_statusa = @id_statusa)
        THROW 50021, N'Статус не найден.', 1;

    DECLARE @nazvanie NVARCHAR(50) =
        (SELECT nazvanie FROM dbo.KURS_KS_Status_Zakaza WHERE id_statusa = @id_statusa);

    DECLARE @id_kuriera INT =
        (SELECT id_kuriera FROM dbo.KURS_KS_Zakaz WHERE id_zakaza = @id_zakaza);

    BEGIN TRANSACTION;

    UPDATE dbo.KURS_KS_Zakaz
    SET id_statusa = @id_statusa,
        data_dostavki = CASE WHEN @nazvanie = N'Доставлен' THEN SYSDATETIME() ELSE data_dostavki END,
        data_izmeneniya = SYSDATETIME()
    WHERE id_zakaza = @id_zakaza;

    IF @nazvanie IN (N'Доставлен', N'Отменён') AND @id_kuriera IS NOT NULL
    BEGIN
        UPDATE dbo.KURS_KS_Kurier
        SET status = CASE
            WHEN EXISTS
            (
                SELECT 1
                FROM dbo.KURS_KS_Zakaz z
                JOIN dbo.KURS_KS_Status_Zakaza s ON s.id_statusa = z.id_statusa
                WHERE z.id_kuriera = @id_kuriera
                  AND s.nazvanie IN (N'Назначен', N'В пути')
            ) THEN N'Занят' ELSE N'Свободен'
        END
        WHERE id_kuriera = @id_kuriera;
    END;

    INSERT INTO dbo.KURS_KS_Istoriya_Statusov
        (id_zakaza, id_statusa, id_polzovatelya, kommentariy)
    VALUES
        (@id_zakaza, @id_statusa, @id_polzovatelya, @kommentariy);

    INSERT INTO dbo.KURS_KS_Audit (login, deystvie, tablica, id_obekta)
    SELECT p.login, N'Статус заказа №' + CAST(@id_zakaza AS NVARCHAR(10)) + N' изменён на «' + @nazvanie + N'»',
           N'KURS_KS_Zakaz', @id_zakaza
    FROM dbo.KURS_KS_Polzovatel p
    WHERE p.id_polzovatelya = @id_polzovatelya;

    COMMIT TRANSACTION;
END;
GO

/* ---------------------------------------------------------------------------
   4.4. Отчёт о доставках за период
   --------------------------------------------------------------------------- */
CREATE PROCEDURE dbo.sp_KS_Otchet_Dostavki
    @data_ot DATE,
    @data_do DATE
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        z.nomer,
        z.klient,
        z.kurier,
        z.zona,
        z.ves,
        z.cena,
        z.sposob_oplaty,
        z.data_sozdaniya,
        z.data_dostavki
    FROM dbo.VW_KS_Zakazy z
    WHERE z.status = N'Доставлен'
      AND CAST(z.data_dostavki AS DATE) BETWEEN @data_ot AND @data_do
    ORDER BY z.data_dostavki DESC;
END;
GO

PRINT N'=== 5. Заполнение справочников и тестовых данных ===';
GO

/* Роли */
INSERT INTO dbo.KURS_KS_Rol (nazvanie) VALUES
    (N'Администратор'),
    (N'Менеджер'),
    (N'Курьер');
GO

/* Пользователи. Пароли: admin123 / manager123 / courier123.
   Хеш = SHA-256(соль + пароль) в кодировке UTF-16LE, как в приложении. */
INSERT INTO dbo.KURS_KS_Polzovatel (login, parol_hash, sol, id_roli, fio, telefon)
VALUES
(
    N'admin',
    CONVERT(CHAR(64), HASHBYTES('SHA2_256', N'a1b2c3d4e5f60718' + N'admin123'), 2),
    N'a1b2c3d4e5f60718',
    (SELECT id_roli FROM dbo.KURS_KS_Rol WHERE nazvanie = N'Администратор'),
    N'Ощепков Тимофей Сергеевич',
    N'+7 900 000-00-01'
),
(
    N'manager',
    CONVERT(CHAR(64), HASHBYTES('SHA2_256', N'b2c3d4e5f6071829' + N'manager123'), 2),
    N'b2c3d4e5f6071829',
    (SELECT id_roli FROM dbo.KURS_KS_Rol WHERE nazvanie = N'Менеджер'),
    N'Смирнова Анна Павловна',
    N'+7 900 000-00-02'
),
(
    N'courier',
    CONVERT(CHAR(64), HASHBYTES('SHA2_256', N'c3d4e5f60718293a' + N'courier123'), 2),
    N'c3d4e5f60718293a',
    (SELECT id_roli FROM dbo.KURS_KS_Rol WHERE nazvanie = N'Курьер'),
    N'Иванов Пётр Алексеевич',
    N'+7 900 000-00-03'
);
GO

/* Зоны обслуживания */
INSERT INTO dbo.KURS_KS_Zona (nazvanie, opisanie) VALUES
    (N'Центр', N'Исторический центр, улицы в пределах центральной части города'),
    (N'Северный', N'Северный жилой район и прилегающие микрорайоны'),
    (N'Южный', N'Южный район, включая промышленную зону'),
    (N'Заволжский', N'Заволжский район, левый берег Волги');
GO

/* Клиенты */
INSERT INTO dbo.KURS_KS_Klient (fio, telefon, email, adres) VALUES
    (N'Смирнова Анна Павловна',      N'+7 901 111-11-11', N'anna.smirnova@mail.ru',    N'г. Ярославль, ул. Свободы, 10'),
    (N'Кузнецов Дмитрий Олегович',   N'+7 901 222-22-22', N'kuznetsov@mail.ru',        N'г. Ярославль, ул. Чехова, 25'),
    (N'Волкова Ирина Михайловна',    N'+7 901 333-33-33', N'volkova@mail.ru',          N'г. Ярославль, ул. Победы, 8'),
    (N'Морозов Сергей Викторович',   N'+7 901 444-44-44', N'morozov@mail.ru',          N'г. Ярославль, пр. Ленина, 42'),
    (N'Фёдорова Ольга Дмитриевна',   N'+7 901 555-55-55', N'fedorova@mail.ru',         N'г. Ярославль, ул. Суркова, 14'),
    (N'Никитин Артём Андреевич',     N'+7 901 666-66-66', N'nikitin@mail.ru',          N'г. Ярославль, ул. Бабича, 7'),
    (N'Егорова Мария Сергеевна',     N'+7 901 777-77-77', N'egorova@mail.ru',          N'г. Ярославль, ул. Рыбинская, 30'),
    (N'Павлов Илья Николаевич',      N'+7 901 888-88-88', N'pavlov@mail.ru',           N'г. Ярославль, ул. Калинина, 19'),
    (N'Соколова Дарья Игоревна',     N'+7 901 999-99-99', N'sokolova@mail.ru',         N'г. Ярославль, ул. Некрасова, 3'),
    (N'Тихонов Максим Олегович',     N'+7 902 111-11-11', N'tikhonov@mail.ru',         N'г. Ярославль, ул. Пушкина, 11');
GO

/* Курьеры */
INSERT INTO dbo.KURS_KS_Kurier (fio, telefon, transport, id_zony, status, is_active, data_priema) VALUES
    (N'Иванов Пётр Алексеевич',      N'+7 900 111-22-33', N'Автомобиль', (SELECT id_zony FROM dbo.KURS_KS_Zona WHERE nazvanie = N'Центр'),      N'Свободен', 1, DATEADD(MONTH, -8, CAST(SYSDATETIME() AS DATE))),
    (N'Петров Алексей Игоревич',     N'+7 900 222-33-44', N'Мотоцикл',   (SELECT id_zony FROM dbo.KURS_KS_Zona WHERE nazvanie = N'Северный'),   N'Свободен', 1, DATEADD(MONTH, -5, CAST(SYSDATETIME() AS DATE))),
    (N'Соколов Андрей Викторович',   N'+7 900 333-44-55', N'Велосипед',  (SELECT id_zony FROM dbo.KURS_KS_Zona WHERE nazvanie = N'Южный'),     N'Занят',    1, DATEADD(MONTH, -3, CAST(SYSDATETIME() AS DATE))),
    (N'Белова Екатерина Андреевна',  N'+7 900 444-55-66', N'Пешком',     (SELECT id_zony FROM dbo.KURS_KS_Zona WHERE nazvanie = N'Центр'),      N'Занят',    1, DATEADD(MONTH, -2, CAST(SYSDATETIME() AS DATE))),
    (N'Громов Никита Сергеевич',     N'+7 900 555-66-77', N'Автомобиль', (SELECT id_zony FROM dbo.KURS_KS_Zona WHERE nazvanie = N'Заволжский'), N'Свободен', 1, DATEADD(MONTH, -1, CAST(SYSDATETIME() AS DATE))),
    (N'Кириллова Ольга Павловна',    N'+7 900 666-77-88', N'Велосипед',  (SELECT id_zony FROM dbo.KURS_KS_Zona WHERE nazvanie = N'Северный'),   N'Свободен', 1, DATEADD(DAY, -12, CAST(SYSDATETIME() AS DATE)));
GO

/* Привязка учётной записи courier к первому курьеру */
UPDATE dbo.KURS_KS_Kurier
SET id_polzovatelya = (SELECT id_polzovatelya FROM dbo.KURS_KS_Polzovatel WHERE login = N'courier')
WHERE telefon = N'+7 900 111-22-33';
GO

/* Тарифы: 4 зоны * 3 диапазона веса = 12 строк */
INSERT INTO dbo.KURS_KS_Tarif (id_zony, ves_ot, ves_do, cena)
SELECT z.id_zony, v.ves_ot, v.ves_do,
       CASE z.nazvanie
           WHEN N'Центр'     THEN v.cena * 1.00
           WHEN N'Северный'  THEN v.cena * 0.95
           WHEN N'Южный'     THEN v.cena * 0.90
           ELSE                   v.cena * 1.10
       END
FROM dbo.KURS_KS_Zona z
CROSS JOIN (VALUES
    (CAST(0.00 AS DECIMAL(6,2)),  CAST(1.00 AS DECIMAL(6,2)),  CAST(300.00 AS DECIMAL(10,2))),
    (CAST(1.01 AS DECIMAL(6,2)),  CAST(5.00 AS DECIMAL(6,2)),  CAST(450.00 AS DECIMAL(10,2))),
    (CAST(5.01 AS DECIMAL(6,2)),  CAST(20.00 AS DECIMAL(6,2)), CAST(700.00 AS DECIMAL(10,2)))
) v (ves_ot, ves_do, cena);
GO

/* Статусы заказов */
INSERT INTO dbo.KURS_KS_Status_Zakaza (nazvanie) VALUES
    (N'Новый'),
    (N'Принят'),
    (N'Назначен'),
    (N'В пути'),
    (N'Доставлен'),
    (N'Отменён');
GO

/* ---------------------------------------------------------------------------
   Заказы. 24 записи: новые, в работе, доставленные и отменённые.
   Идентификаторы клиентов/зон/курьеров/статусов соответствуют порядку
   вставки выше (свежая база).
   --------------------------------------------------------------------------- */
DECLARE @z1 INT = (SELECT id_zony FROM dbo.KURS_KS_Zona WHERE nazvanie = N'Центр');
DECLARE @z2 INT = (SELECT id_zony FROM dbo.KURS_KS_Zona WHERE nazvanie = N'Северный');
DECLARE @z3 INT = (SELECT id_zony FROM dbo.KURS_KS_Zona WHERE nazvanie = N'Южный');
DECLARE @z4 INT = (SELECT id_zony FROM dbo.KURS_KS_Zona WHERE nazvanie = N'Заволжский');

DECLARE @s1 INT = (SELECT id_statusa FROM dbo.KURS_KS_Status_Zakaza WHERE nazvanie = N'Новый');
DECLARE @s2 INT = (SELECT id_statusa FROM dbo.KURS_KS_Status_Zakaza WHERE nazvanie = N'Принят');
DECLARE @s3 INT = (SELECT id_statusa FROM dbo.KURS_KS_Status_Zakaza WHERE nazvanie = N'Назначен');
DECLARE @s4 INT = (SELECT id_statusa FROM dbo.KURS_KS_Status_Zakaza WHERE nazvanie = N'В пути');
DECLARE @s5 INT = (SELECT id_statusa FROM dbo.KURS_KS_Status_Zakaza WHERE nazvanie = N'Доставлен');
DECLARE @s6 INT = (SELECT id_statusa FROM dbo.KURS_KS_Status_Zakaza WHERE nazvanie = N'Отменён');

DECLARE @Orders TABLE
(
    nomer                NVARCHAR(30),
    id_klienta           INT,
    adres_otpravitelya   NVARCHAR(250),
    fio_poluchatelya     NVARCHAR(150),
    telefon_poluchatelya NVARCHAR(30),
    adres_poluchatelya   NVARCHAR(250),
    ves                  DECIMAL(6,2),
    id_zony              INT,
    id_statusa           INT,
    id_kuriera           INT,
    data_sozdaniya       DATETIME2(0),
    data_dostavki        DATETIME2(0),
    kommentariy          NVARCHAR(500),
    sposob_oplaty        NVARCHAR(30)
);

INSERT INTO @Orders VALUES
(N'K-20261018-0001', 1, N'г. Ярославль, ул. Свободы, 10',      N'Смирнов Александр Петрович',  N'+7 905 111-22-33', N'г. Ярославль, ул. Кирова, 5',       0.80, @z1, @s5, 1, DATEADD(DAY, -20, SYSDATETIME()), DATEADD(HOUR,  3, DATEADD(DAY, -20, SYSDATETIME())), N'Доставлено вовремя',            N'Карта'),
(N'K-20261018-0002', 2, N'г. Ярославль, ул. Чехова, 25',       N'Кузнецова Ольга Ивановна',    N'+7 905 222-33-44', N'г. Ярославль, ул. Республиканская, 12', 1.20, @z2, @s5, 2, DATEADD(DAY, -20, SYSDATETIME()), DATEADD(HOUR,  5, DATEADD(DAY, -20, SYSDATETIME())), N'Хрупкий груз',                  N'Онлайн'),
(N'K-20261019-0003', 3, N'г. Ярославль, ул. Победы, 8',        N'Волков Михаил Сергеевич',     N'+7 905 333-44-55', N'г. Ярославль, ул. Малая Пролетарская, 7', 2.40, @z3, @s5, 3, DATEADD(DAY, -19, SYSDATETIME()), DATEADD(HOUR,  4, DATEADD(DAY, -19, SYSDATETIME())), N'Позвонить за 30 минут',        N'Наличные'),
(N'K-20261019-0004', 4, N'г. Ярославль, пр. Ленина, 42',       N'Морозова Елена Викторовна',   N'+7 905 444-55-66', N'г. Ярославль, ул. Свердлова, 18',       0.45, @z1, @s5, 4, DATEADD(DAY, -18, SYSDATETIME()), DATEADD(HOUR,  2, DATEADD(DAY, -18, SYSDATETIME())), N'Документы',                     N'Карта'),
(N'K-20261020-0005', 5, N'г. Ярославль, ул. Суркова, 14',      N'Фёдоров Павел Андреевич',     N'+7 905 555-66-77', N'г. Ярославль, ул. Труфанова, 9',        3.10, @z2, @s5, 2, DATEADD(DAY, -17, SYSDATETIME()), DATEADD(HOUR,  6, DATEADD(DAY, -17, SYSDATETIME())), N'Крупногабарит',                 N'Онлайн'),
(N'K-20261020-0006', 6, N'г. Ярославль, ул. Бабича, 7',        N'Никитина Анна Артёмовна',     N'+7 905 666-77-88', N'г. Ярославль, ул. Ярославская, 24',     1.80, @z1, @s5, 1, DATEADD(DAY, -16, SYSDATETIME()), DATEADD(HOUR,  3, DATEADD(DAY, -16, SYSDATETIME())), N'Оставить у консьержа',          N'Наличные'),
(N'K-20261021-0007', 7, N'г. Ярославль, ул. Рыбинская, 30',    N'Егоров Сергей Михайлович',    N'+7 905 777-88-99', N'г. Ярославль, ул. Володарского, 15',    0.60, @z3, @s5, 3, DATEADD(DAY, -15, SYSDATETIME()), DATEADD(HOUR,  4, DATEADD(DAY, -15, SYSDATETIME())), N'Скоропортящийся груз',          N'Карта'),
(N'K-20261021-0008', 8, N'г. Ярославль, ул. Калинина, 19',     N'Павлова Ирина Николаевна',    N'+7 905 888-99-00', N'г. Ярославль, ул. Советская, 33',       2.00, @z4, @s5, 5, DATEADD(DAY, -14, SYSDATETIME()), DATEADD(HOUR,  7, DATEADD(DAY, -14, SYSDATETIME())), N'Доставка до подъезда',          N'Онлайн'),
(N'K-20261022-0009', 9, N'г. Ярославль, ул. Некрасова, 3',     N'Соколов Игорь Дмитриевич',    N'+7 905 999-00-11', N'г. Ярославль, ул. Собинова, 8',         0.30, @z1, @s5, 4, DATEADD(DAY, -13, SYSDATETIME()), DATEADD(HOUR,  2, DATEADD(DAY, -13, SYSDATETIME())), N'Цветы',                         N'Карта'),
(N'K-20261022-0010', 10, N'г. Ярославль, ул. Пушкина, 11',     N'Тихонова Мария Олеговна',     N'+7 906 111-22-33', N'г. Ярославль, ул. Комсомольская, 6',    4.50, @z2, @s5, 2, DATEADD(DAY, -12, SYSDATETIME()), DATEADD(HOUR,  5, DATEADD(DAY, -12, SYSDATETIME())), N'Бытовая техника',               N'Онлайн'),
(N'K-20261024-0011', 1, N'г. Ярославль, ул. Свободы, 10',      N'Смирнов Виктор Павлович',     N'+7 905 111-22-34', N'г. Ярославль, ул. Угличская, 21',       1.00, @z1, @s5, 1, DATEADD(DAY, -10, SYSDATETIME()), DATEADD(HOUR,  3, DATEADD(DAY, -10, SYSDATETIME())), N'Документы',                     N'Наличные'),
(N'K-20261025-0012', 2, N'г. Ярославль, ул. Чехова, 25',       N'Кузнецов Андрей Олегович',    N'+7 905 222-33-45', N'г. Ярославль, ул. Победы, 44',          6.20, @z3, @s5, 3, DATEADD(DAY, -9,  SYSDATETIME()), DATEADD(HOUR,  6, DATEADD(DAY, -9,  SYSDATETIME())), N'Мебель, 2 места',               N'Карта'),
(N'K-20261026-0013', 3, N'г. Ярославль, ул. Победы, 8',        N'Волкова Дарья Михайловна',    N'+7 905 333-44-56', N'г. Ярославль, ул. Кудрявцева, 4',       0.90, @z1, @s4, 4, DATEADD(HOUR, 15, DATEADD(DAY, -1, CAST(CAST(SYSDATETIME() AS date) AS datetime))), NULL, N'В пути, позвонить получателю', N'Онлайн'),
(N'K-20261026-0014', 4, N'г. Ярославль, пр. Ленина, 42',       N'Морозов Илья Викторович',     N'+7 905 444-55-67', N'г. Ярославль, ул. Салтыкова-Щедрина, 2', 2.70, @z2, @s3, 2, DATEADD(HOUR, 16, DATEADD(DAY, -1, CAST(CAST(SYSDATETIME() AS date) AS datetime))), NULL, N'Назначен курьер',               N'Карта'),
(N'K-20261027-0015', 5, N'г. Ярославль, ул. Суркова, 14',      N'Фёдорова Ольга Дмитриевна',   N'+7 905 555-66-78', N'г. Ярославль, ул. Панина, 17',          1.50, @z2, @s3, 6, DATEADD(MINUTE, -12, SYSDATETIME()), NULL, N'Доставить после 18:00',         N'Наличные'),
(N'K-20261027-0016', 6, N'г. Ярославль, ул. Бабича, 7',        N'Никитин Артём Андреевич',     N'+7 905 666-77-89', N'г. Ярославль, ул. Волгоградская, 12',   0.70, @z4, @s2, NULL, DATEADD(MINUTE, -10, SYSDATETIME()), NULL, N'Принят, ожидает назначения',   N'Онлайн'),
(N'K-20261027-0017', 7, N'г. Ярославль, ул. Рыбинская, 30',    N'Егорова Мария Сергеевна',     N'+7 905 777-88-90', N'г. Ярославль, ул. 3-я Яковлевская, 5',  8.40, @z3, @s1, NULL, DATEADD(MINUTE, -8, SYSDATETIME()), NULL, N'Новый заказ',                   N'Карта'),
(N'K-20261027-0018', 8, N'г. Ярославль, ул. Калинина, 19',     N'Павлов Илья Николаевич',      N'+7 905 888-99-01', N'г. Ярославль, ул. Титова, 8',           3.60, @z4, @s1, NULL, DATEADD(MINUTE, -6, SYSDATETIME()), NULL, N'Новый заказ',                   N'Наличные'),
(N'K-20261027-0019', 9, N'г. Ярославль, ул. Некрасова, 3',     N'Соколова Дарья Игоревна',     N'+7 905 999-00-12', N'г. Ярославль, ул. Свердлова, 25',       0.50, @z1, @s2, NULL, DATEADD(MINUTE, -4, SYSDATETIME()), NULL, N'Принят',                        N'Онлайн'),
(N'K-20261027-0020', 10, N'г. Ярославль, ул. Пушкина, 11',     N'Тихонов Максим Олегович',     N'+7 906 111-22-34', N'г. Ярославль, ул. Елены Колесовой, 10', 12.00, @z2, @s1, NULL, DATEADD(MINUTE, -2, SYSDATETIME()), NULL, N'Крупный груз',                  N'Карта'),
(N'K-20261023-0021', 3, N'г. Ярославль, ул. Победы, 8',        N'Волков Алексей Михайлович',   N'+7 905 333-44-57', N'г. Ярославль, ул. Чайковского, 40',     1.10, @z1, @s6, NULL, DATEADD(DAY, -11, SYSDATETIME()), NULL, N'Клиент отменил заказ',          N'Наличные'),
(N'K-20261024-0022', 5, N'г. Ярославль, ул. Суркова, 14',      N'Фёдоров Андрей Павлович',     N'+7 905 555-66-79', N'г. Ярославль, ул. Слепнёва, 20',        2.20, @z2, @s6, NULL, DATEADD(DAY, -10, SYSDATETIME()), NULL, N'Некорректный адрес',            N'Карта'),
(N'K-20261025-0023', 8, N'г. Ярославль, ул. Калинина, 19',     N'Павлова Ольга Николаевна',    N'+7 905 888-99-02', N'г. Ярославль, ул. Батова, 5',           0.40, @z4, @s5, 5, DATEADD(DAY,  -2, SYSDATETIME()), DATEADD(HOUR,  4, DATEADD(DAY,  -2, SYSDATETIME())), N'Доставлено',                    N'Онлайн'),
(N'K-20261026-0024', 9, N'г. Ярославль, ул. Некрасова, 3',     N'Соколов Дмитрий Игоревич',    N'+7 905 999-00-13', N'г. Ярославль, ул. Победы, 15',          1.70, @z1, @s5, 4, DATEADD(DAY,  -1, SYSDATETIME()), DATEADD(HOUR,  3, DATEADD(DAY,  -1, SYSDATETIME())), N'Доставлено',                    N'Карта');

INSERT INTO dbo.KURS_KS_Zakaz
(
    nomer, id_klienta, adres_otpravitelya, fio_poluchatelya, telefon_poluchatelya,
    adres_poluchatelya, ves, id_zony, id_tarifa, cena, id_statusa, id_kuriera,
    data_sozdaniya, data_dostavki, kommentariy, sposob_oplaty, data_izmeneniya
)
SELECT
    o.nomer, o.id_klienta, o.adres_otpravitelya, o.fio_poluchatelya, o.telefon_poluchatelya,
    o.adres_poluchatelya, o.ves, o.id_zony, t.id_tarifa, t.cena, o.id_statusa, o.id_kuriera,
    o.data_sozdaniya, o.data_dostavki, o.kommentariy, o.sposob_oplaty, o.data_sozdaniya
FROM @Orders o
JOIN dbo.KURS_KS_Tarif t
    ON t.id_zony = o.id_zony
   AND o.ves BETWEEN t.ves_ot AND t.ves_do;
GO

/* Пересчёт статусов курьеров по фактическим активным заказам */
UPDATE ku
SET status = CASE
    WHEN EXISTS
    (
        SELECT 1
        FROM dbo.KURS_KS_Zakaz z
        JOIN dbo.KURS_KS_Status_Zakaza s ON s.id_statusa = z.id_statusa
        WHERE z.id_kuriera = ku.id_kuriera
          AND s.nazvanie IN (N'Назначен', N'В пути')
    ) THEN N'Занят' ELSE N'Свободен'
END
FROM dbo.KURS_KS_Kurier ku;
GO

/* История статусов: создание + текущий статус */
INSERT INTO dbo.KURS_KS_Istoriya_Statusov (id_zakaza, id_statusa, id_polzovatelya, data_izmeneniya, kommentariy)
SELECT z.id_zakaza, s.id_statusa, NULL, z.data_sozdaniya, N'Заказ создан'
FROM dbo.KURS_KS_Zakaz z
JOIN dbo.KURS_KS_Status_Zakaza s ON s.nazvanie = N'Новый';
GO

INSERT INTO dbo.KURS_KS_Istoriya_Statusov (id_zakaza, id_statusa, id_polzovatelya, data_izmeneniya, kommentariy)
SELECT z.id_zakaza, z.id_statusa, 1, z.data_izmeneniya, N'Текущий статус заказа'
FROM dbo.KURS_KS_Zakaz z
WHERE z.id_statusa <> (SELECT id_statusa FROM dbo.KURS_KS_Status_Zakaza WHERE nazvanie = N'Новый');
GO

/* Несколько записей аудита для демонстрации */
INSERT INTO dbo.KURS_KS_Audit (login, deystvie, tablica, id_obekta, data_deystviya) VALUES
    (N'admin',   N'Первичное заполнение базы данных', NULL, NULL, DATEADD(DAY, -20, SYSDATETIME())),
    (N'admin',   N'Вход в систему',                   NULL, NULL, DATEADD(DAY, -1,  SYSDATETIME())),
    (N'manager', N'Вход в систему',                   NULL, NULL, DATEADD(HOUR, -6, SYSDATETIME())),
    (N'manager', N'Создан заказ K-20261027-0019',     N'KURS_KS_Zakaz', 19, DATEADD(HOUR, -3, SYSDATETIME())),
    (N'courier', N'Вход в систему',                   NULL, NULL, DATEADD(HOUR, -1, SYSDATETIME()));
GO

PRINT N'=== 6. Проверка результата ===';
GO

SELECT N'KURS_KS_Rol' AS [Таблица], COUNT(*) AS [Строк] FROM dbo.KURS_KS_Rol
UNION ALL SELECT N'KURS_KS_Polzovatel', COUNT(*) FROM dbo.KURS_KS_Polzovatel
UNION ALL SELECT N'KURS_KS_Zona', COUNT(*) FROM dbo.KURS_KS_Zona
UNION ALL SELECT N'KURS_KS_Klient', COUNT(*) FROM dbo.KURS_KS_Klient
UNION ALL SELECT N'KURS_KS_Kurier', COUNT(*) FROM dbo.KURS_KS_Kurier
UNION ALL SELECT N'KURS_KS_Tarif', COUNT(*) FROM dbo.KURS_KS_Tarif
UNION ALL SELECT N'KURS_KS_Status_Zakaza', COUNT(*) FROM dbo.KURS_KS_Status_Zakaza
UNION ALL SELECT N'KURS_KS_Zakaz', COUNT(*) FROM dbo.KURS_KS_Zakaz
UNION ALL SELECT N'KURS_KS_Istoriya_Statusov', COUNT(*) FROM dbo.KURS_KS_Istoriya_Statusov
UNION ALL SELECT N'KURS_KS_Audit', COUNT(*) FROM dbo.KURS_KS_Audit;
GO

PRINT N'=== ГОТОВО. База данных «Курьерская служба» создана. ===';
PRINT N'Учётные записи: admin/admin123, manager/manager123, courier/courier123';
GO


