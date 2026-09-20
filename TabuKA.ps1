<#
.SYNOPSIS
    TabuKA Yardımcı Scripti
.DESCRIPTION
    TabuKA projesinin derleme, test ve çalıştırma işlemlerini tek dosyadan yönetir.

.PARAMETER Action
    Gerçekleştirilecek işlem: build | run | build-android | run-android | test

.PARAMETER Configuration
    Derleme yapılandırması (Debug, Release). Varsayılan: Debug
#>

param (
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet("build", "run", "build-android", "run-android", "test")]
    [string]$Action,

    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

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

function Test-Build {
    Show-Banner -Emoji "👾" -Title "Proje Derleniyor..."

    Write-Host "`n[1/2] TabuKA Masaüstü Derleniyor (dotnet build)..." -ForegroundColor Green
    dotnet build TabuKA.csproj --configuration Debug
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
    Show-Banner -Emoji "🎮" -Title "Kelime Anlatma Arenası"
    Write-Host "  Masaüstü uygulaması başlatılıyor...     " -ForegroundColor Cyan

    dotnet run --project TabuKA.csproj
}

function Test-BuildAndroidTeacher {
    Show-Banner -Emoji "🤖" -Title "Android APK Derleme"

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
        Write-Host "`n🎉 TEBRİKLER! Android paketi başarıyla hazırlandı:" -ForegroundColor Green
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

switch ($Action) {
    "build" { Test-Build }
    "test" { Test-Build }
    "run" { Run-Desktop }
    "build-android" { Test-BuildAndroidTeacher }
    "run-android" { Run-Android }
}