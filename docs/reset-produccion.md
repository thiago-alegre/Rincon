# Puesta en cero de Rincon

Este procedimiento deja el sistema listo para uso real:

- Borra ventas, detalles de ventas, anulaciones, recambios, cajas, cuentas personales, pagos, articulos, lotes y categorias.
- Borra usuarios comunes y roles existentes.
- Mantiene `__EFMigrationsHistory`, para no perder el estado de migraciones de EF Core.
- Crea nuevamente los roles `Admin`, `Employee` y `Dios`.
- Crea un usuario inicial oculto/protegido con rol `Admin` + `Dios`.

## Credenciales iniciales

Usuario:

```text
admin@rinconweb.online
```

Password inicial:

```text
Cambiar123Aa!
```

Despues del primer ingreso, cambiar la contrasena desde el propio usuario.

## 1. Confirmar backup

En el servidor:

```bash
sudo bash -c 'set -a; source /etc/rincon-backup.env; set +a; /usr/local/bin/rincon-backup-postgres'
sudo ls -lh /var/backups/rincon
```

Debe verse un `.dump` reciente.

## 2. Subir el script al servidor

Desde PowerShell local:

```powershell
cd C:\Users\alegr\source\repos\Rincon
scp -P 5425 .\scripts\reset-production-postgres.sql root@138.36.239.198:/tmp/reset-production-postgres.sql
```

## 3. Ejecutar la puesta en cero

Entrar al servidor:

```powershell
ssh -p5425 root@138.36.239.198
```

Ejecutar:

```bash
sudo -u postgres psql -d rincon -f /tmp/reset-production-postgres.sql
```

## 4. Limpiar imagenes subidas

Si se quiere dejar tambien vacia la carpeta de imagenes de articulos:

```bash
sudo find /var/www/rincon/wwwroot/imagenes/articles -type f -delete
sudo chown -R www-data:www-data /var/www/rincon/wwwroot/imagenes/articles
```

## 5. Reiniciar la aplicacion

```bash
sudo systemctl restart rincon.service
sudo systemctl status rincon.service
```

Debe figurar `active (running)`.

## 6. Validar

Ingresar a:

```text
https://rinconweb.online
```

Validaciones recomendadas:

- Entrar con `admin@rinconweb.online`.
- Cambiar la contrasena inicial.
- Confirmar que el usuario no aparece en la pantalla `Usuarios`.
- Crear un administrador real para el negocio.
- Crear un empleado de prueba.
- Crear una categoria.
- Crear un articulo.
- Abrir caja.
- Registrar una venta chica.
- Confirmar que ventas, caja y estadisticas responden correctamente.

## 7. Borrar el script temporal del servidor

```bash
sudo rm -f /tmp/reset-production-postgres.sql
```
