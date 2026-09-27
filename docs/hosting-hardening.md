# Correcciones de hosting de Rincon

## Validación local

Ejecutar desde la solución:

```powershell
dotnet build Rincon.sln -c Release
dotnet run --project tests/HostingSecurityChecks -c Release
```

Las pruebas levantan un servidor local sin base de datos, usan claves temporales y verifican HTTPS, confianza del proxy, errores y persistencia entre instancias. No prueban el runtime Linux instalado en producción.

## Publicación y conservación de claves

Publicar en una carpeta nueva, respaldar el despliegue actual completo y detener el servicio antes de reemplazar binarios. No actualizar DLL individuales con el proceso ejecutándose. Conservar configuración de producción, archivos subidos y claves. Iniciar nuevamente y comprobar logs y login.

La aplicación conserva por defecto ContentRootPath/DataProtectionKeys para no invalidar cookies existentes. Para independizar las claves del despliegue:

1. Respaldar la carpeta actual con acceso restringido; no copiar ni mostrar el contenido XML en logs.
2. Con el servicio detenido, copiar todo el key ring existente a /var/lib/rincon/keys. Conservar también el original para rollback.
3. Asignar propietario www-data y permisos 700 al directorio y 600 a los archivos.
4. Configurar DataProtection__KeysPath=/var/lib/rincon/keys en el entorno de systemd. Mantener SetApplicationName("Rincon").
5. Recargar systemd e iniciar Rincon. El arranque verifica Protect/Unprotect y falla explícitamente si las claves no son utilizables.
6. Verificar que una cookie anterior siga funcionando y que el servicio pueda escribir en la nueva carpeta. Respaldar esta carpeta separadamente de la aplicación y la base.

No eliminar claves antiguas ni deshabilitar la rotación. La prueba al inicio no garantiza que una escritura futura funcione si cambian los permisos o se llena el disco. Las claves siguen sin cifrado adicional en reposo: proteger permisos y backups. Introducir cifrado con certificado requiere aprovisionamiento y respaldo de la clave privada; no configurar un certificado ficticio ni reutilizar automáticamente el certificado renovable de Nginx.

## Comprobación en producción

```bash
curl -I https://rinconweb.online/
curl -I http://127.0.0.1:5005/ -H 'Host: rinconweb.online' -H 'X-Forwarded-Proto: https'
journalctl -u rincon -n 100 --no-pager
```

Ambas peticiones anónimas deben responder 302 con Location HTTPS hacia Identity/Account/Login. Probar login, carrito y logout desde el navegador. El proxy confiable es únicamente loopback; si cambia la topología, configurar sus IP explícitamente. AllowedHosts contiene los dominios y direcciones actuales; actualizarlo antes de añadir dominios.

## Incidente de Registry

La causa original no está demostrada. Guardar el primer error completo y contrastar las fechas de cambios de paquetes .NET con el inicio del proceso. Después de actualizar el runtime, reiniciar controladamente el servicio y probarlo. No agregar assemblies sueltos ni asumir que su ausencia en Rincon.deps.json demuestra un publish incompleto.

## Session

Session continúa en memoria: las ocho horas son un límite de inactividad, no una garantía de persistencia. Reiniciar borra carritos no confirmados; las ventas confirmadas en base de datos no dependen de este almacenamiento. Evitar reiniciar con ventas en curso. Hacer persistente el carrito requiere un almacenamiento durable y pruebas del flujo comercial; esta corrección de hosting no cambia ese modelo de datos.

La respuesta de emergencia ya no reejecuta Session y no revela excepciones. Los errores siguen registrados por el middleware de ASP.NET Core. Data Protection tiene logging Information para registrar eventos de claves sin registrar su material secreto.
