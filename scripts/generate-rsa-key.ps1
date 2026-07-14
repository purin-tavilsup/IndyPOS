# Generate RSA 2048-bit signing key for IndyPOS CloudApi
# Output: Base64-encoded PEM file ready for INDYPOS_RSA_SIGNING_KEY env var

param(
    [int]$KeySize = 2048,
    [string]$OutputPath = ".\cloudapi-signing-key.txt"
)

Write-Host "Generating RSA $KeySize-bit signing key..." -ForegroundColor Cyan

# Generate RSA key using OpenSSL (if available) or .NET
$pemContent = $null

if (Get-Command openssl -ErrorAction SilentlyContinue) {
    # Use OpenSSL for better compatibility
    $tempKeyPath = [System.IO.Path]::GetTempFileName()
    openssl genrsa -out $tempKeyPath $KeySize 2>$null
    $pemContent = Get-Content $tempKeyPath -Raw
    Remove-Item $tempKeyPath -Force
}
else {
    # Use .NET RSA
    $rsa = [System.Security.Cryptography.RSA]::Create($KeySize)
    $pemContent = "-----BEGIN RSA PRIVATE KEY-----`n"
    $privateKeyBytes = $rsa.ExportRSAPrivateKey()
    $base64 = [Convert]::ToBase64String($privateKeyBytes)

    # Format with 64-char line breaks
    for ($i = 0; $i -lt $base64.Length; $i += 64) {
        $line = $base64.Substring($i, [Math]::Min(64, $base64.Length - $i))
        $pemContent += "$line`n"
    }
    $pemContent += "-----END RSA PRIVATE KEY-----"
}

# Convert PEM to base64 for environment variable
$pemBytes = [System.Text.Encoding]::UTF8.GetBytes($pemContent)
$base64Encoded = [Convert]::ToBase64String($pemBytes)

# Save to file
$base64Encoded | Out-File -FilePath $OutputPath -NoNewline -Encoding UTF8

Write-Host ""
Write-Host "RSA key generated successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Output file: $OutputPath" -ForegroundColor Yellow
Write-Host ""
Write-Host "To use in production:" -ForegroundColor Cyan
Write-Host "  1. Set environment variable:"
Write-Host "     `$env:INDYPOS_RSA_SIGNING_KEY = Get-Content '$OutputPath' -Raw"
Write-Host ""
Write-Host "  2. Or add to DigitalOcean App Platform:"
Write-Host "     - Go to App Settings > Environment Variables"
Write-Host "     - Add: INDYPOS_RSA_SIGNING_KEY = <contents of $OutputPath>"
Write-Host ""
Write-Host "IMPORTANT: Keep this key secure! Do not commit to git." -ForegroundColor Red
