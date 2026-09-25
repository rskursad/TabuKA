<#
.SYNOPSIS
    TabuKA Yardımcı Scripti (Windows PowerShell / PowerShell Core)
.DESCRIPTION
    TabuKA projesinin derleme, test, masaüstü çalıştırma, Android derleme, 
    Android imza anahtarı oluşturma (keystore) ve imzalı Android yayınlama 
    (publish-android) işlemlerini tek dosyadan yönetir.

.PARAMETER Action
    Gerçekleştirilecek işlem: 
    - build           : Masaüstü projeyi derler ve testleri çalıştırır
    - run             : Masaüstü uygulamayı başlatır
    - test            : Birim testlerini çalıştırır
    - build-android   : Android APK derler (Debug/Release)
    - run-android     : Bağlı Android cihaz veya emülatörde çalıştırır
    - create-keystore : Android için RSA 2048-bit imza anahtarı (keystore) oluşturur
    - publish-android : İmzalı Android paketi yayınlar (keystore yoksa otomatik oluşturur)

.PARAMETER Configuration
    Derleme yapılandırması (Debug, Release). Varsayılan: Debug

.PARAMETER KeystorePath
    Keystore dosya yolu. Varsayılan: tabuka.keystore

.PARAMETER KeyAlias
    Keystore anahtar takma adı. Varsayılan: tabuka

.PARAMETER KeyPassword
    Keystore şifresi. Varsayılan: tabuka123

.PARAMETER PackageFormat
    Android paketleme formatı: apk veya aab (Google Play Bundle). Varsayılan: apk
#>

param (
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet("build", "run", "test", "build-android", "run-android", "create-keystore", "create-keystore-custom", "keystore", "publish-android", "android-publish")]
    [string]$Action,

    [string]$Configuration = "Debug",
    [string]$KeystorePath = "tabuka.keystore",
    [string]$KeyAlias = "tabuka",
    [string]$KeyPassword = "tabuka123",
    [ValidateSet("apk", "aab")]
    [string]$PackageFormat = "apk",
    [switch]$Interactive = $false
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

# keystore.properties mevcutsa yükle (komut satırı parametreleri yoksa)
$keystorePropPath = Join-Path $scriptDir "keystore.properties"
if (Test-Path $keystorePropPath) {
    Get-Content $keystorePropPath | ForEach-Object {
        $line = $_.Trim()
        if ($line -and -not $line.StartsWith("#")) {
            $parts = $line -split "=", 2
            if ($parts.Length -eq 2) {
                $k = $parts[0].Trim()
                $v = $parts[1].Trim()
                switch -Regex ($k) {
                    "^(storeFile|KEYSTORE|KEYSTORE_FILE)$" { if (-not $PSBoundParameters.ContainsKey('KeystorePath')) { $KeystorePath = $v } }
                    "^(keyAlias|KEY_ALIAS)$"               { if (-not $PSBoundParameters.ContainsKey('KeyAlias')) { $KeyAlias = $v } }
                    "^(storePassword|KEYSTORE_PASS|KEYSTORE_PASSWORD)$" { if (-not $PSBoundParameters.ContainsKey('KeyPassword')) { $KeyPassword = $v } }
                    "^(keyPassword|KEY_PASS)$"             { if (-not $PSBoundParameters.ContainsKey('KeyPassword')) { $KeyPassword = $v } }
                }
            }
        }
    }
}

function Show-Banner {
    param ([string]$Emoji, [string]$Title)
    Write-Host "==========================================" -ForegroundColor Cyan
    Write-Host "  $Emoji TabuKA - $Title" -ForegroundColor Yellow
    Write-Host "==========================================" -ForegroundColor Cyan
}

function Find-Adb {
    $adb = Get-Command "adb" -ErrorAction SilentlyContinue
    if (-not $adb) {
        $potential = "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe"
        if (Test-Path $potential) {
            $env:PATH += ";$env:LOCALAPPDATA\Android\Sdk\platform-tools"
            return $true
        }
    }
    return [bool]$adb
}

function Find-Keytool {
    $cmd = Get-Command "keytool" -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }

    $candidates = @(
        "$env:JAVA_HOME\bin\keytool.exe",
        "$env:ProgramFiles\Android\Android Studio\jbr\bin\keytool.exe",
        "$env:LOCALAPPDATA\Android\Sdk\jbr\bin\keytool.exe",
        "$env:ProgramFiles\Eclipse Adoptium\*\bin\keytool.exe",
        "$env:ProgramFiles\Microsoft\jdk-*\bin\keytool.exe",
        "$env:ProgramFiles\Java\*\bin\keytool.exe"
    )

    foreach ($pattern in $candidates) {
        $found = Resolve-Path $pattern -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($found -and (Test-Path $found.Path)) {
            return $found.Path
        }
    }
    return $null
}

