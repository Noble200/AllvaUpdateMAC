#!/bin/bash
# Script de compilacion, empaquetado (.app + .dmg) y publicacion para macOS
# Uso: ./build-release-mac.sh [version]
# Ejemplo: ./build-release-mac.sh 1.4.4

set -e

VERSION="${1:-1.4.4}"
APP_NAME="AllvaSystem"
BUNDLE_ID="com.allvasystem.desktop"

# Detectar arquitectura
ARCH=$(uname -m)
if [ "$ARCH" = "arm64" ]; then
    RID="osx-arm64"
else
    RID="osx-x64"
fi

PUBLISH_DIR="./bin/Release/net8.0/$RID/publish"
APP_BUNDLE="./$APP_NAME.app"
DMG_NAME="$APP_NAME-$VERSION-macOS.dmg"
DMG_PATH="./Releases/$DMG_NAME"

echo "============================================"
echo "  AllvaSystem v$VERSION - Build macOS ($RID)"
echo "============================================"

# =====================
# PASO 1: Compilar
# =====================
echo ""
echo "[1/4] Limpiando y compilando..."
dotnet clean -c Release -r "$RID" 2>/dev/null || true
dotnet publish -c Release -r "$RID" --self-contained

echo "Compilacion exitosa."

# =====================
# PASO 2: Crear .app bundle
# =====================
echo ""
echo "[2/4] Creando $APP_NAME.app bundle..."

# Limpiar bundle anterior
rm -rf "$APP_BUNDLE"

# Estructura del .app
mkdir -p "$APP_BUNDLE/Contents/MacOS"
mkdir -p "$APP_BUNDLE/Contents/Resources"

# Copiar ejecutable y dependencias
cp -R "$PUBLISH_DIR/"* "$APP_BUNDLE/Contents/MacOS/"

# Crear Info.plist
cat > "$APP_BUNDLE/Contents/Info.plist" << PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>
    <string>$APP_NAME</string>
    <key>CFBundleDisplayName</key>
    <string>AllvaSystem</string>
    <key>CFBundleIdentifier</key>
    <string>$BUNDLE_ID</string>
    <key>CFBundleVersion</key>
    <string>$VERSION</string>
    <key>CFBundleShortVersionString</key>
    <string>$VERSION</string>
    <key>CFBundleExecutable</key>
    <string>Allva.Desktop</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleIconFile</key>
    <string>allva-icon</string>
    <key>LSMinimumSystemVersion</key>
    <string>12.0</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>NSSupportsAutomaticGraphicsSwitching</key>
    <true/>
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
</dict>
</plist>
PLIST

# Copiar icono si existe (convertir .ico a .icns o usar el .ico directamente)
if [ -f "Assets/allva-icon.ico" ]; then
    cp "Assets/allva-icon.ico" "$APP_BUNDLE/Contents/Resources/allva-icon.ico"
fi

# Hacer ejecutable
chmod +x "$APP_BUNDLE/Contents/MacOS/Allva.Desktop"

# Quitar atributos de cuarentena
xattr -cr "$APP_BUNDLE" 2>/dev/null || true

echo "Bundle creado: $APP_BUNDLE"

# =====================
# PASO 3: Crear .dmg
# =====================
echo ""
echo "[3/4] Creando $DMG_NAME..."

mkdir -p ./Releases

# Eliminar DMG anterior si existe
rm -f "$DMG_PATH"

# Crear DMG con hdiutil
hdiutil create -volname "$APP_NAME" \
    -srcfolder "$APP_BUNDLE" \
    -ov -format UDZO \
    "$DMG_PATH"

echo "DMG creado: $DMG_PATH"

# =====================
# PASO 4: Resumen
# =====================
echo ""
echo "============================================"
echo "  Build completado exitosamente!"
echo "============================================"
echo ""
echo "  Version:  $VERSION"
echo "  Arch:     $RID"
echo "  Bundle:   $APP_BUNDLE"
echo "  DMG:      $DMG_PATH"
echo "  Tamano:   $(du -sh "$DMG_PATH" | cut -f1)"
echo ""
echo "Para instalar manualmente:"
echo "  open $DMG_PATH"
echo ""
echo "Para subir como release a GitHub:"
echo "  gh release create v$VERSION $DMG_PATH --title \"Mac V $VERSION\" --notes \"AllvaSystem v$VERSION para macOS\""
echo ""
