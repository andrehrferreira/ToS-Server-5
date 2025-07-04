param (
    [string]$ServerDir,
    [string]$ClientUnrealProjectDir
)

# Verificar se está rodando no Windows
if ($IsWindows -eq $false) {
    Write-Host "This script must be run on Windows."
    exit 1
}

# Validar argumentos
if (-not $ServerDir -or -not $ClientUnrealProjectDir) {
    Write-Host "Usage: .\link_plugin.ps1 -ServerDir 'C:\Server' -ClientUnrealProjectDir 'C:\Unreal\MyGame'"
    exit 1
}

# Diretório do plugin gerado pelo servidor
$pluginSourceDir = Join-Path -Path $ServerDir -ChildPath "Unreal"

# Validar existência do diretório do plugin
if (-not (Test-Path $pluginSourceDir -PathType Container)) {
    Write-Host "Plugin source directory not found: $pluginSourceDir"
    exit 1
}

# Diretório de plugins do projeto Unreal
$pluginsDir = Join-Path -Path $ClientUnrealProjectDir -ChildPath "Plugins"

# Criar diretório Plugins se não existir
if (-not (Test-Path $pluginsDir -PathType Container)) {
    Write-Host "Creating Plugins directory in Unreal project."
    New-Item -Path $pluginsDir -ItemType Directory | Out-Null
}

# Diretório de destino do link simbólico
$linkDir = Join-Path -Path $pluginsDir -ChildPath "ToS_Network"

# Se o link simbólico ou pasta já existir, remover
if (Test-Path $linkDir) {
    Write-Host "Removing existing plugin link or folder: $linkDir"
    Remove-Item -Path $linkDir -Recurse -Force
}

# Criar junction
Write-Host "Creating junction: $linkDir -> $pluginSourceDir"
$junctionCommand = "cmd /c mklink /J `"$linkDir`" `"$pluginSourceDir`""
Invoke-Expression $junctionCommand

# Verificar se foi criado
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Plugin link created successfully."
    Write-Host "$linkDir now mirrors $pluginSourceDir."
} else {
    Write-Host "❌ Failed to create plugin junction."
    exit 1
}
