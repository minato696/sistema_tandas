# Migración de Base de Datos: SQLite a PostgreSQL

## Resumen del Proyecto

Este documento describe la migración del sistema **Generador de Pautas** desde SQLite (archivo local/red) hacia PostgreSQL (servidor de base de datos).

---

## Configuración del Servidor PostgreSQL

### Datos de Conexión
| Parámetro | Valor |
|-----------|-------|
| **Host** | 192.168.10.188 |
| **Puerto** | 9134 |
| **Base de datos** | generador_pautas |
| **Usuario** | pautas_user |
| **Contraseña** | Pautas2024! |

### Ubicación del Servidor
- **Sistema Operativo**: Ubuntu Linux
- **PostgreSQL Version**: 16
- **Acceso SSH**: Puerto 5134, usuario root

---

## Estructura de la Base de Datos

### Tablas Creadas

#### 1. Comerciales (Tabla Principal)
```sql
CREATE TABLE Comerciales (
    Codigo VARCHAR(50) PRIMARY KEY,
    FilePath TEXT NOT NULL,
    FechaInicio TIMESTAMP NOT NULL,
    FechaFinal TIMESTAMP NOT NULL,
    Ciudad VARCHAR(100) NOT NULL,
    Radio VARCHAR(100) NOT NULL,
    Posicion VARCHAR(10) NOT NULL,
    Estado VARCHAR(20) NOT NULL DEFAULT 'Activo',
    TipoProgramacion VARCHAR(50) DEFAULT 'Cada 00-30'
);
```

#### 2. ComercialesAsignados
```sql
CREATE TABLE ComercialesAsignados (
    Id SERIAL PRIMARY KEY,
    Fila INTEGER NOT NULL,
    Columna INTEGER NOT NULL,
    ComercialAsignado TEXT NOT NULL,
    Codigo VARCHAR(50) NOT NULL REFERENCES Comerciales(Codigo) ON DELETE CASCADE
);
```

#### 3. Usuarios
```sql
CREATE TABLE Usuarios (
    Id SERIAL PRIMARY KEY,
    Usuario VARCHAR(100) NOT NULL UNIQUE,
    Contrasena VARCHAR(256) NOT NULL,
    Rol VARCHAR(50) NOT NULL DEFAULT 'Usuario',
    NombreCompleto VARCHAR(200),
    Estado VARCHAR(20) NOT NULL DEFAULT 'Activo',
    FechaCreacion TIMESTAMP NOT NULL
);
```

#### 4. Ciudades
```sql
CREATE TABLE Ciudades (
    Id SERIAL PRIMARY KEY,
    Nombre VARCHAR(100) NOT NULL UNIQUE,
    Estado VARCHAR(20) NOT NULL DEFAULT 'Activo'
);
```

#### 5. Radios
```sql
CREATE TABLE Radios (
    Id SERIAL PRIMARY KEY,
    Nombre VARCHAR(100) NOT NULL UNIQUE,
    Descripcion TEXT,
    Estado VARCHAR(20) NOT NULL DEFAULT 'Activo'
);
```

#### 6. RadiosCiudades (Relación N:N)
```sql
CREATE TABLE RadiosCiudades (
    Id SERIAL PRIMARY KEY,
    RadioId INTEGER NOT NULL REFERENCES Radios(Id) ON DELETE CASCADE,
    CiudadId INTEGER NOT NULL REFERENCES Ciudades(Id) ON DELETE CASCADE,
    Estado VARCHAR(20) NOT NULL DEFAULT 'Activo',
    UNIQUE(RadioId, CiudadId)
);
```

#### 7. TandasProgramacion
```sql
CREATE TABLE TandasProgramacion (
    Id SERIAL PRIMARY KEY,
    Nombre VARCHAR(100) NOT NULL UNIQUE,
    Horarios TEXT NOT NULL,
    Estado VARCHAR(20) NOT NULL DEFAULT 'Activo'
);
```

---

## Índices para Optimización

```sql
-- Índices en ComercialesAsignados
CREATE INDEX idx_comerciales_asignados_codigo ON ComercialesAsignados(Codigo);
CREATE INDEX idx_comerciales_asignados_fila_col ON ComercialesAsignados(Fila, Columna);
CREATE INDEX idx_comerciales_asignados_fila_col_codigo ON ComercialesAsignados(Fila, Columna, Codigo);

-- Índices en Comerciales
CREATE INDEX idx_comerciales_estado ON Comerciales(Estado);
CREATE INDEX idx_comerciales_fechas ON Comerciales(FechaInicio, FechaFinal);
CREATE INDEX idx_comerciales_ciudad_radio ON Comerciales(Ciudad, Radio);
CREATE INDEX idx_comerciales_consulta_principal ON Comerciales(Ciudad, Radio, Estado, FechaInicio, FechaFinal);
CREATE INDEX idx_comerciales_posicion ON Comerciales(Posicion);
CREATE INDEX idx_comerciales_ciudad_radio_posicion ON Comerciales(Ciudad, Radio, Posicion);
CREATE INDEX idx_comerciales_filepath ON Comerciales(FilePath);
```

---

## Archivos Modificados en C#

### Archivos Convertidos de SQLite a PostgreSQL

