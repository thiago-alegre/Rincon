# Backup diario adicional en Google Drive

Este procedimiento mantiene sin cambios el backup local de PostgreSQL:

- se crea primero en `/var/backups/rincon`;
- conserva la retención local configurada en `RETENTION_DAYS`;
- luego intenta subir el mismo archivo a Google Drive;
- conserva en Drive solamente los últimos 30 días;
- un fallo de Drive no elimina ni invalida el backup local.

## 1. Preparar Google Drive

1. Ingresar con la cuenta de Google creada exclusivamente para backups.
2. Activar la verificación en dos pasos de la cuenta.
3. No compartir públicamente la carpeta ni sus archivos.

La carpeta `Backups Rincon` se creará más adelante mediante rclone. Esto permite
usar el alcance limitado `drive.file` sin darle acceso al resto del Drive.

## 2. Crear credenciales OAuth propias

En Google Cloud Console:

1. Crear un proyecto llamado `Rincon Backups`.
2. Habilitar `Google Drive API`.
3. Configurar la pantalla de consentimiento OAuth como `Externa` y agregar como
   usuario de prueba la cuenta exclusiva de backups.
4. Crear credenciales OAuth de tipo `Aplicación de escritorio`.
5. Cuando la autorización ya funcione, cambiar el estado de publicación de la
   aplicación a `En producción`. Si permanece en modo `Prueba`, Google puede
   hacer caducar el token de actualización a los 7 días.
6. Guardar el Client ID y Client Secret en un gestor de contraseñas.

No guardar estas credenciales en el repositorio.

## 3. Instalar rclone

En el VPS:

```bash
sudo -v
curl https://rclone.org/install.sh | sudo bash
rclone version
```

## 4. Autorizar la cuenta de Drive

En el VPS ejecutar:

```bash
sudo rclone config
```

Responder:

1. `n` para crear una conexión nueva.
2. Nombre: `rincon-drive`.
3. Tipo: `drive`.
4. Ingresar el Client ID propio.
5. Ingresar el Client Secret propio.
6. Elegir el alcance `drive.file`, para limitar el acceso a archivos creados por esta integración.
7. Dejar vacío `service_account_file`.
8. No usar configuración avanzada.
9. Responder `n` cuando pregunte si el servidor puede abrir un navegador.

Rclone mostrará el comando `rclone authorize` que debe ejecutarse en una computadora con navegador. Instalar rclone en esa computadora, ejecutar exactamente el comando indicado, iniciar sesión con la cuenta exclusiva de backups y pegar en el VPS el token generado.

No copiar el token en documentación, capturas, chats ni en el repositorio.

Finalizar la configuración y confirmar que el remoto se llame `rincon-drive`.

## 5. Proteger y probar la configuración

En el VPS:

```bash
sudo chmod 600 /root/.config/rclone/rclone.conf
sudo rclone --config /root/.config/rclone/rclone.conf mkdir "rincon-drive:Backups Rincon"
sudo rclone --config /root/.config/rclone/rclone.conf lsd rincon-drive:
```

Debe aparecer la carpeta `Backups Rincon`.

## 6. Habilitar Drive en el entorno del backup

Editar el archivo privado existente:

```bash
sudo nano /etc/rincon-backup.env
```

Agregar sin cambiar las variables actuales de PostgreSQL:

```text
RCLONE_REMOTE='rincon-drive'
DRIVE_BACKUP_DIR='Backups Rincon'
RCLONE_CONFIG_FILE='/root/.config/rclone/rclone.conf'
DRIVE_RETENTION_DAYS='30'
```

Mantener sus permisos:

```bash
sudo chmod 600 /etc/rincon-backup.env
```

## 7. Instalar la versión actualizada del script

Desde la computadora local, ubicada en el repositorio, subir primero un archivo
nuevo sin reemplazar el script activo:

```powershell
scp -P 5425 .\scripts\backup-postgres.sh root@138.36.239.198:/tmp/rincon-backup-postgres.new
```

En el VPS, validar la sintaxis, conservar una copia del script actual e instalar
la versión validada:

```bash
sudo bash -n /tmp/rincon-backup-postgres.new
sudo cp -a /usr/local/bin/rincon-backup-postgres /usr/local/bin/rincon-backup-postgres.before-drive
sudo install -o root -g root -m 700 /tmp/rincon-backup-postgres.new /usr/local/bin/rincon-backup-postgres
sudo rm /tmp/rincon-backup-postgres.new
sudo chown root:root /usr/local/bin/rincon-backup-postgres
sudo chmod 700 /usr/local/bin/rincon-backup-postgres
```

La copia `rincon-backup-postgres.before-drive` permite volver rápidamente al
script anterior si fuera necesario.

## 8. Prueba manual

Ejecutar:

```bash
sudo bash -c 'set -a; source /etc/rincon-backup.env; set +a; /usr/local/bin/rincon-backup-postgres'
```

La salida debe informar primero `Backup created` y luego `Drive backup uploaded`.

Comprobar ambos destinos:

```bash
sudo ls -lht /var/backups/rincon | head
sudo rclone --config /root/.config/rclone/rclone.conf lsl "rincon-drive:Backups Rincon"
```

## 9. Confirmar la tarea diaria

El cron actual no debe duplicarse ni reemplazarse. Verificarlo con:

```bash
sudo crontab -l
```

Debe continuar ejecutando una sola vez al día el mismo script, que ahora realiza en orden el backup local y la copia adicional a Drive.

## 10. Validar la retención antes de habilitar el borrado

Para ver qué archivos remotos tienen más de 30 días sin eliminarlos:

```bash
sudo rclone --config /root/.config/rclone/rclone.conf delete "rincon-drive:Backups Rincon" \
  --min-age 30d \
  --include 'rincon_*.dump' \
  --dry-run
```

El script definitivo aplica la misma selección sin `--dry-run` después de cada subida exitosa. Solo afecta archivos `rincon_*.dump` dentro de `Backups Rincon`.

## 11. Comprobación recomendada

La presencia del archivo en Drive no reemplaza una prueba de restauración. Periódicamente se debe descargar una copia, restaurarla en una base de prueba y verificar sus tablas sin tocar producción.