function Test-Build {
    Show-Banner -Emoji "🔨" -Title "Proje Derleniyor..."

    Write-Host "`n[1/2] TabuKA Masaüstü Derleniyor (dotnet build)..." -ForegroundColor Green
    dotnet build TabuKA.csproj --configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Host "`n❌ Derleme sırasında hata oluştu!" -ForegroundColor Red
        exit $LASTEXITCODE
    }

    Write-Host "`n[2/2] Birim Testleri Çalıştırılıyor (dotnet test)..." -ForegroundColor Green
    dotnet test TabuKA.Tests\TabuKA.Tests.csproj --verbosity normal

    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n✅ TÜM ADIMLAR BAŞARILI! TabuKA oynamaya hazır." -ForegroundColor Green
        Write-Host "Oyunu başlatmak için: .\TabuKA.ps1 run" -ForegroundColor Yellow
    } else {
        Write-Host "`n⚠️ Bazı testler başarısız oldu!" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

function Run-Desktop {
    Show-Banner -Emoji "🎮" -Title "Kelime Anlatma Oyunu Başlatılıyor"
    Write-Host "  Masaüstü uygulaması başlatılıyor...     " -ForegroundColor Cyan

    dotnet run --project TabuKA.csproj -c $Configuration
}

function Test-BuildAndroidTeacher {
    Show-Banner -Emoji "📱" -Title "Android APK Derleme"

    Write-Host "`n[1/3] .NET Android İş Yükü Kontrol Ediliyor..." -ForegroundColor Green
    $workloads = dotnet workload list
    if ($workloads -notmatch "android") {
        Write-Host "⚠️ 'android' iş yükü bulunamadı!" -ForegroundColor Yellow
        Write-Host "Yüklemek için Yönetici olarak şu komutu çalıştırabilirsiniz:" -ForegroundColor Cyan
        Write-Host "  dotnet workload install android" -ForegroundColor White
        exit 1
    }
    Write-Host "✅ Android iş yükü kurulu." -ForegroundColor Green

    Write-Host "`n[2/3] TabuKA Android Paketi Derleniyor ($Configuration)..." -ForegroundColor Green
    dotnet build "TabuKA.Android\TabuKA.Android.csproj" -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Host "`n❌ Android derleme sırasında hata oluştu!" -ForegroundColor Red
        exit $LASTEXITCODE
    }

    Write-Host "`n[3/3] Oluşturulan APK Aranıyor..." -ForegroundColor Green
    $apkFiles = Get-ChildItem -Path "TabuKA.Android\bin\$Configuration" -Filter "*.apk" -Recurse -ErrorAction SilentlyContinue

    if ($apkFiles) {
        Write-Host "`n🎉 BAŞARILI! Android paketi hazır:" -ForegroundColor Green
        foreach ($apk in $apkFiles) {
            $sizeMb = [math]::Round($apk.Length / 1MB, 2)
            Write-Host "  📱 APK: $($apk.FullName) ($sizeMb MB)" -ForegroundColor Yellow
        }
        Write-Host "`nCihaza veya emülatöre yükleyip çalıştırmak için: .\TabuKA.ps1 run-android" -ForegroundColor Cyan
    } else {
        Write-Host "`n⚠️ Derleme tamamlandı ancak APK dosyası bulunamadı. Lütfen bin klasörünü kontrol edin." -ForegroundColor Yellow
    }
}

function Run-Android {
    Show-Banner -Emoji "📱" -Title "Android Cihazda Çalıştır"

    Write-Host "`n[1/3] Android Cihaz/Emülatör Kontrol Ediliyor (adb devices)..." -ForegroundColor Green
    if (Find-Adb) {
        $devices = adb devices | Select-String -Pattern "\b(device|emulator)\b"
        if ($devices) {
            Write-Host "✅ Bulunan Cihazlar/Emülatörler:" -ForegroundColor Green
            $devices | ForEach-Object { Write-Host "  • $_" -ForegroundColor Cyan }
        } else {
            Write-Host "⚠️ Bağlı Android cihaz veya açık emülatör bulunamadı!" -ForegroundColor Yellow
            Write-Host "  Lütfen telefonunuzu USB ile bağlayıp USB Hata Ayıklama'yı açın" -ForegroundColor White
            Write-Host "  veya Android Studio üzerinden bir emülatör başlatın." -ForegroundColor White
        }
    } else {
        Write-Host "ℹ️ ADB komutu PATH'de bulunamadı. dotnet run doğrudan denenecek..." -ForegroundColor Yellow
    }

    Write-Host "`n[2/3] TabuKA Android Hedefine Gönderiliyor..." -ForegroundColor Green
    dotnet build "TabuKA.Android\TabuKA.Android.csproj" -t:Run -c $Configuration

    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n🚀 TabuKA Android cihazınızda başarıyla başlatıldı!" -ForegroundColor Green
    } else {
        Write-Host "`n❌ Dağıtım veya çalıştırma sırasında hata oluştu." -ForegroundColor Red
    }
}

