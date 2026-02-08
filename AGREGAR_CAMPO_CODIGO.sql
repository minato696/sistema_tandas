-- Script para agregar el campo Codigo a la tabla ComercialesAsignados
-- Ejecuta esto en Microsoft Access

-- Agregar columna Codigo a ComercialesAsignados
ALTER TABLE ComercialesAsignados ADD COLUMN Codigo TEXT(50);

-- Mensaje de confirmación
SELECT 'Campo Codigo agregado correctamente a ComercialesAsignados' AS Mensaje;
