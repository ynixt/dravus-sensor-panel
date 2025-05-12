param(
    [string]$Action = "",
    [string]$SolutionFile = "./DravusSensorPanel.sln",
    [string]$LintIgnoreFile = "./.lintignore"
)

# Caminho da pasta onde manteremos o ReSharper CLI
$cliDir = Join-Path $PSScriptRoot "resharper-cli"

# Se a pasta não existir, cria
if (!(Test-Path $cliDir)) {
    New-Item -ItemType Directory -Path $cliDir | Out-Null
}

# Caminho do ZIP temporário
$cliZip     = Join-Path $cliDir "resharper-cli.zip"
$cleanupExe = Join-Path $cliDir "CleanupCode.exe"
$inspectExe = Join-Path $cliDir "InspectCode.exe"

# Verifica se CleanupCode.exe e InspectCode.exe existem
if (!((Test-Path $cleanupExe) -and (Test-Path $inspectExe))) {
    Write-Host "ReSharper CLI não encontrado. Baixando..."
    $downloadUrl = "https://download.jetbrains.com/resharper/dotUltimate.2024.3.6/JetBrains.ReSharper.CommandLineTools.2024.3.6.zip"

    Invoke-WebRequest -Uri $downloadUrl -OutFile $cliZip
    Expand-Archive -Path $cliZip -DestinationPath $cliDir
    Remove-Item $cliZip
}
else {
    Write-Host "ReSharper CLI já presente em '$cliDir'."
}

Write-Host "==> Lendo .lintignore em: $LintIgnoreFile"

# Ler .lintignore
if (Test-Path $LintIgnoreFile) {
    $excludeList  = Get-Content $LintIgnoreFile | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    $excludePaths = $excludeList -join ";"
}
else {
    Write-Warning "Arquivo .lintignore não encontrado! Nenhuma pasta será ignorada."
    $excludePaths = ""
}

# Se $Action = "fix", corrige. Caso contrário, só verifica.
if ($Action -eq "fix") {
    # ----------------------------
    # MODO FIX (CleanupCode.exe)
    # ----------------------------
    Write-Host "==> Executando CleanupCode (Full Cleanup) no arquivo/solução: $SolutionFile"

    if ([string]::IsNullOrEmpty($excludePaths)) {
        & $cleanupExe `
            "$SolutionFile" `
            "--profile=Built-in: Full Cleanup" `
            "--disable-settings-layers=GlobalAll;GlobalPerProduct"
    } else {
        & $cleanupExe `
            "$SolutionFile" `
            "--profile=Built-in: Full Cleanup" `
            "--disable-settings-layers=GlobalAll;GlobalPerProduct" `
            "--exclude=$excludePaths"
    }

    Write-Host "==> Formatação/correções concluídas de acordo com o .editorconfig."
}
else {
    # ----------------------------
    # MODO VERIFICAR (InspectCode.exe)
    # ----------------------------
    Write-Host "==> Executando InspectCode no arquivo/solução: $SolutionFile"
    $reportFile = Join-Path $PSScriptRoot "InspectCodeResult.xml"

    if ([string]::IsNullOrEmpty($excludePaths)) {
        & $inspectExe `
            "$SolutionFile" `
            "--output=$reportFile" `
            "--disable-settings-layers=GlobalAll;GlobalPerProduct"
    } else {
        & $inspectExe `
            "$SolutionFile" `
            "--output=$reportFile" `
            "--disable-settings-layers=GlobalAll;GlobalPerProduct" `
            "--exclude=$excludePaths"
    }

    Write-Host "==> Verificação concluída. Relatório salvo em: $reportFile"
    Write-Host "    (Este arquivo XML lista os problemas encontrados, mas não faz correções.)"
}

Write-Host "==> Script finalizado."