function Create-Keystore {
    if ($Interactive) {
        Create-Keystore-Custom
        return
    }

    Show-Banner -Emoji "🔑" -Title "Android İmza Anahtarı (Keystore) Oluşturma (Test)"
    if (Test-Path $KeystorePath) {
        Write-Host "ℹ️ Keystore dosyası zaten mevcut: $KeystorePath" -ForegroundColor Yellow
        return
    }

    $keytoolPath = Find-Keytool
    if (-not $keytoolPath) {
        Write-Host "❌ 'keytool' bulunamadı! Lütfen JDK veya Android Studio'nun kurulu olduğundan emin olun." -ForegroundColor Red
        exit 1
    }

    Write-Host "Keytool: $keytoolPath" -ForegroundColor Cyan
    Write-Host "Otomatik test anahtarı oluşturuluyor ($KeystorePath)..." -ForegroundColor Green

    & $keytoolPath -genkeypair -v -keystore $KeystorePath -alias $KeyAlias -keyalg RSA -keysize 2048 -validity 10000 -storepass $KeyPassword -keypass $KeyPassword -dname "CN=TabuKA, OU=Gaming, O=TabuKA, L=Istanbul, ST=Istanbul, C=TR"

    if ($LASTEXITCODE -eq 0 -and (Test-Path $KeystorePath)) {
        Write-Host "`n✅ Keystore başarıyla oluşturuldu: $KeystorePath" -ForegroundColor Green
        Write-Host "   Alias: $KeyAlias" -ForegroundColor Yellow
    } else {
        Write-Host "`n❌ Keystore oluşturulamadı!" -ForegroundColor Red
        exit 1
    }
}

function Create-Keystore-Custom {
    Show-Banner -Emoji "🔑" -Title "Kişisel Android İmza Anahtarı (Keystore) Oluşturma"

    $keytoolPath = Find-Keytool
    if (-not $keytoolPath) {
        Write-Host "❌ 'keytool' bulunamadı! Lütfen JDK veya Android Studio'nun kurulu olduğundan emin olun." -ForegroundColor Red
        exit 1
    }

    Write-Host "Kendi imzanız için bilgileriniz etkileşimli olarak sorulacaktır.`n" -ForegroundColor Cyan

    $ks = Read-Host "Keystore dosya adı [$KeystorePath]"
    if ($ks) { $script:KeystorePath = $ks }
    $al = Read-Host "Anahtar takma adı (Alias) [$KeyAlias]"
    if ($al) { $script:KeyAlias = $al }

    if (Test-Path $KeystorePath) {
        $confirm = Read-Host "⚠️ '$KeystorePath' dosyası zaten mevcut! Üzerine yazmak istiyor musunuz? (e/H)"
        if ($confirm -notmatch "^[eEyY]$") {
            Write-Host "İşlem iptal edildi." -ForegroundColor Yellow
            return
        }
        Remove-Item -Path $KeystorePath -Force
    }

    Write-Host "`n▶️ keytool başlatılıyor (Lütfen şifrenizi ve kimlik bilgilerinizi girin):" -ForegroundColor Green
    & $keytoolPath -genkeypair -v -keystore $KeystorePath -alias $KeyAlias -keyalg RSA -keysize 2048 -validity 10000

    if ($LASTEXITCODE -eq 0 -and (Test-Path $KeystorePath)) {
        Write-Host "`n✅ Kişisel imza anahtarınız başarıyla oluşturuldu: $KeystorePath" -ForegroundColor Green
        Write-Host "   Alias: $KeyAlias" -ForegroundColor Yellow

        $saveProp = Read-Host "`nBu anahtarı 'keystore.properties' içine kaydetmek ister misiniz? (E/h)"
        if ($saveProp -notmatch "^[hHnN]$") {
            "storeFile=$KeystorePath`nkeyAlias=$KeyAlias`n# storePassword=şifreniz`n# keyPassword=şifreniz" | Out-File -FilePath $keystorePropPath -Encoding utf8
            Write-Host "✅ 'keystore.properties' kaydedildi." -ForegroundColor Green
        }
    } else {
        Write-Host "`n❌ Keystore oluşturulamadı!" -ForegroundColor Red
        exit 1
    }
}

