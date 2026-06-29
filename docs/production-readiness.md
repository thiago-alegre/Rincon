# Rincon - Cierre de Produccion

## 1. Backup automatico de PostgreSQL

Copiar el script al servidor:

```powershell
scp -P 5425 .\scripts\backup-postgres.sh root@138.36.239.198:/usr/local/bin/rincon-backup-postgres
```

Preparar permisos en el servidor:

```bash
sudo chmod +x /usr/local/bin/rincon-backup-postgres
sudo mkdir -p /var/backups/rincon
sudo chmod 700 /var/backups/rincon
```

Crear archivo de entorno privado:

```bash
sudo nano /etc/rincon-backup.env
```

Contenido:

```bash
PGPASSWORD='CAMBIAR_POR_PASSWORD_REAL'
DB_HOST='localhost'
DB_PORT='5432'
DB_NAME='rincon'
DB_USER='rincon_app'
BACKUP_DIR='/var/backups/rincon'
RETENTION_DAYS='14'
```

Permisos:

```bash
sudo chmod 600 /etc/rincon-backup.env
```

Probar manualmente:

```bash
sudo bash -c 'set -a; source /etc/rincon-backup.env; set +a; /usr/local/bin/rincon-backup-postgres'
sudo ls -lh /var/backups/rincon
```

Agregar cron diario:

```bash
sudo crontab -e
```

Linea sugerida, todos los dias a las 03:00:

```cron
0 3 * * * bash -c 'set -a; source /etc/rincon-backup.env; set +a; /usr/local/bin/rincon-backup-postgres' >> /var/log/rincon-backup.log 2>&1
```

## 2. Verificar que el backup sirve

En una base de prueba, nunca sobre produccion:

```bash
createdb rincon_restore_test
pg_restore --dbname rincon_restore_test --no-owner --no-privileges /var/backups/rincon/ARCHIVO.dump
psql -d rincon_restore_test -c "\dt"
dropdb rincon_restore_test
```

## 3. Seguridad antes de entrega

- Cambiar password del usuario PostgreSQL `rincon_app`.
- Actualizar la misma password en `/etc/systemd/system/rincon.service`.
- Ejecutar:

```bash
sudo systemctl daemon-reload
sudo systemctl restart rincon.service
sudo systemctl status rincon.service
```

- Confirmar HTTPS:

```bash
sudo certbot renew --dry-run
```

- Confirmar servicios:

```bash
sudo systemctl status rincon.service
sudo systemctl status nginx
```

## 4. Prueba funcional final

- Login administrador.
- Crear categoria.
- Crear articulo con imagen.
- Crear lote, si corresponde.
- Abrir caja.
- Registrar venta en efectivo.
- Registrar venta por transferencia.
- Registrar venta a cuenta personal.
- Saldar parcialmente una cuenta personal.
- Anular una venta cargada por error.
- Registrar recambio del mismo producto.
- Cerrar caja.
- Revisar detalle de caja.
- Revisar ventas, ventas anuladas y estadisticas.
- Cerrar sesion.

## 5. Reset de datos de prueba

El reset final se debe hacer con script SQL controlado, cuando se decida que ya no se necesitan los datos de testing. No ejecutar deletes manuales sueltos en produccion.