| Archivo | Descripción |
|---------|-------------|
| `DataAccess.cs` | Acceso general a datos |
| `DatabaseService.cs` | Servicio de base de datos |
| `SQLiteDataAccess.cs` | Ahora usa `PostgreSQLDataAccess` |
| `SQLiteDatabaseService.cs` | Ahora usa `PostgreSQLDatabaseService` |
| `SQLiteMigration.cs` | Ahora usa `PostgreSQLMigration` (con alias para compatibilidad) |
| `UserManager.cs` | Gestión de usuarios |
| `ConfigManager.cs` | Configuración del sistema |
| `Form1.cs` | Formulario principal |
| `AdminCiudadesForm.cs` | Administración de ciudades |
| `AdminTandasForm.cs` | Administración de tandas |
| `AdminRadiosForm.cs` | Administración de radios |
| `ImportadorExcelForm.cs` | Importador de Excel |
| `ReportesForm.cs` | Formulario de reportes |
| `ReportesService.cs` | Servicio de reportes |
| `DashboardControl.cs` | Control del dashboard |
| `DashboardStats.cs` | Estadísticas del dashboard |
| `GenerarPauta.cs` | Generación de pautas |
| `Agregar_Comerciales.cs` | Agregar comerciales |

### Cambios de Sintaxis SQL

| SQLite | PostgreSQL |
|--------|------------|
| `date(columna)` | `columna::date` |
| `INSERT OR IGNORE` | `INSERT ... ON CONFLICT DO NOTHING` |
| `GLOB '[0-9]*'` | `~ '^[0-9]+$'` |
| `rowid` | `ctid` |
| `INTEGER PRIMARY KEY AUTOINCREMENT` | `SERIAL PRIMARY KEY` |
| `PRAGMA foreign_keys = ON;` | No necesario |

---

## Configuración en config.ini

```ini
[Database]
; Tipo de base de datos: SQLite o PostgreSQL
Type=PostgreSQL

[PostgreSQL]
; Configuracion de PostgreSQL
Host=192.168.10.188
Port=9134
Database=generador_pautas
Username=pautas_user
Password=Pautas2024!
```

---

## Paquetes NuGet Utilizados

| Paquete | Versión | Descripción |
|---------|---------|-------------|
| **Npgsql** | 4.1.13 | Driver PostgreSQL para .NET Framework 4.8 |
| System.Buffers | 4.5.1 | Dependencia de Npgsql |
| System.Memory | 4.5.4 | Dependencia de Npgsql |
| System.Numerics.Vectors | 4.5.0 | Dependencia de Npgsql |
| System.Runtime.CompilerServices.Unsafe | 4.7.1 | Dependencia de Npgsql |
| System.Threading.Tasks.Extensions | 4.5.4 | Dependencia de Npgsql |
| System.ValueTuple | 4.5.0 | Dependencia de Npgsql |

> **IMPORTANTE**: Npgsql 4.1.13 es la última versión compatible con .NET Framework 4.8.
> Las versiones 5.x y superiores requieren .NET 5/6/7/8.

---

## Usuarios por Defecto

| Usuario | Contraseña | Rol |
|---------|------------|-----|
| admin | admin123 | Administrador |
| usuario | usuario123 | Usuario |

---

## Radios por Defecto

- EXITOSA
- KARIBEÑA
- LAKALLE

---

## Ciudades por Defecto

ABANCAY, ANDAHUAYLAS, AYACUCHO, BARRANCA, CAJAMARCA, CAÑETE, CERRO DE PASCO,
CHACHAPOYAS, CHICLAYO, CHIMBOTE, CHINCHA, CHULUCANAS, CUSCO, HUACHO, HUANCABAMBA,
HUANCAVELICA, HUANUCO, HUARAL, HUARAZ, HUARMEY, ILO, JAEN, JAUJA, JULIACA, LIMA,
LOS ORGANOS, MOLLENDO, MOQUEGUA, MOYOBAMBA, PACASMAYO, PAITA, PISCO, PIURA, PUCALLPA,
PUNO, PUERTO MALDONADO, SULLANA, TACNA, TALARA, TARAPOTO, TINGO MARIA, TRUJILLO,
TUMBES, VENTANILLA, YURIMAGUAS

---

## Comandos Útiles en PostgreSQL

### Conectar al servidor
```bash
psql -h 192.168.10.188 -p 9134 -U pautas_user -d generador_pautas
```

### Ver tablas
```sql
\dt
```

### Ver estructura de una tabla
```sql
\d Comerciales
```

### Contar registros
```sql
SELECT COUNT(*) FROM Comerciales;
SELECT COUNT(*) FROM ComercialesAsignados;
```

### Ver tamaño de la base de datos
```sql
SELECT pg_size_pretty(pg_database_size('generador_pautas'));
```

### Ver conexiones activas
```sql
SELECT count(*) FROM pg_stat_activity WHERE datname = 'generador_pautas';
```

---

## Ventajas de PostgreSQL sobre SQLite en Red

1. **Mejor rendimiento en red**: Conexiones directas al servidor, sin transferir archivos
2. **Concurrencia real**: Múltiples usuarios pueden escribir simultáneamente
3. **Sin bloqueos de archivo**: No hay problemas de "archivo en uso"
4. **Escalabilidad**: Soporta millones de registros sin degradación
5. **Transacciones ACID completas**: Mayor integridad de datos
6. **Connection pooling**: Reutilización eficiente de conexiones

---

## Solución de Problemas

### Error de conexión
1. Verificar que PostgreSQL esté ejecutándose en el servidor
2. Verificar que el firewall permita el puerto 9134
3. Verificar credenciales en config.ini

### Error "TypeMapping.GlobalTypeMapper"
Este error puede ocurrir si hay conflicto de versiones de Npgsql.
Solución: Limpiar y reconstruir el proyecto en Visual Studio.

### Verificar conexión desde Windows
```cmd
telnet 192.168.10.188 9134
```

---

## Fecha de Migración
Enero 2026

## Autor
Sistema migrado con asistencia de Claude Code