function Publish-Android {
    Show-Banner -Emoji "📦" -Title "İmzalı Android Paketi Yayınlama ($PackageFormat)"

    $workloads = dotnet workload list
    if ($workloads -notmatch "android") {
        Write-Host "⚠️ 'android' iş yükü bulunamadı!" -ForegroundColor Yellow
        Write-Host "Yüklemek için Yönetici olarak şu komutu çalıştırabilirsiniz:" -ForegroundColor Cyan
        Write-Host "  dotnet workload install android" -ForegroundColor White
        exit 1
    }

    if (-not (Test-Path $KeystorePath)) {
        Write-Host "❌ İmza anahtarı ($KeystorePath) bulunamadı!" -ForegroundColor Red
        Write-Host "  • Kendi imzanızı oluşturmak için:      .\TabuKA.ps1 create-keystore-custom" -ForegroundColor Cyan
        Write-Host "  • Var olan anahtarınızı bağlamak için: 'keystore.properties' dosyasını düzenleyin" -ForegroundColor Cyan
        Write-Host "  • Otomatik test anahtarı üretmek için: .\TabuKA.ps1 create-keystore" -ForegroundColor Cyan
        exit 1
    }

    $fullKeystorePath = (Resolve-Path $KeystorePath).Path
    $outputDir = "publish\android"
    if (-not (Test-Path $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    }

    Write-Host "`n[1/2] İmzalı paket derleniyor ve yayınlanıyor (Release)..." -ForegroundColor Green
    Write-Host "  Keystore: $fullKeystorePath" -ForegroundColor Cyan
    Write-Host "  Alias:    $KeyAlias" -ForegroundColor Cyan
    Write-Host "  Format:   $PackageFormat" -ForegroundColor Cyan

    dotnet publish "TabuKA.Android\TabuKA.Android.csproj" `
        -c Release `
        -f net10.0-android `
        -p:AndroidKeyStore=true `
        -p:AndroidSigningKeyStore="$fullKeystorePath" `
        -p:AndroidSigningStorePass="$KeyPassword" `
        -p:AndroidSigningKeyAlias="$KeyAlias" `
        -p:AndroidSigningKeyPass="$KeyPassword" `
        -p:AndroidPackageFormat="$PackageFormat" `
        -o $outputDir

    if ($LASTEXITCODE -ne 0) {
        Write-Host "`n❌ Android yayınlama sırasında hata oluştu!" -ForegroundColor Red
        exit $LASTEXITCODE
    }

    Write-Host "`n[2/2] Yayınlanan imzalı paketler kontrol ediliyor..." -ForegroundColor Green
    $signedArtifacts = Get-ChildItem -Path $outputDir -Filter "*Signed*" -Recurse -ErrorAction SilentlyContinue
    if (-not $signedArtifacts) {
        $signedArtifacts = Get-ChildItem -Path $outputDir -Filter "*.$PackageFormat" -Recurse -ErrorAction SilentlyContinue
    }
    if (-not $signedArtifacts) {
        $signedArtifacts = Get-ChildItem -Path "TabuKA.Android\bin\Release\net10.0-android" -Filter "*Signed*" -Recurse -ErrorAction SilentlyContinue
    }

    if ($signedArtifacts) {
        Write-Host "`n🎉 BAŞARILI! İmzalı Android paketi hazır:" -ForegroundColor Green
        foreach ($item in $signedArtifacts) {
            $sizeMb = [math]::Round($item.Length / 1MB, 2)
            Write-Host "  📱 Paket: $($item.FullName) ($sizeMb MB)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "`n⚠️ Yayınlama tamamlandı ancak paket bulunamadı. Lütfen $outputDir klasörünü kontrol edin." -ForegroundColor Yellow
    }
}

switch ($Action) {
    "build"                  { Test-Build }
    "test"                   { Test-Build }
    "run"                    { Run-Desktop }
    "build-android"          { Test-BuildAndroidTeacher }
    "run-android"            { Run-Android }
    "create-keystore"        { Create-Keystore }
    "create-keystore-custom" { Create-Keystore-Custom }
    "keystore"               { Create-Keystore }
    "publish-android"        { Publish-Android }
    "android-publish"        { Publish-Android }
}