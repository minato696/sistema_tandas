-- Script para limpiar completamente la base de datos
-- Ejecuta esto en Microsoft Access

-- Limpiar tabla Comerciales
DELETE FROM Comerciales;

-- Limpiar tabla ComercialesAsignados
DELETE FROM ComercialesAsignados;

-- Mensaje de confirmación
SELECT 'Base de datos limpiada correctamente' AS Mensaje;
