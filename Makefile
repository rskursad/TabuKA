# ============================================================
#  TabuKA Makefile - Linux / macOS / Windows (Git-Bash, WSL)
#  Windows için PowerShell alternatifi: TabuKA.ps1
# ============================================================

PROJECT       := TabuKA.csproj
TEST_PROJ     := TabuKA.Tests/TabuKA.Tests.csproj
ANDROID_PROJ  := TabuKA.Android/TabuKA.Android.csproj
CONFIG        ?= Debug
OUTPUT_DIR    := publish

# ---- Platform tespiti ----
ifeq ($(OS),Windows_NT)
PLATFORM := windows
DOTNET_FRAMEWORK := net10.0-windows
else
UNAME_S := $(shell uname -s)
ifeq ($(UNAME_S),Darwin)
PLATFORM := macos
DOTNET_FRAMEWORK := net10.0
else
PLATFORM := linux
DOTNET_FRAMEWORK := net10.0
endif
endif

# ---- Renkli çıktı ----
C_RESET  := \033[0m
C_CYAN   := \033[36m
C_GREEN  := \033[32m
C_RED    := \033[31m
C_YELLOW := \033[33m

define banner
	@printf "$(C_CYAN)==========================================\n$(C_RESET)"
	@printf "$(C_YELLOW)  TabuKA - $(1)\n$(C_RESET)"
	@printf "$(C_CYAN)==========================================\n$(C_RESET)"
endef

.PHONY: all help build run test ci publish \
        publish-linux publish-win publish-mac \
        workload-android android-build android-run apk \
        clean

all: build

help:
	@echo "TabuKA - Make komutları"
	@echo "------------------------"
	@echo "  make build                Derle (varsayılan: Debug)"
	@echo "  make run                  Masaüstü uygulamayı çalıştır"
	@echo "  make test                 Birim testlerini çalıştır"
	@echo "  make ci                   Derle + test et"
	@echo "  make publish              Çalıştığı platform için yayınla (framework-dependent)"
	@echo "  make publish-linux        Kendi kendine yeten Linux paketi (linux-x64)"
	@echo "  make publish-win          Windows paketi (win-x64)"
	@echo "  make publish-mac          macOS paketi (osx-x64)"
	@echo "  make workload-android     Android iş yükünü kur"
	@echo "  make android-build        Android APK derle"
	@echo "  make android-run          Bağlı cihaz/emülatörde çalıştır"
	@echo "  make clean                Bin/obj ve publish klasörlerini temizle"
	@echo ""
	@echo "  Konfigürasyon: CONFIG=Release (örn: make run CONFIG=Release)"
	@echo "  Aktif platform: $(PLATFORM)"

build:
	$(call banner,Derleniyor [$(CONFIG)] - $(PLATFORM))
	dotnet build $(PROJECT) -c $(CONFIG)

run:
	$(call banner,Oyun Başlatılıyor [$(CONFIG)] - $(PLATFORM))
	dotnet run --project $(PROJECT) -c $(CONFIG) -f $(DOTNET_FRAMEWORK)

test:
	$(call banner,Birim Testleri [$(CONFIG)])
	dotnet test $(TEST_PROJ) -c $(CONFIG)

ci: build test

publish:
	$(call banner,Publish - $(PLATFORM))
	dotnet publish $(PROJECT) -c Release -f net10.0 -o $(OUTPUT_DIR)/$(PLATFORM)
	@echo ""
	@printf "$(C_GREEN)✅ Paket hazır: $(OUTPUT_DIR)/$(PLATFORM)$(C_RESET)\n"
	@printf "$(C_YELLOW)   Çalıştırmak için: ./$(OUTPUT_DIR)/$(PLATFORM)/$(PROJECT:.csproj=)$(C_RESET)\n"

publish-linux:
	$(call banner,Publish - Linux (self-contained))
	dotnet publish $(PROJECT) -c Release -f net10.0 -r linux-x64 --self-contained true -o $(OUTPUT_DIR)/linux

publish-win:
	$(call banner,Publish - Windows (win-x64))
	@if [ "$(PLATFORM)" != "windows" ]; then \
		printf "$(C_RED)❌ Windows paketi yalnızca Windows üzerinde derlenebilir.\n$(C_RESET)"; \
		exit 1; \
	fi
	dotnet publish $(PROJECT) -c Release -f net10.0-windows -r win-x64 --self-contained false -o $(OUTPUT_DIR)/win

publish-mac:
	$(call banner,Publish - macOS (osx-x64))
	@if [ "$(PLATFORM)" != "macos" ]; then \
		printf "$(C_RED)❌ macOS paketi yalnızca macOS üzerinde derlenebilir.\n$(C_RESET)"; \
		exit 1; \
	fi
	dotnet publish $(PROJECT) -c Release -f net10.0 -r osx-x64 --self-contained true -o $(OUTPUT_DIR)/macos

workload-android:
	$(call banner,Android İş Yükü Kurulumu)
	dotnet workload install android

android-build:
	$(call banner,Android APK Derleniyor [$(CONFIG)])
	@if dotnet workload list 2>/dev/null | grep -qi "android"; then \
		dotnet build $(ANDROID_PROJ) -c $(CONFIG); \
	else \
		printf "$(C_RED)❌ 'android' iş yükü kurulu değil!\n$(C_RESET)"; \
		printf "$(C_YELLOW)   Kurulum: make workload-android\n$(C_RESET)"; \
		exit 1; \
	fi

apk: android-build

android-run:
	$(call banner,Android Cihazda Çalıştırılıyor [$(CONFIG)])
	@if command -v adb >/dev/null 2>&1; then \
		adb devices; \
	else \
		printf "$(C_YELLOW)⚠️ adb PATH'te bulunamadı.\n$(C_RESET)"; \
	fi
	dotnet build $(ANDROID_PROJ) -t:Run -c $(CONFIG)

clean:
	$(call banner,Temizlik)
	dotnet clean $(PROJECT) -c $(CONFIG) -v q
	rm -rf bin obj
	rm -rf TabuKA.Android/bin TabuKA.Android/obj
	rm -rf TabuKA.Tests/bin TabuKA.Tests/obj
	rm -rf $(OUTPUT_DIR)
	@printf "$(C_GREEN)✅ Temizlendi.\n$(C_RESET)"