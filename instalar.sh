#!/bin/bash
# ============================================
# AllvaSystem - Instalador/Actualizador macOS
# ============================================
# Uso desde terminal:
#   curl -fsSL https://raw.githubusercontent.com/Noble200/AllvaUpdateMAC/main/instalar.sh | bash
#
# O para una version especifica:
#   curl -fsSL https://raw.githubusercontent.com/Noble200/AllvaUpdateMAC/main/instalar.sh | bash -s -- 1.4.4

set -e

APP_NAME="AllvaSystem"
REPO="Noble200/AllvaUpdateMAC"
INSTALL_DIR="/Applications"
TEMP_DIR="/tmp/AllvaSystem-Install"
MOUNT_POINT="/Volumes/$APP_NAME"

# Version: si se pasa como argumento usarla, sino obtener la ultima de GitHub
if [ -n "$1" ]; then
    VERSION="$1"
    DOWNLOAD_URL="https://github.com/$REPO/releases/download/v$VERSION/$APP_NAME-$VERSION-macOS.dmg"
else
    echo "Obteniendo ultima version disponible..."
    # Obtener la URL del ultimo release
    DOWNLOAD_URL=$(curl -fsSL "https://api.github.com/repos/$REPO/releases/latest" | grep "browser_download_url.*\.dmg" | head -1 | cut -d '"' -f 4)
    VERSION=$(echo "$DOWNLOAD_URL" | grep -oE '[0-9]+\.[0-9]+\.[0-9]+')

    if [ -z "$DOWNLOAD_URL" ]; then
        echo "Error: No se pudo obtener la URL de descarga"
        exit 1
    fi
fi

DMG_FILE="$TEMP_DIR/$APP_NAME-$VERSION-macOS.dmg"

echo "============================================"
echo "  AllvaSystem - Instalador macOS"
echo "============================================"
echo ""
echo "  Version: $VERSION"
echo ""

# =====================
# PASO 1: Descargar
# =====================
echo "[1/5] Descargando AllvaSystem v$VERSION..."

mkdir -p "$TEMP_DIR"
rm -f "$DMG_FILE"

curl -fSL --progress-bar "$DOWNLOAD_URL" -o "$DMG_FILE"

if [ ! -f "$DMG_FILE" ]; then
    echo "Error: No se pudo descargar el archivo"
    exit 1
fi

echo "Descarga completada."

# =====================
# PASO 2: Montar DMG
# =====================
echo "[2/5] Montando imagen de disco..."

# Desmontar si ya estaba montado
hdiutil detach "$MOUNT_POINT" 2>/dev/null || true

hdiutil attach "$DMG_FILE" -nobrowse -quiet

if [ ! -d "$MOUNT_POINT" ]; then
    echo "Error: No se pudo montar la imagen"
    exit 1
fi

echo "Imagen montada."

# =====================
# PASO 3: Instalar
# =====================
echo "[3/5] Instalando en $INSTALL_DIR..."

# Cerrar la app si esta corriendo
pkill -f "$APP_NAME" 2>/dev/null || true
sleep 1

# Eliminar version anterior
if [ -d "$INSTALL_DIR/$APP_NAME.app" ]; then
    echo "  Eliminando version anterior..."
    rm -rf "$INSTALL_DIR/$APP_NAME.app"
fi

# Copiar nueva version
cp -R "$MOUNT_POINT/$APP_NAME.app" "$INSTALL_DIR/"

echo "Instalacion completada."

# =====================
# PASO 4: Configurar permisos
# =====================
echo "[4/5] Configurando permisos..."

# Quitar cuarentena de macOS (para que no pida verificacion de desarrollador)
xattr -cr "$INSTALL_DIR/$APP_NAME.app" 2>/dev/null || true

# Asegurar que el ejecutable tiene permisos
chmod +x "$INSTALL_DIR/$APP_NAME.app/Contents/MacOS/Allva.Desktop" 2>/dev/null || true

echo "Permisos configurados."

# =====================
# PASO 5: Limpiar
# =====================
echo "[5/5] Limpiando archivos temporales..."

hdiutil detach "$MOUNT_POINT" -quiet 2>/dev/null || true
rm -rf "$TEMP_DIR"

echo "Limpieza completada."

# =====================
# RESULTADO
# =====================
echo ""
echo "============================================"
echo "  AllvaSystem v$VERSION instalado!"
echo "============================================"
echo ""
echo "  Ubicacion: $INSTALL_DIR/$APP_NAME.app"
echo ""
echo "  Para abrir:"
echo "    open /Applications/$APP_NAME.app"
echo ""
echo "  Para actualizar en el futuro:"
echo "    curl -fsSL https://raw.githubusercontent.com/$REPO/main/instalar.sh | bash"
echo ""
