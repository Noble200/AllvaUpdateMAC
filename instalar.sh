#!/bin/bash
# Instalador de AllvaSystem para macOS
# Uso: curl -sL https://raw.githubusercontent.com/Noble200/AllvaUpdateMAC/main/instalar.sh | bash

clear
echo "============================================"
echo "   Instalador de AllvaSystem para macOS"
echo "============================================"
echo ""

DMG_URL="https://github.com/Noble200/AllvaUpdateMAC/releases/download/Uptade/AllvaSystem-1.2.9-macOS.dmg"
DMG_NAME="AllvaSystem-1.2.9-macOS.dmg"
APP_NAME="AllvaSystem.app"
TEMP_DIR="/tmp/AllvaSystem-Install"
MOUNT_POINT="/Volumes/AllvaSystem"

mkdir -p "$TEMP_DIR"
cd "$TEMP_DIR"

echo "[1/5] Descargando AllvaSystem..."
echo ""
curl -L -# -o "$DMG_NAME" "$DMG_URL"

if [ $? -ne 0 ]; then
    echo ""
    echo "Error: No se pudo descargar el archivo."
    echo "Verifica tu conexion a internet."
    exit 1
fi

echo ""
echo "[2/5] Montando imagen..."
hdiutil attach "$DMG_NAME" -quiet

if [ $? -ne 0 ]; then
    echo "Error: No se pudo montar el DMG."
    exit 1
fi

echo "[3/5] Instalando en Aplicaciones..."
if [ -d "/Applications/$APP_NAME" ]; then
    rm -rf "/Applications/$APP_NAME"
fi

cp -R "$MOUNT_POINT/$APP_NAME" /Applications/

if [ $? -ne 0 ]; then
    echo "Error: No se pudo copiar la aplicacion."
    hdiutil detach "$MOUNT_POINT" -quiet
    exit 1
fi

echo "[4/5] Configurando permisos..."
xattr -cr "/Applications/$APP_NAME"

echo "[5/5] Limpiando archivos temporales..."
hdiutil detach "$MOUNT_POINT" -quiet
rm -rf "$TEMP_DIR"

echo ""
echo "============================================"
echo "   AllvaSystem instalado correctamente!"
echo "============================================"
echo ""
echo "La aplicacion esta en: /Applications/$APP_NAME"
echo ""
echo "Para abrir, ejecuta:"
echo "  open /Applications/AllvaSystem.app"
echo ""
